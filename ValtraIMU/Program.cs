using MotionSystems;
using System.Globalization;

namespace ValtraIMU;

internal class Program
{
    static void Main(string[] args)
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
        var imuDataProvider = Services.IMUDataProvider.Create(ref settings);
        if (imuDataProvider == null)
        {
            Console.WriteLine("Simulating data.");
        }

        // Run the feeder
        using (var mi = new ForceSeatMI_NET8())
        {
            if (!mi.IsLoaded())
            {
                Console.WriteLine("ForceSeatMI library has not been found! Please install ForceSeatPM.");
                return;
            }

            mi.ActivateProfile("SDK - Vehicle Telemetry ACE");

            Console.Write("Connecting to the MotionPlatform client... ");
            Thread.Sleep(3000);
            Console.WriteLine("done.");

            Services.DataFeeder feeder = imuDataProvider == null ? 
                new Services.DummyDataFeeder(mi, settings) :
                new Services.IMUDataFeeder(new ForceSeatMI_NET8(), settings, imuDataProvider);

            feeder.Run();

            Console.Write("Parking the platform... ");
            mi.Park(FSMI_ParkMode.Normal);
            Thread.Sleep(3000);
            Console.WriteLine("done.");
        }
    }
}
