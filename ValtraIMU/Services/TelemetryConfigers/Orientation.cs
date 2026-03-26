using MotionSystems;

namespace ValtraIMU.TelemetryConfigers;

internal class Orientation : ITelemetryConfiger
{
    public void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] value)
    {
        if (settings.IsVerbose && !settings.IsDebugMode)
        {
            Console.CursorLeft = 0;
            Console.Write($"BodyPitch: {value[0]:F4} rad, BodyRoll: {value[1]:F4} rad");
        }

        telemetry.bodyPitch = (float)value[0];
        telemetry.bodyRoll = (float)value[1];
    }
}
