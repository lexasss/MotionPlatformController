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

        Models.IMUData[]? data = null;

        if (!File.Exists(settings.Filename))
        {
            settings.Filename = SharpFileOpenDialog.ShowSingleSelect(IntPtr.Zero, "Valtra IMU+GNSS data");
        }

        if (File.Exists(settings.Filename))
        {
            Console.Write($"Loading data from {settings.Filename}...  ");
            data = Services.IMUDataProvider.LoaFromFile(settings.Filename);
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

            Services.DataFeeder feeder = data == null ? 
                new Services.DummyDataFeeder(mi) :
                new Services.IMUDataFeeder(new ForceSeatMI_NET8(), data);

            feeder.Run();
        }
    }
}
