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

        var acceleration = record.Acceleration; // Models.Acceleration2.FromArray(_accelerationFilter.Process(record.Acceleration.ToArray()));
        if (_accelVertOffset == 0)  // looks like this is not needed as MotionPlanform ignores high values, but just in case (it is around "g") ...
            _accelVertOffset = acceleration.Vertical;

        var amplitude = _settings.Amplitude;

        _telemetry.bodyAngularVelocity[0].yaw = (float)(angVelAsRadians.Yaw * amplitude);
        _telemetry.bodyAngularVelocity[0].pitch = (float)(angVelAsRadians.Pitch * amplitude);
        _telemetry.bodyAngularVelocity[0].roll = (float)(angVelAsRadians.Roll * amplitude);
        _telemetry.bodyLinearAcceleration[0].forward = (float)(acceleration.Longitudinal * amplitude);
        _telemetry.bodyLinearAcceleration[0].upward = (float)((acceleration.Vertical - _accelVertOffset) * amplitude);
        _telemetry.bodyLinearAcceleration[0].right = (float)(acceleration.Lateral * amplitude);

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
    //readonly Services.ButterworthFilter _accelerationFilter = new(10, 1, 3, Services.ButterworthFilter.FilterType.HighPass, 0.01);

    double _accelVertOffset = 0;

    #endregion
}
