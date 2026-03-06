using CommandLine;

namespace ValtraIMU;

/// <summary>
/// Command-line settings for the application. Uses CommandLineParser library to parse the arguments.
/// </summary>
internal class Settings
{
    [Value(0, Required = false, HelpText = "Valtra IMU+GNSS data, or 'sim' to use data simulator")]
    public string? Filename { get; set; }

    [Option('m', "mode", Required = false, Default = SimulationMode.SineWave, HelpText = "Simulation mode")]
    public SimulationMode SimulationMode { get; set; } = SimulationMode.SineWave;

    [Option('s', "skip", Required = false, Default = 0, HelpText = "Skip rate for IMU data")]
    public int SkipRate { get; set; } = 0;

    [Option('a', "amplitude", Required = false, Default = 1, HelpText = "Signal amplitude in simulation mode")]
    public double Amplitude { get; set; } = 1;

    [Option('f', "frequency", Required = false, Default = 0.5, HelpText = "Signal frequency in simulation mode")]
    public double Frequency { get; set; } = 0.5;

    [Option('v', "verbose", Required = false, HelpText = "Debug info is printed in the verbose mode.")]
    public bool IsVerbose { get; set; } = false;

    [Option('d', "debug", Required = false, HelpText = "Sets to the debug mode.")]
    public bool IsDebugMode { get; set; } = false;

    public static bool TryGetInstance(out Settings settings, out string? error)
    {
        error = null;

        try
        {
            _instance ??= Create();
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        settings = _instance ?? new Settings();
        return _instance != null;
    }

    #region Internal

    static Settings? _instance = null;

    private static Settings Create()
    {
        var args = Environment.GetCommandLineArgs()[1..];
        var settings = Parser.Default.ParseArguments<Settings>(args);

        bool missesRequiredSettings = false;
        settings.WithNotParsed(errors => missesRequiredSettings = missesRequiredSettings || errors.Any(error => error is MissingRequiredOptionError));

        if (missesRequiredSettings)
            throw new Exception("Missing required options");

        return settings.Value ?? new Settings();
    }

    #endregion
}
