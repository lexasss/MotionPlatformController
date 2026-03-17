using MotionSystems;
using System.Globalization;

namespace ValtraIMU;

/// <summary>
/// The app, either stream Valtra IMU+GNSS data to ForceSeatPM or simulate it with dummy data, depending on the settings.
/// </summary>
internal class Program
{
    static string ProfileName => "SDK - Vehicle Telemetry ACE";

    static void Main()
    {
        // Set the US-culture across the application to avoid decimal point parsing issues
        var culture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        // Get settings
        if (!Settings.TryGetInstance(out Settings settings, out string? error))
        {
            Console.WriteLine(error);
            return;
        }

        // Try to create IMU data provider
        var imuDataProvider = DataProviders.IMUFile.Create(ref settings);
        if (imuDataProvider == null)
        {
            Console.WriteLine($"Simulation mode {settings.SimulationMode}.");
        }

        // Run the feeder
        Task.Run(async () => await RunMotionPlatform(settings, imuDataProvider)).Wait();
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
