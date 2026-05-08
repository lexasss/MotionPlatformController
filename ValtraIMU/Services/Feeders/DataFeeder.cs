using MotionSystems;
using Spectre.Console;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ValtraIMU.Feeders;

/// <summary>
/// Base class for feeding data to MotionPlatform using ForceSeatMI.
/// It handles the main loop and timing, while descendants implement <see cref="PrepareTelemetry"> 
/// where they set the telemetry fields before this class method <see cref="SendData"/>
/// broadcasts and upstreams it to the MotionPlatform. Descendants also set <see cref="_nextRecordTimestamp">.
/// </summary>
/// <param name="mi">ForceSeatMI object</param>
/// <param name="settings">settings object</param>
internal abstract class DataFeeder
{
    public bool UsePositionInsteadOfTelemetry { get; set; } = false;

    public DataFeeder(ForceSeatMI_NET8 mi, Settings settings)
    {
        _mi = mi;
        _settings = settings;

        _position.structSize = (byte)Marshal.SizeOf(_position);
        _position.mask = FSMI_POS_BIT.STATE | FSMI_POS_BIT.POSITION | FSMI_POS_BIT.MAX_SPEED;
        _position.maxSpeed = 65535;

        (_useSFX, _sfxAmplitude, _sfxFrequency, _sfxArea) = settings.ParseSFX();
    }

    /// <summary>
    /// Starts and manages the main simulation loop: the MP control, periodic data transmission,
    /// MP status display, and graceful shutdown upon user request.
    /// </summary>
    /// <remarks>This method is blocking and should be called from a thread that can safely wait for user input.
    /// Subsequent calls to this method without proper cleanup may result in unexpected behavior.</remarks>
    /// <returns>Execution result</returns>
    public virtual Result Run()
    {
        var result = Result.Ok;

        _ = TimeBeginPeriod(1);

        var printer = new Services.DisplayPrinter();

        _stopwatch.Start();
        _nextRecordTimestamp = 0;

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("Press:");
        AnsiConsole.MarkupLine(" - [blue]ESC[/] to terminate");
        AnsiConsole.MarkupLine(" - [blue]s[/] to stop current data stream");
        AnsiConsole.MarkupLine(" - [blue]SPACE[/] to pause/continue");
        AnsiConsole.MarkupLine(" - [blue]v[/] to toggle verbosity");
        AnsiConsole.MarkupLine(" - [blue]d[/] to toggle MotionPlatform diagnostics data output");
        AnsiConsole.WriteLine("\nRunning . . .");

        Console.CursorVisible = false;

        while (true)
        {
            var userInput = HandleKeyPress();
            if (userInput == KeyHandlerResult.Interrupted)
            {
                result = Result.Stopped;
                break;
            }
            else if (userInput == KeyHandlerResult.Exiting)
            {
                result = Result.Exiting;
                break;
            }

            if (!_isPaused)
            {
                if (!SendData())
                    break;

                var miInfoUpdateResult = UpdateMotionPlatformInfo();
                if (miInfoUpdateResult == Result.Failed)
                {
                    result = Result.Failed;
                    break;
                }
                else if (miInfoUpdateResult == Result.Ok)
                {
                    if (Settings.CanBroadcastData && _settings.BroadcastDataType.Value == BroadcastDataType.PlatformInfo)
                        _telemetryBroadcaster.Send(ref _platformInfo);

                    if (_settings.IsDebugMode)
                        printer.PrintStatus(ref _platformInfo);
                }
            }

            while (_stopwatch.ElapsedMilliseconds < _nextRecordTimestamp)
            {
                Thread.Sleep(1);
            }
        }

        _stopwatch.Stop();

        if (Console.CursorLeft > 0)
            AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]Stopped[/].");
        Console.CursorVisible = true;

        _ = TimeEndPeriod(1);

