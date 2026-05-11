namespace ValtraMPC.Models;

internal interface IRecord
{
    /// <summary>milliseconds since the start of the session</summary>
    long Timestamp { get; init; }
}
