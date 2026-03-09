using MotionSystems;

namespace ValtraIMU.TelemetryConfigers;

internal interface ITelemetryConfiger
{
    void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] values);
}
