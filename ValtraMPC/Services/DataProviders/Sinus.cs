namespace ValtraMPC.DataProviders;

/// <summary>
/// Creates a sinusoidal pattern of data with given amplitude, frequency and phase.
/// </summary>
/// <param name="settings">settings object</param>
/// <param name="initialPhase">sine initial phase</param>
internal class Sinus(Settings settings, double initialPhase = 0) : IDataProvider<double>
{
    /// <summary>
    /// IDataProvider implementation
    /// </summary>
    public double Current => _nextValue ?? throw new Exception();

    public double Amplitude { get; set; } = settings.Amplitude;
    public double Frequency { get; set; } = settings.Frequency;
    public double InitialPhase { get; set; } = initialPhase;

    #region IDataProvider implementation

    public void Dispose() { }

    public bool MoveNext() => Get(_timestamp += _interval, out _);

    public void Reset()
    {
        _nextValue = null;
        _timestamp = 0;
    }

    public bool Get(long timestamp, out double result)
    {
        result = Amplitude * Math.Sin(InitialPhase + CYCLE_MS * Frequency * timestamp);
        _nextValue = result;
        return true;
    }

    #endregion

    #region Internal

    const double CYCLE_MS = 2.0 * Math.PI / 1000.0;

    readonly int _interval = Settings.Interval;

    object System.Collections.IEnumerator.Current => Current;

    double? _nextValue = null;
    long _timestamp = 0;

    #endregion
}
