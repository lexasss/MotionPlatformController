using MotionSystems;

namespace ValtraIMU.Services;

/// <summary>
/// Implements dummy data feeding to MotionPlatform using ForceSeatMI.
/// In SineWave mode, uses sine functions to generate angular velocity and linear acceleration data.
/// In MoveForward mode, simulates a vehicle accelerating to a certain speed, maintaining it, and then decelerating to a stop.
/// In SwayForward mode, simulates a vehicle swaying up/down by generating pitch angular velocity data.
/// </summary>
internal class DummyDataFeeder : DataFeeder
{
    public DummyDataFeeder(ForceSeatMI_NET8 mi, Settings settings) : base(mi, settings)
    {
        _mode = settings.SimulationMode;
        _telemetry = FSMI_TelemetryACE.Prepare();

        if (_mode == SimulationMode.SineWave)
        {
            _dataProvider = new SinDataProvider(settings);
            _extraDataProvider = new SinDataProvider(settings)
            {
                Amplitude = 0.5,  // rad
                Frequency = 0.5
            };
        }
        else if (_mode == SimulationMode.MoveForward)
        {
            _dataProvider = new PulseDataProvider(settings);
        }
        else if (_mode == SimulationMode.SwayForward)
        {
            _dataProvider = new SinDataProvider(settings);
        }
        else
        {
            throw new NotImplementedException($"Simulation mode '{_mode}' not yet implemented.");
        }
    }

    /// <summary>
    /// Sends simulated telemetry data.
    /// </summary>
    /// <returns>true if there was data to send, false otherwise.</returns>
    protected override bool SendData()
    {
        _telemetry.state = FSMI_State.NO_PAUSE;

        if (!_dataProvider.Get(_nextSampleTimestamp, out double value))
            return false;

        if (_mode == SimulationMode.SineWave)
        {
            double angVelRoll = 0;
            _extraDataProvider?.Get(_nextSampleTimestamp, out angVelRoll);

            if (!_settings.IsVerbose)
                Console.WriteLine($"AngularVelocity.Roll: {angVelRoll:F4} rad/s, LinearAcceleleration.Right: {value:F4} m/s²");

            _telemetry.bodyAngularVelocity[0].roll = (float)angVelRoll;
            _telemetry.bodyLinearAcceleration[0].right = (float)value;
        }
        else if (_mode == SimulationMode.MoveForward)
        {
            var acceleration = (value - _speed) / ((double)INTERVAL / 1000);

            if (!_settings.IsVerbose)
                Console.WriteLine($"LinearVelocit.Forward: {value:F4} m/s, LinearAcceleration.Forward: {acceleration:F4} m/s²");

            _telemetry.bodyLinearAcceleration[0].forward = (float)acceleration;
            _telemetry.bodyLinearVelocity[0].forward = (float)value;

            _speed = value;
        }
        else if (_mode == SimulationMode.SwayForward)
        {
            if (!_settings.IsVerbose)
                Console.WriteLine($"AngularVelocity.Pitch: {value:F4} rad/s");

            _telemetry.bodyAngularVelocity[0].pitch = (float)value;
        }
        else if (_mode == SimulationMode.SwayAside)
        {
            if (!_settings.IsVerbose)
                Console.WriteLine($"AngularVelocity.Roll: {value:F4} rad/s");

            _telemetry.bodyAngularVelocity[0].roll = (float)value;
        }
        else
        {
            throw new NotImplementedException($"Simulation mode '{_mode}' not yet implemented.");
        }

        _mi.SendTelemetryACE(ref _telemetry);

        _nextSampleTimestamp += INTERVAL;

        return true;
    }

    #region Internal

    const int INTERVAL = 4;

    readonly SimulationMode _mode;
    readonly IDataProvider<double> _dataProvider;
    readonly SinDataProvider? _extraDataProvider;

    FSMI_TelemetryACE _telemetry;

    double _speed = 0;

    #endregion
}
