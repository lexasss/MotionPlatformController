using MotionSystems;

namespace ValtraIMU.Feeders;

/// <summary>
/// Implements dummy data feeding to MotionPlatform using ForceSeatMI.
/// In SineWave mode, uses sine functions to generate angular velocity and linear acceleration data.
/// In MoveForward mode, simulates a vehicle accelerating to a certain speed, maintaining it, and then decelerating to a stop.
/// In SwayForward mode, simulates a vehicle swaying up/down by generating pitch angular velocity data.
/// </summary>
internal class DummyFeeder : DataFeeder
{
    public DummyFeeder(ForceSeatMI_NET8 mi, Settings settings) : base(mi, settings)
    {
        var mode = settings.SimulationMode.Value;

        if (mode == SimulationMode.SineAcceleration)
        {
            _dataProviders = [new DataProviders.Sinus(settings)];
            _telemetryConfiger = new TelemetryConfigers.LinearAcceleration();
        }
        else if (mode == SimulationMode.MovePulse)
        {
            _dataProviders = [new DataProviders.Pulse(settings)];
            _telemetryConfiger = new TelemetryConfigers.LinearVelocity(Settings.Interval);
        }
        else if (mode == SimulationMode.Sway)
        {
            var constProvider = new DataProviders.Const(0);
            var sinusProvider = new DataProviders.Sinus(settings)
            {
                Amplitude = settings.Amplitude,  // rad
                Frequency = settings.Frequency
            };

            if (_settings.Axis.Value == Axis.Right)
                _dataProviders = [sinusProvider, constProvider];
            else
                _dataProviders = [constProvider, sinusProvider];

            _telemetryConfiger = new TelemetryConfigers.Orientation();
        }
        else if (mode == SimulationMode.CircluarSway)
        {
            _telemetryConfiger = new TelemetryConfigers.Orientation();
            _dataProviders = [
                new DataProviders.Sinus(settings), 
                new DataProviders.Sinus(settings) { InitialPhase = Math.PI / 2 }
            ];
        }
        else if (mode == SimulationMode.SideSwayPlusForward || mode == SimulationMode.SideSwayPlusUpward)
        {
            _telemetryConfiger = new TelemetryConfigers.LinearAcceleration();
            _dataProviders = [
                new DataProviders.Sinus(settings),
                new DataProviders.Sinus(settings) { InitialPhase = Math.PI / 2 }
            ];
        }
        else
        {
            throw new NotImplementedException($"Simulation mode '{mode}' not yet implemented.");
        }
    }

    /// <summary>
    /// Sends simulated telemetry data.
    /// </summary>
    /// <returns>true if there was data to send, false otherwise.</returns>
    protected override bool PrepareTelemetry()
    {
        _telemetry.state = FSMI_State.NO_PAUSE;

        double[] values;
        try
        {
            values = _dataProviders.Select(dp =>
            {
                if (!dp.Get(_nextRecordTimestamp, out double value))
                    throw new Exception($"Data provider failed to provide data for timestamp {_nextRecordTimestamp}.");
                return value;
            }).ToArray();
        }
        catch
        {
            return false;
        }

        _telemetryConfiger.Config(ref _telemetry, _settings, values);

        _nextRecordTimestamp += Settings.Interval;

        return true;
    }

    #region Internal

    readonly DataProviders.IDataProvider<double>[] _dataProviders;
    readonly TelemetryConfigers.ITelemetryConfiger _telemetryConfiger;

    #endregion
}
