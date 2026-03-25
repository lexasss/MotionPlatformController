using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ValtraIMU;

public class FriendlyEnumConverter(Type type) : EnumConverter(type)
{
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return value == null ? string.Empty :
                Regex.Replace(
                        value.ToString() ?? "",
                        "(?<=[^A-Z])([A-Z])", " $1",
                        RegexOptions.Compiled
                    ).Trim();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
