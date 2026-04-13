using MotionSystems;

namespace ValtraIMU.Feeders;

/// <summary>
/// Implements data feeding to MotionPlatform using ForceSeatMI.
/// The IMU data can be obtained using <see cref="DataProviders.IMUFileCabin">.
/// </summary>
internal class IMUCabin(ForceSeatMI_NET8 mi, Settings settings, DataProviders.IMUFileCabin dataProvider) : DataFeeder(mi, settings)
{
    /// <summary>
    /// Implements the data conversion between IMU and MotionPlatform formats and sending logic.
    /// </summary>
    /// <returns>true if the telemetry data was sent successfully; otherwise, false.</returns>
    protected override bool PrepareTelemetry()
    {
        if (!_dataProvider.MoveNext())
            return false;

        _telemetry.state = FSMI_State.NO_PAUSE;

        var record = _dataProvider.Current;

        var angVelAsRadians = record.AngularVelocity.ToRadians();
        var orientAsRadians = record.Orientation.ToRadians();

        var amplitude = _settings.Amplitude;

        _telemetry.bodyAngularVelocity[0].yaw = (float)(angVelAsRadians.Yaw * amplitude);
        _telemetry.bodyAngularVelocity[0].pitch = (float)(angVelAsRadians.Pitch * amplitude);
        _telemetry.bodyAngularVelocity[0].roll = (float)(angVelAsRadians.Roll * amplitude);
        _telemetry.bodyLinearAcceleration[0].forward = (float)(record.Acceleration.Longitudinal * amplitude);
        _telemetry.bodyLinearAcceleration[0].upward = (float)(record.Acceleration.Vertical * amplitude);
        _telemetry.bodyLinearAcceleration[0].right = (float)(record.Acceleration.Lateral * amplitude);

        _telemetry.bodyPitch = (float)orientAsRadians.Pitch;
        _telemetry.bodyRoll = (float)orientAsRadians.Roll;

        if (_settings.IsVerbose && !_settings.IsDebugMode && _nextRecordTimestamp % 100 == 0)
        {
            Console.CursorLeft = 0;
            Console.Write($"[{record.Time:F3}] ");
            Console.Write($"Vel: yaw {angVelAsRadians.Yaw,8:F4}, pitch {angVelAsRadians.Pitch,8:F4}, roll {angVelAsRadians.Roll,8:F4} | ");
            Console.Write($"Acc: f {record.Acceleration.Longitudinal,8:F4}, u {record.Acceleration.Vertical,8:F4}, r {record.Acceleration.Lateral,8:F4} | ");
            Console.Write($"Ort: pitch {orientAsRadians.Pitch,8:F4}, roll {orientAsRadians.Roll,8:F4}");
        }

        _nextRecordTimestamp = record.Time;

        return true;
    }

    #region Internal

    readonly DataProviders.IMUFileCabin _dataProvider = dataProvider;

    #endregion
}
