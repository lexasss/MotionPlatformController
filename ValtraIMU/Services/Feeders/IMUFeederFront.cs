using MotionSystems;
using Spectre.Console;

namespace ValtraIMU.Feeders;

/// <summary>
/// Implements data feeding to MotionPlatform using ForceSeatMI.
/// The IMU+GNSS data can be obtained using <see cref="DataProviders.IMUFileFront">.
/// </summary>
internal class IMUFeederFront(ForceSeatMI_NET8 mi, Settings settings, DataProviders.IMUFileFront dataProvider) : DataFeeder(mi, settings)
{
    /// <summary>
    /// Implements the data conversion between IMU+GNSS and MotionPlatform formats and sending logic.
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

        _telemetry.bodyAngularVelocity[0].yaw = (float)(angVelAsRadians.Z * amplitude);
        _telemetry.bodyAngularVelocity[0].pitch = (float)(angVelAsRadians.X * amplitude);
        _telemetry.bodyAngularVelocity[0].roll = (float)(angVelAsRadians.Y * amplitude);
        _telemetry.bodyLinearAcceleration[0].forward = (float)(record.BodyAcceleration.Y * amplitude);
        _telemetry.bodyLinearAcceleration[0].upward = (float)(record.BodyAcceleration.Z * amplitude);
        _telemetry.bodyLinearAcceleration[0].right = (float)(record.BodyAcceleration.X * amplitude);

        _telemetry.bodyPitch = (float)orientAsRadians.Pitch;
        _telemetry.bodyRoll = (float)orientAsRadians.Roll;

        if (_settings.IsVerbose && !_settings.IsDebugMode && _nextRecordTimestamp % 100 == 0)
        {
            int top = Console.CursorTop;
            Console.CursorLeft = 0;
            AnsiConsole.Markup($"[yellow][[{record.Timestamp}]][/] ");
            AnsiConsole.Markup($"[yellow]Vel[/] yaw {angVelAsRadians.Z,8:F4}, pitch {angVelAsRadians.X,8:F4}, roll {angVelAsRadians.Y,8:F4} | ");
            AnsiConsole.Markup($"[yellow]Acc[/] f {record.BodyAcceleration.Y,8:F4}, u {record.BodyAcceleration.Z,8:F4}, r {record.BodyAcceleration.X,8:F4} | ");
            AnsiConsole.Markup($"[yellow]Ort[/] pitch {orientAsRadians.Pitch,8:F4}, roll {orientAsRadians.Roll,8:F4}");
            Console.CursorTop = top;
        }

        _nextRecordTimestamp = record.Timestamp;

        return true;
    }

    protected override void PreparePosition()
    {
        var record = _dataProvider.Current; /// no need to move forward, as <see cref="PrepareTelemetry"/> is called always prior to this method

        // Implements recommended position calculation, as in ForceSeatMI.pdf, chapter 8.1

        var dt = 0.001 * (record.Timestamp - _prevTimestamp);   // in seconds
        _prevTimestamp = record.Timestamp;

        double[] velocities = [
            _settings.Amplitude * record.BodyAcceleration.X * dt,
            _settings.Amplitude * record.BodyAcceleration.Y * dt,
            _settings.Amplitude * record.BodyAcceleration.Z * dt,
        ];
        _swayPos += _swayVel * dt + velocities[0] * dt * 0.5;
        _surgePos += _surgeVel * dt + velocities[1] * dt * 0.5;
        _heavePos += _heaveVel * dt + velocities[2] * dt * 0.5;

        _swayVel += velocities[0];
        _surgeVel += velocities[1];
        _heaveVel += velocities[2];

        _position.state = FSMI_State.NO_PAUSE;

        _position.sway = (float)(_swayPos * 1000);      // m => mm
        _position.surge = (float)(_surgePos * 1000);
        _position.heave = (float)(_heavePos * 1000);

        _position.pitch = _telemetry.bodyPitch;
        _position.roll = _telemetry.bodyRoll;

        if (_settings.IsVerbose && !_settings.IsDebugMode && _nextRecordTimestamp % 100 == 0)
        {
            int top = Console.CursorTop;
            Console.CursorTop += 1;
            Console.CursorLeft = 0;
            AnsiConsole.Markup($"[yellow][[{record.Timestamp}]][/] ");
            AnsiConsole.Write($"sway {_position.sway,8:F4}, surge {_position.surge,8:F4}, heave {_position.heave,8:F4} | vsway {_swayVel,8:F4}, vsurge {_surgeVel,8:F4}, vheave {_heaveVel,8:F4}");
            Console.CursorTop = top;
        }
    }

    #region Internal

    readonly DataProviders.IMUFileFront _dataProvider = dataProvider;

    long _prevTimestamp = 0;

    double _swayPos = 0;
    double _surgePos = 0;
    double _heavePos = 0;
    double _swayVel = 0;
    double _surgeVel = 0;
    double _heaveVel = 0;

    #endregion
}
