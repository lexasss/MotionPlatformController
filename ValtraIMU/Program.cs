using MotionSystems;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;
using System.Reflection;

namespace ValtraIMU;

/// <summary>
/// The app, either streams Valtra IMU (+GNSS) data to ForceSeatPM or simulates it with dummy data, depending on the settings.
/// </summary>
internal class Program : Command<Settings>
{
    public record class ContextArgs(double? Amplitude = null, bool? IsDebugMode = null, bool? IsVerbose = null);

    static string ProfileName => "SDK - Vehicle Telemetry ACE";

    static ContextArgs _contextArgs = new();
    static ForceSeatMI_NET8 _mi = new ();

    static void Main(string[] args)
    {
        bool hasFinished = false;

        var task = EngagePlatform();
        task.Wait();
        if (!task.Result)
            return;

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

        DisengagePlatform().Wait();
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
        DataProviders.IMUFileFront? imuFrontProvider = DataProviders.IMUFileFront.Create(ref settings);
        DataProviders.IMUFileCabin? imuCabinProvider = null;

        if (imuFrontProvider == null)
        {
            imuCabinProvider = DataProviders.IMUFileCabin.Create(ref settings);
            if (imuCabinProvider == null)
            {
                Console.WriteLine($"Simulation mode {settings.SimulationMode.Value}.");
            }
        }

        // Run the feeder
        var result = RunMotionPlatform(settings, imuFrontProvider, imuCabinProvider);

        // memorize some session parameters to pass them to the next cycle if needed
        _contextArgs = new ContextArgs(settings.Amplitude, settings.IsDebugMode, settings.IsVerbose);

        if (result != Result.Exiting)
        {
            var defaultAnswer = result == Result.Stopped ? "y" : "n";
            var shouldRunAnotherTask = AnsiConsole.Ask("Run another task (y/n):", defaultAnswer).Equals("y", StringComparison.CurrentCultureIgnoreCase);
            return shouldRunAnotherTask ? 0 : -1;
        }

        return -1;
    }

    // Internal

    static async Task<bool> EngagePlatform()
    {
        _mi = new ForceSeatMI_NET8();

        if (!_mi.IsLoaded())
        {
            Console.WriteLine("ForceSeatMI library has not been found! Please install ForceSeatPM.");
            return false;
        }

        if (!_mi.ActivateProfile(ProfileName))
        {
            Console.WriteLine($"Failed to activate the profile! Please make sure that the '{ProfileName}' profile is installed and active in ForceSeatPM.");
            return false;
        }

        Console.Write("Connecting to the MotionPlatform client... ");
        await Task.Delay(3000);
        Console.WriteLine("done.");

        //FSMI_PlatformInfo info = new();                                       // Is there a way to check that we use USB-simulator?
        //_mi.GetPlatformInfoEx(ref info, (uint)Marshal.SizeOf(info), 500);     // We could then skip waiting for the lift-up

        Console.Write("Lifting the platform up... ");
        if (!_mi.BeginMotionControl())
        {
            Console.WriteLine("Failed to start motion control!");
            return false;
        }
        else
        {
            _mi.Park(FSMI_ParkMode.ToCenter);
            await Task.Delay(10000);
            Console.WriteLine("done.");
            await Task.Delay(1500);
        }

        return true;
    }

    static async Task DisengagePlatform()
    {
        Console.Write("Parking the platform... ");
        _mi.EndMotionControl();

        await Task.Delay(7000);
        Console.WriteLine("done.");
    }

    static Result RunMotionPlatform(Settings settings, DataProviders.IMUFileFront? imuFrontProvider, DataProviders.IMUFileCabin? imuCabinProvider)
    {
        Feeders.DataFeeder feeder;
        if (imuFrontProvider != null)
            feeder = new Feeders.IMUFeederFront(_mi, settings, imuFrontProvider);
        else if (imuCabinProvider != null)
            feeder = new Feeders.IMUFeederCabin(_mi, settings, imuCabinProvider);
        else
            feeder = new Feeders.DummyFeeder(_mi, settings);

        return feeder.Run();
    }
}
