using System.Collections;

namespace ValtraIMU.DataProviders;

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
