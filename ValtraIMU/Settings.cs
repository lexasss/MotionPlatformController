using SharpDialogs;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace ValtraIMU;

/// <summary>
/// Command-line settings for the application. 
/// Always run <see cref="Resolve"/> method before using its values.
/// </summary>
internal class Settings : CommandSettings
{
    [Description($"Valtra IMU+GNSS data file, or '{SIM_LABEL}' to use data simulator")]
    [CommandArgument(0, "[filename]")]
    public FlagValue<string?> Filename { get; set; } = new FlagValue<string?>();

    [CommandOption("-m|--mode <MODE>")]
    [Description("Simulation mode")]
    public FlagValue<SimulationMode> SimulationMode { get; set; } = new FlagValue<SimulationMode>();

    [CommandOption("-x|--axis <AXIS>")]
    [Description("Axis used in simulation mode")]
    public FlagValue<Axis> Axis { get; set; } = new FlagValue<Axis>();

    [CommandOption("-a|--amplitude <NUMBER>")]
    [Description("Signal amplitude in simulation mode")]
    [DefaultValue(1.0)]
    public double Amplitude { get; set; }

    [CommandOption("-f|--frequency <NUMBER>")]
    [Description("Signal frequency in simulation mode")]
    [DefaultValue(0.5)]
    public double Frequency { get; set; }

    [CommandOption("-s|--skip <NUMBER>")]
    [Description("Skip rate for IMU data")]
    [DefaultValue(1)]
    public int SkipRate { get; set; }

    [CommandOption("-v|--verbose")]
    [Description("Debug info is printed in the verbose mode.")]
    [DefaultValue(false)]
    public bool IsVerbose { get; set; }

    [CommandOption("-d|--debug")]
    [Description("Sets to the debug mode.")]
    [DefaultValue(false)]
    public bool IsDebugMode { get; set; }

    public static int Interval => 4;   // ms, corresponds to 250 Hz

    public void Resolve()
    {
        Filename.Value = Filename.IsSet ? Filename.Value : (AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select IMU data source:")
                    .AddChoices([
                        "1. Recorded file",
                        "2. Simulated data"
                    ])
                ).StartsWith("2") ? SIM_LABEL : null);

        if (Filename.Value == SIM_LABEL)
        {
            SimulationMode.Value = SimulationMode.IsSet ? SimulationMode.Value : AnsiConsole.Prompt(
                new SelectionPrompt<SimulationMode>()
                    .Title("Select simulation mode:")
                    .AddChoices(Enum.GetValues<SimulationMode>()));

            if (SimulationMode.Value == ValtraIMU.SimulationMode.SineWaveAccel ||
                SimulationMode.Value == ValtraIMU.SimulationMode.MovePulse)
            {
                Axis.Value = Axis.IsSet ? Axis.Value : AnsiConsole.Prompt(
                    new SelectionPrompt<Axis>()
                        .Title("Select axis used in simulation mode:")
                        .AddChoices(Enum.GetValues<Axis>()));
            }
        }
        else if (!File.Exists(Filename.Value))
        {
            Filename.Value = SharpFileOpenDialog.ShowSingleSelect(IntPtr.Zero, "Valtra IMU+GNSS data");
        }
    }

    const string SIM_LABEL = "sim";
}
