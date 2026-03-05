namespace ValtraIMU.Services;

internal class PulseDataProvider : IDataProvider<double>
{
    public double Current => _nextData ?? throw new Exception();

    public double Amplitude { get; set; } = 1;
    public double Frequency { get; set; } = 1.0 / 4;

    public PulseDataProvider(Settings settings)
    {
        Amplitude = settings.Amplitude;
    }

    #region IDataProvider implementation

    public void Dispose() { }

    public bool MoveNext() => Get(0, out _);

    public void Reset() 
    {
        _nextData = null;
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


    // Internal

    const double CYCLE_MS = 2.0 * Math.PI / 1000.0;

    object System.Collections.IEnumerator.Current => Current;

    double? _nextData = null;
}
