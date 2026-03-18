using MotionSystems;

namespace ValtraIMU.TelemetryConfigers;

internal class LinearAcceleration : ITelemetryConfiger
{
    public void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] values)
    {
        var value = values[0];

        if (settings.IsVerbose && !settings.IsDebugMode)
            Console.WriteLine($"LinearAcceleleration.{settings.Axis.Value}: {value:F4} m/s²");

        ref FSMI_TelemetryRUF linAccel = ref telemetry.bodyLinearAcceleration[0];
        switch (settings.Axis.Value)
        {
            case Axis.Right:
                linAccel.right = (float)value;
                break;
            case Axis.Forward:
                linAccel.forward = (float)value;
                break;
            case Axis.Upward:
                linAccel.upward = (float)value;
                break;
        }
    }
}
