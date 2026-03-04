using MotionSystems;

namespace ValtraIMU.Services;

/// <summary>
/// Implements dummy data feeding to MotionPlatform using ForceSeatMI.
/// Uses sine functions to generate angular velocity and linear acceleration data.
/// </summary>
internal class DummyDataFeeder : DataFeeder
{
    public DummyDataFeeder(ForceSeatMI_NET8 mi) : base(mi)
    {
        _telemetry = FSMI_TelemetryACE.Prepare();
    }

    // Internal

    const int INTERVAL = 4;

    private FSMI_TelemetryACE _telemetry;

    /// <summary>
    /// Sends the fake telemetry data.
    /// </summary>
    /// <returns>always true.</returns>
    protected override bool SendData()
    {
        _telemetry.state = FSMI_State.NO_PAUSE;

        _telemetry.bodyAngularVelocity[0].yaw = 0;
        _telemetry.bodyAngularVelocity[0].pitch = 0;
        _telemetry.bodyAngularVelocity[0].roll = (float)CalcSin(_nextSampleTimestamp, 20000, 0.5/*hz*/);
        _telemetry.bodyLinearAcceleration[0].forward = 0;
        _telemetry.bodyLinearAcceleration[0].upward = 0;
        _telemetry.bodyLinearAcceleration[0].right = (float)CalcSin(_nextSampleTimestamp, 10, 0.7/*hz*/);

        _mi.SendTelemetryACE(ref _telemetry);

        _nextSampleTimestamp += INTERVAL;

        return true;
    }

    private static double CalcSin(double t_ms, double amplitude, double hz)
    {
        return amplitude * Math.Sin((2.0 / 1000.0 * Math.PI) * hz * t_ms);
    }
}
