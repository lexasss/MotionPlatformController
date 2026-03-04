using CommandLine;

namespace ValtraIMU.Services;

internal class Settings
{
    [Option('f', "file", Required = false, HelpText = "Valtra IMU+GNSS data")]
    public string? Filename { get; set; }

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

    /// <summary>
    /// IMPORTANT! The constructor must not be used explicitely, rather use <see cref="TryGetInstance">TryGetInstance</see>.
    /// </summary>
    public Settings() { }


    // Internal

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
}
