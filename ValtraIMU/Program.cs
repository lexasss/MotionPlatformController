using MotionSystems;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ValtraIMU;

/// <summary>
/// The app, either streams Valtra IMU (+GNSS) data to ForceSeatPM or simulates it with dummy data, depending on the settings.
/// </summary>
internal class Program : Command<Settings>
{
    public record class ContextArgs(double? Amplitude = null, bool? IsDebugMode = null, bool? IsVerbose = null);

    static string[] Profiles => [
        "SDK - Vehicle Telemetry ACE",
        "SDK - Positioning"
    ];

    static ContextArgs _contextArgs = new();
    static ForceSeatMI_NET8 _mi = new ();
    static FSMI_PlatformInfo _platformInfo = new();
    static bool _isSimulator = false;
    static bool _isPositioningSDK = false;

    static void Main(string[] args)
    {
        bool hasFinished = false;

        _platformInfo.structSize = (byte)Marshal.SizeOf(_platformInfo);

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

        var profile = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select ForceSeatPM profile:")
                    .AddChoices(Profiles));
        _isPositioningSDK = profile == Profiles[1];

        if (!_mi.ActivateProfile(profile))
        {
            Console.WriteLine($"Failed to activate the profile. Please make sure that the '{profile}' profile is installed and active in ForceSeatPM.");
            return false;
        }

        // _mi.GetPlatformInfoEx(ref _platformInfo, _platformInfo.structSize, 100);
        // _isSimulator = platformInfo.state == FSMI_PlatformCurrentState.ParkingCompleted;    // how to detect the simualtion device?

        if (_isSimulator)
        {
            Console.WriteLine("Connected to the MotionPlatform simulator.");
        }
        else
        {
            Console.Write("Connecting to the MotionPlatform client... ");
            await Task.Delay(3000);     // manual suggests having this delay...
            Console.WriteLine("done.");
        }

        if (!_mi.BeginMotionControl())
        {
            Console.WriteLine("Failed to start motion control!");
            return false;
        }
        else if (!_isSimulator)
        {
            Console.Write("Centralizing the platform... ");
            _mi.Park(FSMI_ParkMode.ToCenter);

            int maxTime = 10000;
            do
            {
                await Task.Delay(500);
                _mi.GetPlatformInfoEx(ref _platformInfo, _platformInfo.structSize, 100);
                maxTime -= 500;
            } while ((_platformInfo.state & FSMI_PlatformCurrentState.ParkingCompleted) == 0 && maxTime >= 0);

            //await Task.Delay(10000);
            Console.WriteLine("done.");
            await Task.Delay(1500);
        }

        return true;
    }

    static async Task DisengagePlatform()
    {
        _mi.EndMotionControl();

        if (!_isSimulator)
        {
            _mi.Park(FSMI_ParkMode.Normal);
           
            Console.Write("Parking the platform... ");

            do
            {
                await Task.Delay(500);
                _mi.GetPlatformInfoEx(ref _platformInfo, _platformInfo.structSize, 100);
            } while ((_platformInfo.state & FSMI_PlatformCurrentState.ParkingCompleted) == 0);

            //await Task.Delay(6000);
            Console.WriteLine("done.");
        }
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

        feeder.UsePositionInsteadOfTelemetry = _isPositioningSDK;

        return feeder.Run();
    }
}
