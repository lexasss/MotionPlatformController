namespace ValtraIMU.DataProviders;

internal class Pulse(Settings settings) : IDataProvider<double>
{
    public double Current => _nextData ?? throw new Exception();

    public double Amplitude { get; set; } = settings.Amplitude;
    public double Frequency { get; set; } = 1.0 / 4;

    #region IDataProvider implementation

    public void Dispose() { }

    public bool MoveNext() => Get(_timestamp += _interval, out _);

    public void Reset() 
    {
        _nextData = null;
        _timestamp = 0;
    }

    public bool Get(long timestamp, out double result)
    {
        double? value = null;

        if (timestamp < 500)            // 0.5 sec: initial delay before the pulse starts
        {
            value = 0;
        }
        else if (timestamp <= 2500)     // 2 sec: ramp up
        {
            value = (Math.Sin(CYCLE_MS * Frequency * (timestamp - 500) - Math.PI / 2) + 1) / 2;
        }
        else if (timestamp <= 5000)     // 2.5 sec: steady pulse
        {
            value = 1;
        }
        else if (timestamp <= 7000)     // 2 sec: ramp down
        {
            value = (Math.Sin(CYCLE_MS * Frequency * (timestamp - 5000) + Math.PI / 2) + 1) / 2;
        }
        else if (timestamp <= 10000)    // 3 sec: interval to settle down
        {
            value = 0;
        }

        _nextData = value != null ? value * Amplitude : null;
        result = _nextData ?? default;

        return _nextData != null;
    }

    #endregion

    #region Internal

    const double CYCLE_MS = 2.0 * Math.PI / 1000.0;
    readonly int _interval = Settings.Interval;

    object System.Collections.IEnumerator.Current => Current;

    double? _nextData = null;
    long _timestamp = 0;

    #endregion
}
