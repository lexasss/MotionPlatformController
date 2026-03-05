namespace ValtraIMU.Services;

internal class SinDataProvider : IDataProvider<double>
{
    public double Current => _nextData ?? throw new Exception();

    public double Amplitude { get; set; } = 1;
    public double Frequency { get; set; } = 0.5;

    public SinDataProvider(Settings settings)
    {
        Amplitude = settings.Amplitude;
        Frequency = settings.Frequency;
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
        result = Amplitude * Math.Sin(CYCLE_MS * Frequency * timestamp);
        _nextData = result;
        return true;
    }

    #endregion


    // Internal

    const double CYCLE_MS = 2.0 * Math.PI / 1000.0;

    object System.Collections.IEnumerator.Current => Current;

    double? _nextData = null;
}
