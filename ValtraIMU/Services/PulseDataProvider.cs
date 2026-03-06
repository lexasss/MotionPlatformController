namespace ValtraIMU.Services;

internal class PulseDataProvider(Settings settings) : IDataProvider<double>
{
    public double Current => _nextData ?? throw new Exception();

    public double Amplitude { get; set; } = settings.Amplitude;
    public double Frequency { get; set; } = 1.0 / 4;

    #region IDataProvider implementation

    public void Dispose() { }

    public bool MoveNext() => Get(_timestamp += INTERVAL, out _);

    public void Reset() 
    {
        _nextData = null;
        _timestamp = 0;
    }

    public bool Get(long timestamp, out double result)
    {
        double? value = null;

        if (timestamp < 500)
        {
            value = 0;
        }
        else if (timestamp <= 2500)
        {
            value = (Math.Sin(CYCLE_MS * Frequency * (timestamp - 500) - Math.PI / 2) + 1) / 2;
        }
        else if (timestamp <= 5000)
        {
            value = 1;
        }
        else if (timestamp <= 7000)
        {
            value = (Math.Sin(CYCLE_MS * Frequency * (timestamp - 5000) + Math.PI / 2) + 1) / 2;
        }
        else if (timestamp <= 7500)
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
    const int INTERVAL = 4;

    object System.Collections.IEnumerator.Current => Current;

    double? _nextData = null;
    long _timestamp = 0;

    #endregion
}
