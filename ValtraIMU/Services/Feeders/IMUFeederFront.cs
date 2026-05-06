using MotionSystems;
using System.Diagnostics;

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
            Console.Write($"[{record.Timestamp}] ");
            Console.Write($"Vel: yaw {angVelAsRadians.Z,8:F4}, pitch {angVelAsRadians.X,8:F4}, roll {angVelAsRadians.Y,8:F4} | ");
            Console.Write($"Acc: f {record.BodyAcceleration.Y,8:F4}, u {record.BodyAcceleration.Z,8:F4}, r {record.BodyAcceleration.X,8:F4} | ");
            Console.Write($"Ort: pitch {orientAsRadians.Pitch,8:F4}, roll {orientAsRadians.Roll,8:F4}");
            Console.CursorTop = top;
        }

        _nextRecordTimestamp = record.Timestamp;

        return true;
    }

    Stopwatch stopWatch = Stopwatch.StartNew();

    protected override void PreparePosition()
    {
        var record = _dataProvider.Current; /// no need to move forward, as <see href="PrepareTelemetry"/> is call always prior to this method

        if (_msTimestamp == 0)
        {
            _msTimestamp = record.Timestamp;
            return;
        }

        var amplitude = _settings.Amplitude;

        var ts = record.Timestamp;
        var intervalSeconds = 0.001 * (ts - _msTimestamp);
        _msTimestamp = ts;

        double[] velocities = [
            amplitude * record.BodyAcceleration.X * intervalSeconds,
            amplitude * record.BodyAcceleration.Y * intervalSeconds,
            amplitude * record.BodyAcceleration.Z * intervalSeconds,
        ];
        _swayPos += _swayVel * intervalSeconds + velocities[0] * intervalSeconds * 0.5;
        _surgePos += _surgeVel * intervalSeconds + velocities[1] * intervalSeconds * 0.5;
        _heavePos += _heaveVel * intervalSeconds + velocities[2] * intervalSeconds * 0.5;

        _swayVel += velocities[0];
        _surgeVel += velocities[1];
        _heaveVel += velocities[2];

        _position.state = FSMI_State.NO_PAUSE;

        _position.sway = (float)(_swayPos * 1000);      // m => mm
        _position.surge = (float)(_surgePos * 1000);
        _position.heave = (float)(_heavePos * 1000);

        _position.pitch = _telemetry.bodyPitch;
        _position.roll = _telemetry.bodyRoll;

        static double CalcSin(double t_ms, double amplitude, double hz)
        {
            return amplitude * Math.Sin((2.0 / 1000.0 * Math.PI) * hz * t_ms);
        }

        static double Deg2Rad(double deg)
        {
            return deg * Math.PI / 180;
        }

        _position.pitch = 0;
        _position.roll = (float)CalcSin(stopWatch.ElapsedMilliseconds, Deg2Rad(5), 0.5/*hz*/);
        _position.yaw = 0;
        _position.sway = 0;
        _position.surge = 0;
        _position.heave = 0;


        if (_settings.IsVerbose && !_settings.IsDebugMode && _nextRecordTimestamp % 100 == 0)
        {
            int top = Console.CursorTop;
            Console.CursorTop += 1;
            Console.CursorLeft = 0;
            Console.Write($"[{record.Timestamp}] ");
            Console.Write($"sway {_position.sway,8:F4}, surge {_position.surge,8:F4}, heave {_position.heave,8:F4} | vsway {_swayVel,8:F4}, vsurge {_surgeVel,8:F4}, vheave {_heaveVel,8:F4}");
            Console.CursorTop = top;
        }
    }

    #region Internal

    readonly DataProviders.IMUFileFront _dataProvider = dataProvider;

    long _msTimestamp = 0;

    double _swayPos = 0;
    double _surgePos = 0;
    double _heavePos = 0;
    double _swayVel = 0;
    double _surgeVel = 0;
    double _heaveVel = 0;

    #endregion
}
