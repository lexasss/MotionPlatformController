using MotionSystems;

namespace ValtraIMU.TelemetryConfigers;

internal class Orientation : ITelemetryConfiger
{
    public void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] value)
    {
        var pitch = value[0] / 180 * Math.PI;
        var roll = value[1] / 180 * Math.PI;
        if (settings.IsVerbose && !settings.IsDebugMode)
        {
            Console.CursorLeft = 0;
            Console.Write($"BodyPitch: {pitch:F2} deg, BodyRoll: {roll:F2} deg");
        }

        telemetry.bodyPitch = (float)pitch;
        telemetry.bodyRoll = (float)roll;
    }
}
