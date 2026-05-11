namespace ValtraMPC.Models;

internal partial record class IMUFileFrontColumn(
    string Name,
    string Unit
)
{
    public static IMUFileFrontColumn[] GetColumns(string[] lines)
    {
        var columns = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var units = lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (columns.Length != units.Length)
            throw new InvalidDataException("The number of columns and units do not match.");

        return columns.Zip(units, (name, unit) => new IMUFileFrontColumn(name, unit[1..^1])).ToArray();
    }
}

