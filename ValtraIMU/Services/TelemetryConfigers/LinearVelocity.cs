using MotionSystems;

namespace ValtraIMU.TelemetryConfigers;

internal class LinearVelocity(int interval) : ITelemetryConfiger
{
    public void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] values)
    {
        var value = values[0];
        var acceleration = (value - _speed) / _interval;

        if (settings.IsVerbose && !settings.IsDebugMode)
            Console.WriteLine($"LinearVelocity.{settings.Axis.Value}: {value:F4} m/s, LinearAcceleration.{settings.Axis.Value}: {acceleration:F4} m/s²");

        ref FSMI_TelemetryRUF linAccel = ref telemetry.bodyLinearAcceleration[0];
        ref FSMI_TelemetryRUF linVelocity = ref telemetry.bodyLinearVelocity[0];
        switch (settings.Axis.Value)
        {
            case Axis.Right:
                linAccel.right = (float)acceleration;
                linVelocity.right = (float)value;
                break;
            case Axis.Forward:
                linAccel.forward = (float)acceleration;
                linVelocity.forward = (float)value;
                break;
            case Axis.Upward:
                linAccel.upward = (float)acceleration;
                linVelocity.upward = (float)value;
                break;
        }

        _speed = value;
    }

    #region Internal

    readonly double _interval = (double)interval / 1000;

    double _speed = 0;

    #endregion
}
