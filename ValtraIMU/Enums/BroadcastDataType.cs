using System.ComponentModel;

namespace ValtraIMU;

[TypeConverter(typeof(FriendlyEnumConverter))]
internal enum BroadcastDataType
{
    Telemetry,
    PlatformInfo
}
