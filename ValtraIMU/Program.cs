using MotionSystems;
using SharpDialogs;
using System.Globalization;

namespace ValtraIMU;

internal class Program
{
    static void Main(string[] args)
    {
        // Set the US-culture across the application to avoid decimal point parsing/logging issues
        var culture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        if (!Services.Settings.TryGetInstance(out Services.Settings settings, out string? error))
        {
            Console.WriteLine(error);
            return;
        }

        Services.IMUDataProvider? dataProvider = null;

        if (!File.Exists(settings.Filename))
        {
            settings.Filename = SharpFileOpenDialog.ShowSingleSelect(IntPtr.Zero, "Valtra IMU+GNSS data");
        }

        if (File.Exists(settings.Filename))
        {
            Console.Write($"Loading data from {settings.Filename}...  ");
            dataProvider = new Services.IMUDataProvider(settings.Filename, 1);
            Console.WriteLine("done.");
        }
        else
        {
            Console.WriteLine("Simulating data.");
        }

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

            Services.DataFeeder feeder = dataProvider == null ? 
                new Services.DummyDataFeeder(mi) :
                new Services.IMUDataFeeder(new ForceSeatMI_NET8(), dataProvider);

            feeder.Run();

            Console.Write("Parking the platform... ");
            mi.Park(FSMI_ParkMode.Normal);
            Thread.Sleep(3000);
            Console.WriteLine("done.");
        }
    }
}
