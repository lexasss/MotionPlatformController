using MotionSystems;
using Spectre.Console.Cli;
using System.Globalization;
using System.Reflection;

namespace ValtraIMU;

/// <summary>
/// The app, either streams Valtra IMU+GNSS data to ForceSeatPM or simulates it with dummy data, depending on the settings.
/// </summary>
internal class Program : Command<Settings>
{
    static string ProfileName => "SDK - Vehicle Telemetry ACE";

    static int Main(string[] args)
    {
        var app = new CommandApp<Program>();
        app.Configure(config =>
        {
            config.SetApplicationCulture(CultureInfo.GetCultureInfo("en-US"));
            config.SetApplicationName($"{Assembly.GetExecutingAssembly().FullName?.Split(',')[0]}.exe");
        });
        return app.Run(args);
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cts)
    {
        settings.Resolve();

        // Try to create IMU data provider
        var imuDataProvider = DataProviders.IMUFile.Create(ref settings);
        if (imuDataProvider == null)
        {
            Console.WriteLine($"Simulation mode {settings.SimulationMode.Value}.");
        }

        // Run the feeder
        Task.Run(async () => await RunMotionPlatform(settings, imuDataProvider), cts).Wait(cts);

        return 0;
    }

    static async Task RunMotionPlatform(Settings settings, DataProviders.IMUFile? imuDataProvider)
    {
        using var mi = new ForceSeatMI_NET8();

        if (!mi.IsLoaded())
        {
            Console.WriteLine("ForceSeatMI library has not been found! Please install ForceSeatPM.");
            return;
        }

        if (!mi.ActivateProfile(ProfileName))
        {
            Console.WriteLine($"Failed to activate the profile! Please make sure that the '{ProfileName}' profile is installed and active in ForceSeatPM.");
            return;
        }

        Console.Write("Connecting to the MotionPlatform client... ");
        await Task.Delay(3000);
        Console.WriteLine("done.");

        Feeders.DataFeeder feeder = imuDataProvider == null ?
            new Feeders.Dummy(mi, settings) :
            new Feeders.IMU(mi, settings, imuDataProvider);

        feeder.Run();

        //Console.Write("Parking the platform... ");
        //mi.Park(FSMI_ParkMode.Normal);
        //await Task.Delay(3000);

        Console.WriteLine("done.");
    }
}
