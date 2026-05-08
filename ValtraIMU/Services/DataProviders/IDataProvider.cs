namespace ValtraIMU.DataProviders;

interface IDataProvider<T> : IEnumerator<T>
{
    /// <summary>
    /// An alternative way to provide data when data computation is based on the timestamp.
    /// </summary>
    /// <param name="timestamp">ms</param>
    /// <param name="value">result</param>
    /// <returns>false if no data, true otherwise</returns>
    bool Get(long timestamp, out T? value);
}