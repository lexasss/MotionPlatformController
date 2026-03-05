namespace ValtraIMU.Services;

interface IDataProvider<T> : IEnumerator<T>
{
    /// <summary>
    /// An alternative way to provide data.
    /// Note that this method is not required to be implemented, and the default implementation will return null.
    /// </summary>
    /// <param name="timestamp">ms</param>
    /// <param name="value">result</param>
    /// <returns>false if no data, true otherwise</returns>
    bool Get(long timestamp, out T? value);
}