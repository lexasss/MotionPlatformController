using MotionSystems;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ValtraIMU.Feeders;

/// <summary>
/// Base class for feeding data to MotionPlatform using ForceSeatMI.
/// It handles the main loop and timing, while descendants implement <see cref="PrepareTelemetry"> 
/// where they set the telemetry fields before this class method <see cref="SendData"/>
/// broadcasts and upstreams it to the MotionPlatform. Descendants also set <see cref="_nextSampleTimestamp">.
/// </summary>
/// <param name="mi">ForceSeatMI object</param>
/// <param name="settings">settings object</param>
internal abstract class DataFeeder(ForceSeatMI_NET8 mi, Settings settings)
{
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

        if (!_mi.BeginMotionControl())
        { 
            Console.WriteLine("Failed to start motion control!");
            return Result.Failed;
        }

        _ = TimeBeginPeriod(1);

        var printer = new Services.DisplayPrinter();

        _stopwatch.Start();
        _nextSampleTimestamp = 0;

        Console.WriteLine("Press:");
        Console.WriteLine(" - ESC to terminate");
        Console.WriteLine(" - 's' to stop current data stream");
        Console.WriteLine(" - SPACE to pause/continue");
        Console.WriteLine(" - 'v' to toggle verbosity");
        Console.WriteLine(" - 'd' to toggle MotionPlatform diagnostics data output");
        Console.WriteLine("\nRunning . . .");

        while (true)
        {
            var userInput = HandleKeyPress();
            if (userInput == KeyHandlerResult.Interrupted)
            {
                break;
            }
            else if (userInput == KeyHandlerResult.Exiting)
            {
                result = Result.Canceled;
                break;
            }

            if (!_isPaused)
            {
                if (!SendData())
                    break;

                var miInfoUpdateResult = UpdateMotionPlatformInfo();
                if (miInfoUpdateResult == Result.Failed)
                {
                    break;
                }
                else if (miInfoUpdateResult == Result.Ok)
                {
                    _telemetryBroadcaster.Send(ref _platformInfo);

                    if (_settings.IsDebugMode)
                        printer.PrintStatus(ref _platformInfo);
                }
            }

            while (_stopwatch.ElapsedMilliseconds < _nextSampleTimestamp)
            {
                Thread.Sleep(1);
            }
        }

        _stopwatch.Stop();

        if (Console.CursorLeft > 0)
            Console.WriteLine();
        Console.WriteLine("Stopped.");

        _mi.EndMotionControl();

        _ = TimeEndPeriod(1);

        return result;
    }

    #region Shared with descendants

    protected readonly Settings _settings = settings;

    /// <summary>
    /// To be filled by descendants when calling <see cref="PrepareTelemetry"/>
    /// </summary>
    protected FSMI_TelemetryACE _telemetry = FSMI_TelemetryACE.Prepare();

    /// <summary>
    /// Time in milliseconds relative to the start, to be set by descendants in <see cref="SendData">.
    /// </summary>
    protected long _nextSampleTimestamp = 0;

    /// <summary>
    /// To be implemented by descendants to fill <see cref="_telemetry"/> and set <see cref="_nextSampleTimestamp">.
    /// </summary>
    /// <returns>Must return true if the telemetry data was sent successfully; otherwise, false.</returns>
    protected abstract bool PrepareTelemetry();

    #endregion

    #region Internal

    readonly ForceSeatMI_NET8 _mi = mi;
    readonly Stopwatch _stopwatch = new();
    readonly Services.TelemetryBroadcaster _telemetryBroadcaster = new();

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
        if (result)
        {
            //_telemetryBroadcaster.Send(ref _telemetry);
            _mi.SendTelemetryACE(ref _telemetry);
        }

        return result;
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
                        Console.WriteLine();
                    Console.WriteLine("Paused, press SPACE to continue");
                }
                else
                {
                    _stopwatch.Start();
                    if (Console.CursorLeft > 0)
                        Console.WriteLine();
                    Console.WriteLine("Running . . .");
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
                Console.WriteLine("Incorrect structure size: {0} vs {1}", _platformInfo.structSize, Marshal.SizeOf(_platformInfo));
                return Result.Failed;
            }
            else if (_platformInfo.timemark == _recentMark)
            {
                //Console.WriteLine("No new platform info");
            }
            else
            {
                _recentMark = _platformInfo.timemark;
                return Result.Ok;
            }
        }
        else
        {
            Console.WriteLine("Failed to get platform info");
        }

        return Result.Canceled;
    }

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint uMilliseconds);

    #endregion
}
