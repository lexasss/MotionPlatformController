using MotionSystems;

namespace ValtraIMU.Feeders;

/// <summary>
/// Implements data feeding to MotionPlatform using ForceSeatMI.
/// The IMU+GNSS data can be obtained using <see cref="DataProviders.IMUFile">IMUDataProvider</see>.
/// </summary>
internal class IMU : DataFeeder
{
    public IMU(ForceSeatMI_NET8 mi, Settings settings, DataProviders.IMUFile dataProvider) : base(mi, settings)
    {
        _dataProvider = dataProvider;
        _telemetry = FSMI_TelemetryACE.Prepare();
    }

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

        var angVelAsRadians = data.AngularVelocity.ToRadians();
        var orientAsRadians = data.Orientation.ToRadians();

        _telemetry.bodyAngularVelocity[0].yaw = (float)(angVelAsRadians.Z * _settings.Amplitude);
        _telemetry.bodyAngularVelocity[0].pitch = (float)(angVelAsRadians.X * _settings.Amplitude);
        _telemetry.bodyAngularVelocity[0].roll = (float)(angVelAsRadians.Y * _settings.Amplitude);
        _telemetry.bodyLinearAcceleration[0].forward = (float)(data.BodyAcceleration.Y * _settings.Amplitude);
        _telemetry.bodyLinearAcceleration[0].upward = (float)(data.BodyAcceleration.Z * _settings.Amplitude);
        _telemetry.bodyLinearAcceleration[0].right = (float)(data.BodyAcceleration.X * _settings.Amplitude);

        _telemetry.bodyPitch = (float)orientAsRadians.Pitch;
        _telemetry.bodyRoll = (float)orientAsRadians.Roll;

        if (_settings.IsVerbose && !_settings.IsDebugMode && _nextSampleTimestamp % 100 == 0)
        {
            Console.CursorLeft = 0;
            Console.Write($"AngVel: yaw {angVelAsRadians.Z,8:F4}, pitch {angVelAsRadians.X,8:F4}, roll {angVelAsRadians.Y,8:F4} | ");
            Console.Write($"LinAcc: f {data.BodyAcceleration.Y,8:F4}, u {data.BodyAcceleration.Z,8:F4}, r {data.BodyAcceleration.X,8:F4} | ");
            Console.Write($"Ort: pitch {orientAsRadians.Pitch,8:F4}, roll {orientAsRadians.Roll,8:F4}");
        }

        _telemetryBroadcaster.Send(ref _telemetry);

        _mi.SendTelemetryACE(ref _telemetry);

        _nextSampleTimestamp = data.Time;

        return true;
    }

    #region Internal

    readonly DataProviders.IMUFile _dataProvider;

    FSMI_TelemetryACE _telemetry;

    #endregion
}
