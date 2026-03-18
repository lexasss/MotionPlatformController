using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace ValtraIMU;

/// <summary>
/// Command-line settings for the application. Do not use the class instance directly,
/// rather obtain the <see cref="Settings"/> class instance using <see cref="Resolve"/> method.
/// </summary>
internal class SettingsCLI : CommandSettings
{
    [Description("Valtra IMU+GNSS data, or 'sim' to use data simulator")]
    [CommandArgument(0, "[filename]")]
    public string? Filename { get; set; }

    [CommandOption("-m|--mode")]
    [Description("Simulation mode")]
    public SimulationMode? SimulationMode { get; set; }

    [CommandOption("-a|--amplitude")]
    [Description("Signal amplitude in simulation mode")]
    public double? Amplitude { get; set; }

    [CommandOption("-x|--axis")]
    [Description("Axis used in simulation mode")]
    public Axis? Axis { get; set; }

    [CommandOption("-f|--frequency")]
    [Description("Signal frequency in simulation mode")]
    public double? Frequency { get; set; }

    [CommandOption("-s|--skip")]
    [Description("Skip rate for IMU data")]
    public int? SkipRate { get; set; }

    [CommandOption("-v|--verbose")]
    [Description("Debug info is printed in the verbose mode.")]
    public bool? IsVerbose { get; set; }

    [CommandOption("-d|--debug")]
    [Description("Sets to the debug mode.")]
    public bool? IsDebugMode { get; set; }

    public Settings Resolve()
    {
        var result = new Settings();

        result.Filename = Filename ?? (AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select IMU data source:")
                    .AddChoices(new[]
                    {
                        "Recorded filename",
                        "Simulated data"
                    })).StartsWith("Sim") ? "sim" : null);
        if (result.Filename == "sim")
        {
            result.SimulationMode = SimulationMode ?? AnsiConsole.Prompt(
                new SelectionPrompt<SimulationMode>()
                    .Title("Select simulation mode:")
                    .AddChoices(Enum.GetValues<SimulationMode>()));
            result.Axis = Axis ?? AnsiConsole.Prompt(
                new SelectionPrompt<Axis>()
                    .Title("Select axis used in simulation mode:")
                    .AddChoices(Enum.GetValues<Axis>()));
        }

        result.Amplitude = Amplitude ?? result.Amplitude;
        result.Frequency = Frequency ?? result.Frequency;
        result.SkipRate = SkipRate ?? result.SkipRate;
        result.IsVerbose = IsVerbose ?? result.IsVerbose;
        result.IsDebugMode = IsDebugMode ?? result.IsDebugMode;

        return result;
    }
}

internal class Settings
{
    public string? Filename { get; set; }
    public SimulationMode SimulationMode { get; set; } = SimulationMode.SineWaveAccel;
    public Axis Axis { get; set; } = Axis.Forward;
    public double Amplitude { get; set; } = 1;
    public double Frequency { get; set; } = 0.5;
    public int SkipRate { get; set; } = 0;
    public bool IsVerbose { get; set; } = false;
    public bool IsDebugMode { get; set; } = false;

    public static int Interval => 4;   // ms, corresponds to 250 Hz
}