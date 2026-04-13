namespace ValtraIMU.Models;

internal interface IRecord
{
    /// <summary>milliseconds since the start of the session</summary>
    long Time { get; init; }
}
