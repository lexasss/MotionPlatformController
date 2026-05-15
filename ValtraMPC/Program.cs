using MotionSystems;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ValtraMPC;

/// <summary>
/// The app, either streams Valtra IMU (+GNSS) data to ForceSeatPM or simulates it with dummy data, depending on the settings.
/// </summary>
internal class Program : Command<Settings>
{
    static string[] Profiles => [
        "SDK - Vehicle Telemetry ACE",
        "SDK - Positioning"
    ];

    static Models.ContextArgs _contextArgs = new();
    static ForceSeatMI_NET8 _mi = new ();
    static FSMI_PlatformInfo _platformInfo = new();
    static bool _isPositioningSDK = false;

    static void Main(string[] args)
    {
        bool hasFinished = false;

        _platformInfo.structSize = (byte)Marshal.SizeOf(_platformInfo);

        Settings.CanBroadcastData = AnsiConsole.Confirm("Is data broadcasting enabled?", false);

        if (!EngagePlatform())
            return;

        bool mustClear = false;
        do
        {
            if (mustClear)
                Console.Clear();

            var app = new CommandApp<Program>();
            app.WithData(_contextArgs);
            app.Configure(config =>
            {
                config.SetApplicationCulture(CultureInfo.GetCultureInfo("en-US"));
                config.SetApplicationName($"{Assembly.GetExecutingAssembly().FullName?.Split(',')[0]}.exe");
            });
            hasFinished = app.Run(args) != 0;
            mustClear = true;

        } while (!hasFinished);

        DisengagePlatform();
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
        settings.Resolve((Models.ContextArgs?)context.Data);
        if (settings.IsExiting)
            return -1;

        // Try to create IMU data provider
        DataProviders.IMUFileFront? imuFrontProvider = DataProviders.IMUFileFront.Create(ref settings);
        DataProviders.IMUFileCabin? imuCabinProvider = null;

        if (imuFrontProvider == null)
        {
            imuCabinProvider = DataProviders.IMUFileCabin.Create(ref settings);
        }

        // Run the feeder
        var result = RunMotionPlatform(settings, imuFrontProvider, imuCabinProvider);

        // memorize some session parameters to pass them to the next cycle if needed
        _contextArgs = Models.ContextArgs.FromSettings(settings);

        if (result != Result.Exiting)
        {
            var shouldRunAnotherTask = AnsiConsole.Confirm("Run another task?", result == Result.Stopped);
            return shouldRunAnotherTask ? 0 : -1;
        }

        return -1;
    }

    #region Internal

    static bool EngagePlatform()
    {
        _mi = new ForceSeatMI_NET8();

        if (!_mi.IsLoaded())
        {
            AnsiConsole.MarkupLine("ForceSeatMI library [bold]has not been found[/]! Please install ForceSeatPM.");
            return false;
        }

        var profile = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select ForceSeatPM profile:")
                    .AddChoices(Profiles));
        AnsiConsole.MarkupLine($"Selected ForceSeatPM profile: [green]{profile}[/]");
        _isPositioningSDK = profile == Profiles[1];

        if (!_mi.ActivateProfile(profile))
        {
            AnsiConsole.MarkupLine($"Failed to activate the profile. Please make sure that [bold]'{profile}' profile[/] exists in ForceSeatPM and custom apps can activate it.");
            return false;
        }

        bool isPlatformConnected = false;
        AnsiConsole.Status().Start("Connecting to the MotionPlatform client... ", ctx =>
        {
            var task = WaitForAllStates(20000, FSMI_PlatformCurrentState.RefRunCompleted);
            task.Wait();
            isPlatformConnected = task.Result;
        });
        AnsiConsole.Write("Connecting to the MotionPlatform client... ");
        
        if (!isPlatformConnected)
        {
            AnsiConsole.MarkupLine("[red]failed[/].\nExiting... ");
            return false;
        }

        AnsiConsole.MarkupLine("[green]done[/].");

        if (!_mi.BeginMotionControl())
        {
            AnsiConsole.MarkupLine("[red]Failed to start motion control![/]");
            return false;
        }
        else
        {
            _mi.Park(FSMI_ParkMode.ToCenter);

            bool hasCentralized = false;
            AnsiConsole.Status().Start("Centralizing the platform... ", ctx =>
            {
                var task = WaitForAllStates(10000, FSMI_PlatformCurrentState.ParkingCompleted, FSMI_PlatformCurrentState.SoftParkToCenter);
                task.Wait();
                hasCentralized = task.Result;
            });

            AnsiConsole.Write($"Centralizing the platform... ");
            AnsiConsole.MarkupLine(hasCentralized ? "[green]done[/]." : "[orange1]timeout[/].");
        }

        return true;
    }

    static void DisengagePlatform()
    {
        bool result = _mi.Park(FSMI_ParkMode.Normal);

        if (result)
        {
            bool hasParked = false;
            AnsiConsole.Status().Start("Parking the platform... ", ctx =>
            {
                var task = WaitForAllStates(15000, FSMI_PlatformCurrentState.ParkingCompleted, FSMI_PlatformCurrentState.SoftParkNormal);
                task.Wait();
                hasParked = task.Result;
            });

            AnsiConsole.Write("Parking the platform... ");
            AnsiConsole.MarkupLine(hasParked ? "[green]done[/]." : "[orange1]timeout[/].");
        }

        _mi.EndMotionControl();
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
    
    static async Task<bool> WaitForAllStates(long maxWaitingTime, params ushort[] states)
    {
        do
        {
            await Task.Delay(500);
            _mi.GetPlatformInfoEx(ref _platformInfo, _platformInfo.structSize, 100);
            maxWaitingTime -= 500;
        } while (!states.All(state => (_platformInfo.state & state) != 0) && maxWaitingTime >= 0);

        return states.All(state => (_platformInfo.state & state) != 0);
    }

    #endregion
}
