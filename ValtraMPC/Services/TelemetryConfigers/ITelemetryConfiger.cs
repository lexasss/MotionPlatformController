using MotionSystems;

namespace ValtraMPC.TelemetryConfigers;

internal interface ITelemetryConfiger
{
    void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] values);
}