        return result;
    }

    #region Shared with descendants

    protected readonly Settings _settings;

    /// <summary>
    /// To be filled by descendants when calling <see cref="PrepareTelemetry"/>
    /// </summary>
    protected FSMI_TelemetryACE _telemetry = FSMI_TelemetryACE.Prepare();

    /// <summary>
    /// To be filled by descendants when calling <see cref="PreparePosition"/>
    /// </summary>
    protected FSMI_TopTablePositionPhysical _position = new();

    /// <summary>
    /// Time in milliseconds relative to the start, to be set by descendants in <see cref="SendData">.
    /// </summary>
    protected long _nextRecordTimestamp = 0;

    /// <summary>
    /// To be implemented by descendants to fill <see cref="_telemetry"/> and set <see cref="_nextRecordTimestamp">.
    /// </summary>
    /// <returns>Must return true if the telemetry data was sent successfully; otherwise, false.</returns>
    protected abstract bool PrepareTelemetry();

    /// <summary>
    /// To be implemented by descendants to fill <see cref="_position"/>. Notice that <see cref="PrepareTelemetry"> will be called anyway.
    /// </summary>
    protected virtual void PreparePosition() { }

    #endregion

    #region Internal

    readonly ForceSeatMI_NET8 _mi;
    readonly Stopwatch _stopwatch = new();
    readonly Services.TelemetryBroadcaster _telemetryBroadcaster = new();

    readonly bool _useSFX;
    readonly float _sfxAmplitude;
    readonly byte _sfxFrequency;
    readonly ushort _sfxArea;

    static FSMI_PlatformInfo _platformInfo = new();
    readonly uint _piSize = (uint)Marshal.SizeOf(_platformInfo);

    bool _isPaused = false;
    ulong _recentMark = 0;

    enum KeyHandlerResult
    {
        None,
        Handled,
        Interrupted,
        Exiting
    }

    private bool SendData()
    {
        var result = PrepareTelemetry();
        if (!result)
            return result;

        if (Settings.CanBroadcastData && _settings.BroadcastDataType.Value == BroadcastDataType.Telemetry)
            _telemetryBroadcaster.Send(ref _telemetry);

        if (UsePositionInsteadOfTelemetry)
        {
            PreparePosition();
            result = _mi.SendTopTablePosPhy(ref _position);
        }
        else
        {
            if (_useSFX)
            {
                var (sfx, audio, sbt) = CreateAdditionalData();
                result = _mi.SendTelemetryACE3(ref _telemetry, ref sfx, ref audio, ref sbt);
            }
            else
            {
                result = _mi.SendTelemetryACE(ref _telemetry);
            }
        }

        return result;
    }

    private (FSMI_SFX, FSMI_TactileAudioBasedFeedbackEffects, FSMI_SbtData) CreateAdditionalData()
    {
        var sfx = FSMI_SFX.Prepare();
        sfx.effectsCount = 1;
        sfx.effects[0].type = (byte)FSMI_SFX_EffectType.SinusLevel2;
        sfx.effects[0].area = _sfxArea;
        sfx.effects[0].amplitude = _sfxAmplitude;
        sfx.effects[0].frequency = _sfxFrequency;

        var audio = FSMI_TactileAudioBasedFeedbackEffects.Prepare();

        // seat belt tensioner
        var sbt = FSMI_SbtData.Prepare();

        return (sfx, audio, sbt);
    }

    private KeyHandlerResult HandleKeyPress()
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                return KeyHandlerResult.Exiting;
            }
            else if (key.Key == ConsoleKey.S)
            {
                return KeyHandlerResult.Interrupted;
            }
            else if (key.Key == ConsoleKey.Spacebar)
            {
                _isPaused = !_isPaused;
                if (_isPaused)
                {
                    _stopwatch.Stop();
                    if (Console.CursorLeft > 0)
                        AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("Paused, press [blue]SPACE[/] to continue");
                }
                else
                {
                    _stopwatch.Start();
                    if (Console.CursorLeft > 0)
                        AnsiConsole.WriteLine();
                    AnsiConsole.WriteLine("Running . . .");
                }
                return KeyHandlerResult.Handled;
            }
            else if (key.Key == ConsoleKey.V)
            {
                _settings.IsVerbose = !_settings.IsVerbose;
            }
            else if (key.Key == ConsoleKey.D)
            {
                _settings.IsDebugMode = !_settings.IsDebugMode;
            }
        }

        return KeyHandlerResult.None;
    }

    private Result UpdateMotionPlatformInfo()
    {
        // Get current status
        if (_mi.GetPlatformInfoEx(ref _platformInfo, _piSize, 100))
        {
            if (_platformInfo.structSize != Marshal.SizeOf(_platformInfo))
            {
                AnsiConsole.MarkupLine("[red]Incorrect structure size[/]: {0} vs {1}", _platformInfo.structSize, Marshal.SizeOf(_platformInfo));
                return Result.Failed;
            }
            else if (_platformInfo.timemark == _recentMark)
            {
                // no new platform info
            }
            else
            {
                _recentMark = _platformInfo.timemark;
                return Result.Ok;
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Failed to get platform info[/]");
        }

        return Result.Exiting;
    }

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint uMilliseconds);

    #endregion
}
