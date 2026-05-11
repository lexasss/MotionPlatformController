using System.ComponentModel;

namespace ValtraMPC;

[TypeConverter(typeof(FriendlyEnumConverter))]
internal enum BroadcastDataType
{
    Telemetry,
    PlatformInfo
}
