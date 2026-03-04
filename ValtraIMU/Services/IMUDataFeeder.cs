using MotionSystems;

namespace ValtraIMU.Services;

/// <summary>
/// Implements data feeding to MotionPlatform using ForceSeatMI.
/// The IMU+GNSS data can be obtained using <see cref="IMUDataProvider">IMUDataProvider</see>.
/// </summary>
internal class IMUDataFeeder : DataFeeder
{
    public IMUDataFeeder(ForceSeatMI_NET8 mi, Models.IMUData[] data) : base(mi)
    {
        _dataEnumerator = (data as IEnumerable<Models.IMUData>).GetEnumerator();
        _telemetry = FSMI_TelemetryACE.Prepare();
    }

    // Internal

    /// <summary>
    /// Implements the data conversion between IMU+GNSS and MotionPlatform formats and sending logic.
    /// </summary>
    /// <returns>true if the telemetry data was sent successfully; otherwise, false.</returns>
    protected override bool SendData()
    {
        if (!_dataEnumerator.MoveNext())
            return false;

        _telemetry.state = FSMI_State.NO_PAUSE;

        var item = _dataEnumerator.Current;

        _telemetry.bodyAngularVelocity[0].yaw = item.AngularVelocity.Z;
        _telemetry.bodyAngularVelocity[0].pitch = item.AngularVelocity.X;
        _telemetry.bodyAngularVelocity[0].roll = item.AngularVelocity.Y;
        _telemetry.bodyLinearAcceleration[0].forward = item.BodyAcceleration.Y;
        _telemetry.bodyLinearAcceleration[0].upward = item.BodyAcceleration.Z;
        _telemetry.bodyLinearAcceleration[0].right = item.BodyAcceleration.X;

        _telemetry.bodyPitch = item.Orientation.Pitch;
        _telemetry.bodyRoll = item.Orientation.Roll;

        _mi.SendTelemetryACE(ref _telemetry);

        _nextSampleTimestamp = (int)(1000 * item.GPSTime);

        return true;
    }

    private IEnumerator<Models.IMUData> _dataEnumerator;
    private FSMI_TelemetryACE _telemetry;
}
