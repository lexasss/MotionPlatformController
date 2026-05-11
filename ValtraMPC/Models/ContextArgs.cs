namespace ValtraMPC.Models;

internal record class ContextArgs(
    double? Amplitude = null,
    double? Frequency = null,
    bool? IsDebugMode = null,
    bool? IsVerbose = null)
{
    public static ContextArgs FromSettings(Settings settings) =>
        new(settings.Amplitude, settings.Frequency, settings.IsDebugMode, settings.IsVerbose);
}
