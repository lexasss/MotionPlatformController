using MotionSystems;

namespace ValtraMPC.TelemetryConfigers;

/// <summary>
/// Used by Dummy feeder to configure telemetry data to be sent to ForceSeatPM, based on the settings and the generated values.
/// </summary>
internal interface ITelemetryConfiger
{
    void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] values);
}
