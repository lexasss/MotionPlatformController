using System.Collections;

namespace ValtraMPC.DataProviders;

/// <summary>
/// Provides a data provider that always returns a constant double value, regardless of input or iteration state.
/// </summary>
/// <param name="constValue">The constant value to be returned.</param>
internal class Const(double constValue) : IDataProvider<double>
{
    public double Current => constValue;

    object IEnumerator.Current => Current;

    public void Dispose() { }

    public bool Get(long timestamp, out double value)
    {
        value = constValue;
        return true;
    }

    public bool MoveNext() => true;

    public void Reset() { }
}
