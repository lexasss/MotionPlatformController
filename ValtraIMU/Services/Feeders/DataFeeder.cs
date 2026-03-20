using MotionSystems;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ValtraIMU.Services;

namespace ValtraIMU.Feeders;

/// <summary>
/// Base class for feeding data to MotionPlatform using ForceSeatMI.
/// It handles the main loop and timing, while descendants implement <see cref="SendData">SendData</see> to send data 
/// and set <see cref="_nextSampleTimestamp">_nextSampleTimestamp</see>.
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
    public virtual void Run()
    {
        if (!_mi.BeginMotionControl())
        { 
            Console.WriteLine("Failed to start motion control!");
            return;
        }

        _ = TimeBeginPeriod(1);

        var printer = new Services.DisplayPrinter();
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        _nextSampleTimestamp = 0;

        Console.WriteLine("Press 'q' to exit");

        while (true)
        {
            if (Console.KeyAvailable && Console.ReadKey().KeyChar == 'q')
            {
                Console.CursorLeft--;
                break;
            }

            if (!SendData())
                break;

            if (_settings.IsDebugMode && !printer.PrintStatus(_mi))
                break;

            while (stopWatch.ElapsedMilliseconds < _nextSampleTimestamp)
            {
                Thread.Sleep(1);
            }
        }

        stopWatch.Stop();

        Console.WriteLine();
        Console.WriteLine("Stopped.");

        _mi.EndMotionControl();

        _ = TimeEndPeriod(1);
    }

    #region Shared with descendants

    protected readonly Settings _settings = settings;

    protected readonly TelemetryBroadcaster _telemetryBroadcaster = new();

    protected ForceSeatMI_NET8 _mi = mi;
    protected long _nextSampleTimestamp = 0;    /// ms relative to the start, to be set by descendants in <see cref="SendData">SendData</see>.

    /// <summary>
    /// To be implemented by descendants to send data and set <see cref="_nextSampleTimestamp">_nextSampleTimestamp</see> for the next sample.
    /// </summary>
    /// <returns>Must return true if the telemetry data was sent successfully; otherwise, false.</returns>
    protected abstract bool SendData();

    #endregion

    #region Internal

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint uMilliseconds);

    #endregion
}
