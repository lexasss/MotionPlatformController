using MotionSystems;

namespace ValtraIMU.Services;

/// <summary>
/// Implements data feeding to MotionPlatform using ForceSeatMI.
/// The IMU+GNSS data can be obtained using <see cref="IMUDataProvider">IMUDataProvider</see>.
/// </summary>
internal class IMUDataFeeder : DataFeeder
{
    public IMUDataFeeder(ForceSeatMI_NET8 mi, IMUDataProvider dataProvider) : base(mi)
    {
        _dataProvider = dataProvider;
        _telemetry = FSMI_TelemetryACE.Prepare();
    }

    // Internal

    /// <summary>
    /// Implements the data conversion between IMU+GNSS and MotionPlatform formats and sending logic.
    /// </summary>
    /// <returns>true if the telemetry data was sent successfully; otherwise, false.</returns>
    protected override bool SendData()
    {
        if (!_dataProvider.MoveNext())
            return false;

        _telemetry.state = FSMI_State.NO_PAUSE;

        var data = _dataProvider.Current;

        _telemetry.bodyAngularVelocity[0].yaw = (float)data.AngularVelocity.Z;
        _telemetry.bodyAngularVelocity[0].pitch = (float)data.AngularVelocity.X;
        _telemetry.bodyAngularVelocity[0].roll = (float)data.AngularVelocity.Y;
        _telemetry.bodyLinearAcceleration[0].forward = (float)data.BodyAcceleration.Y;
        _telemetry.bodyLinearAcceleration[0].upward = (float)data.BodyAcceleration.Z;
        _telemetry.bodyLinearAcceleration[0].right = (float)data.BodyAcceleration.X;

        _telemetry.bodyPitch = (float)data.Orientation.Pitch;
        _telemetry.bodyRoll = (float)data.Orientation.Roll;

        _mi.SendTelemetryACE(ref _telemetry);

        _nextSampleTimestamp = data.Time;

        return true;
    }

    private readonly IMUDataProvider _dataProvider;
    private FSMI_TelemetryACE _telemetry;
}
