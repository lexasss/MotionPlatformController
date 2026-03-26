using MotionSystems;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;
using System.Reflection;

namespace ValtraIMU;

/// <summary>
/// The app, either streams Valtra IMU+GNSS data to ForceSeatPM or simulates it with dummy data, depending on the settings.
/// </summary>
internal class Program : Command<Settings>
{
    public record class ContextArgs(double? Amplitude = null, bool? IsDebugMode = null, bool? IsVerbose = null);

    static string ProfileName => "SDK - Vehicle Telemetry ACE";

    static ContextArgs _contextArgs = new();

    static void Main(string[] args)
    {
        bool hasFinished = false;

        do
        {
            Console.Clear();

            var app = new CommandApp<Program>();
            app.WithData(_contextArgs);
            app.Configure(config =>
            {
                config.SetApplicationCulture(CultureInfo.GetCultureInfo("en-US"));
                config.SetApplicationName($"{Assembly.GetExecutingAssembly().FullName?.Split(',')[0]}.exe");
            });
            hasFinished = app.Run(args) != 0;

        } while (!hasFinished);
    }

    /// <summary>
    /// Called internally by <see cref="Command"/> base class
    /// </summary>
    /// <param name="context">Base class context</param>
    /// <param name="settings">Parsed command line settings</param>
    /// <param name="cts">Cancellation token</param>
    /// <returns>0 as marker than user did not requested to exit, non-zero otherwise</returns>
    public override int Execute(CommandContext context, Settings settings, CancellationToken cts)
    {
        settings.Resolve((ContextArgs?)context.Data);

        // Try to create IMU data provider
        var imuDataProvider = DataProviders.IMUFile.Create(ref settings);
        if (imuDataProvider == null)
        {
            Console.WriteLine($"Simulation mode {settings.SimulationMode.Value}.");
        }

        // Run the feeder
        Task<Result> task = Task.Run(async Task<Result>? () => await RunMotionPlatform(settings, imuDataProvider), cts);
        task.Wait(cts);

        _contextArgs = new ContextArgs(settings.Amplitude, settings.IsDebugMode, settings.IsVerbose);

        if (task.IsCanceled)
            return -1;

        if (task.Result != Result.Canceled)
        {
            var shouldRunAnotherTask = AnsiConsole.Ask("Run another task (y/n):", "n").Equals("y", StringComparison.CurrentCultureIgnoreCase);
            return shouldRunAnotherTask ? 0 : -1;
        }

        return -1;
    }

    static async Task<Result> RunMotionPlatform(Settings settings, DataProviders.IMUFile? imuDataProvider)
    {
        using var mi = new ForceSeatMI_NET8();

        if (!mi.IsLoaded())
        {
            Console.WriteLine("ForceSeatMI library has not been found! Please install ForceSeatPM.");
            return Result.Failed;
        }

        if (!mi.ActivateProfile(ProfileName))
        {
            Console.WriteLine($"Failed to activate the profile! Please make sure that the '{ProfileName}' profile is installed and active in ForceSeatPM.");
            return Result.Failed;
        }

        Console.Write("Connecting to the MotionPlatform client... ");
        await Task.Delay(3000);
        Console.WriteLine("done.");

        Feeders.DataFeeder feeder = imuDataProvider == null ?
            new Feeders.Dummy(mi, settings) :
            new Feeders.IMU(mi, settings, imuDataProvider);

        return feeder.Run();

        //Console.Write("Parking the platform... ");
        //mi.Park(FSMI_ParkMode.Normal);
        //await Task.Delay(3000);
    }
}
