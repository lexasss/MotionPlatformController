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

    [CommandOption("--sfx")]
    [Description("SFX amplitude (float), frequency (byte) and optionally areas (fr,fl,rr,rl) separated by comma")]
    [DefaultValue(null)]
    public string? SFX { get; set; }

    [CommandOption("-s|--skip <NUMBER>")]
    [Description("Skip rate for IMU data")]
    [DefaultValue(1)]
    public int SkipRate { get; set; }

    [CommandOption("-b|--broadcast <DATATYPE>")]
    [Description("Data type to broadcast")]
    public FlagValue<BroadcastDataType> BroadcastDataType { get; set; } = new FlagValue<BroadcastDataType>();

    [CommandOption("-v|--verbose")]
    [Description("Debug info is printed in the verbose mode.")]
    [DefaultValue(false)]
    public bool IsVerbose { get; set; }

    [CommandOption("-d|--debug")]
    [Description("Sets to the debug mode.")]
    [DefaultValue(false)]
    public bool IsDebugMode { get; set; }

    public static int Interval => 4;   // ms, corresponds to 250 Hz

    public void Resolve(Program.ContextArgs? context)
    {
        Filename.Value = Filename.IsSet ? Filename.Value : (AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select IMU data source:")
                    .AddChoices([
                        "1. Recorded file",
                        "2. Simulated data"
                    ])
                ).StartsWith('2') ? SIM_LABEL : null);

        if (Filename.Value == SIM_LABEL)
        {
            SimulationMode.Value = SimulationMode.IsSet ? SimulationMode.Value : AnsiConsole.Prompt(
                new SelectionPrompt<SimulationMode>()
                    .Title("Select simulation mode:")
                    .AddChoices(Enum.GetValues<SimulationMode>()));

            if (SimulationMode.Value == ValtraIMU.SimulationMode.SineAcceleration ||
                SimulationMode.Value == ValtraIMU.SimulationMode.MovePulse ||
                SimulationMode.Value == ValtraIMU.SimulationMode.Sway)
            {
                Axis.Value = Axis.IsSet ? Axis.Value : AnsiConsole.Prompt(
                    new SelectionPrompt<Axis>()
                        .Title("Select axis used in simulation mode:")
                        .AddChoices(Enum.GetValues<Axis>()));
            }

            string units = SimulationMode.Value switch
            {
                ValtraIMU.SimulationMode.SineAcceleration => "m/s²",
                ValtraIMU.SimulationMode.MovePulse => "m/s",
                ValtraIMU.SimulationMode.Sway or ValtraIMU.SimulationMode.CircluarSway => "deg",
                ValtraIMU.SimulationMode.SideSwayPlusForward or ValtraIMU.SimulationMode.SideSwayPlusUpward => "m/s²",
                _ => "-"
            };
            Amplitude = Amplitude != 1 ? Amplitude : AnsiConsole.Ask($"Amplitude ({units}):", context?.Amplitude ?? Amplitude);

            if (SimulationMode.Value == ValtraIMU.SimulationMode.SineAcceleration || 
                SimulationMode.Value == ValtraIMU.SimulationMode.Sway ||
                SimulationMode.Value == ValtraIMU.SimulationMode.CircluarSway ||
                SimulationMode.Value == ValtraIMU.SimulationMode.SideSwayPlusForward ||
                SimulationMode.Value == ValtraIMU.SimulationMode.SideSwayPlusUpward)
            {
                Frequency = Frequency != 0.5 ? Frequency : AnsiConsole.Ask("Frequency:", context?.Frequency ?? Frequency);
            }
        }
        else if (!File.Exists(Filename.Value))
        {
            Filename.Value = SharpFileOpenDialog.ShowSingleSelect(IntPtr.Zero, "Valtra IMU+GNSS data");
            Amplitude = Amplitude != 1 ? Amplitude : AnsiConsole.Ask("Amplitude:", context?.Amplitude ?? Amplitude);
        }

        IsDebugMode = context?.IsDebugMode ?? IsDebugMode;
        IsVerbose = context?.IsVerbose ?? IsVerbose;

        if (Filename.Value == SIM_LABEL &&
            SimulationMode.Value == ValtraIMU.SimulationMode.SineAcceleration ||
            SimulationMode.Value == ValtraIMU.SimulationMode.SideSwayPlusForward ||
            SimulationMode.Value == ValtraIMU.SimulationMode.SideSwayPlusUpward)
        {
            SFX = SFX != null ? SFX : AnsiConsole.Ask<string?>("Tremor parameters (ampl[[,freq[[,fr,fl,rr,rl]]]]):", null);
        }

        BroadcastDataType.Value = BroadcastDataType.IsSet ? BroadcastDataType.Value : AnsiConsole.Prompt(
            new SelectionPrompt<BroadcastDataType>()
                .Title("Select broadcast data:")
                .AddChoices(Enum.GetValues<BroadcastDataType>()));
    }

    const string SIM_LABEL = "sim";
}
