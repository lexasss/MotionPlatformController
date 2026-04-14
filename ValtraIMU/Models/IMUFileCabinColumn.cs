using System.Text.RegularExpressions;

namespace ValtraIMU.Models;

internal partial record class IMUFileCabinColumn(
    string Name,
    string Unit,
    string? Description
)
{
    public static IMUFileCabinColumn[] GetColumns(string text)
    {
        var columnRegex = ColumnRegex();
        var removeSpacesRegex = RemoveSpacesRegex();

        return text
            .Split('\t')
            .Select(col => columnRegex.Match(col))
            .Select(m => new IMUFileCabinColumn(
                Name: removeSpacesRegex.Replace(m.Groups[1].Value, ""),
                Unit: m.Groups[4].Success ? m.Groups[4].Value : "",
                Description: m.Groups[3].Success ? m.Groups[3].Value : null
            )).ToArray();
    }

    [GeneratedRegex(@"(.+?)(\s-\s(.+))?\s\[(\S*)\]")]
    private static partial Regex ColumnRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex RemoveSpacesRegex();
}

