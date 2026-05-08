using Spectre.Console;
using ValtraIMU.Services.DataProviders;

namespace ValtraIMU.DataProviders;

/// <summary>
/// Reads data from IMU data log file collected with cabin-located device
/// </summary>
internal class IMUFileCabin(string filename, int skipRate = 0) : IMUFile<Models.IMURecordCabin>(filename, skipRate)
{
    /// <summary>
    /// Creates an instance of <see cref="IMUFileCabin"/> based on the settings.
    /// </summary>
    /// <param name="settings">Settings</param>
    /// <returns>an instance of <see cref="IMUFileCabin"/>, or null if the filename is not set or the file does not exist</returns>
    public static IMUFileCabin? Create(ref Settings settings)
    {
        IMUFileCabin? result = null;

        var filename = settings.Filename.Value;
        if (File.Exists(filename))
        {
            AnsiConsole.Write($"Loading as IMU Cabin...  ");
            try
            {
                result = new IMUFileCabin(filename, settings.SkipRate);
                AnsiConsole.MarkupLine("[green]done[/].");
            }
            catch { AnsiConsole.MarkupLine("[red]failed[/]."); }
        }

        return result;
    }

    public override bool Get(long timestamp, out Models.IMURecordCabin? record)
    {
        bool result;
        while (result = MoveNext())
        {
            if (Current.Timestamp >= timestamp)
                break;
        }
        record = result ? Current : null;
        return result;
    }


    #region Internal

    protected override void SetInitialValues(Models.IMURecordCabin record)
    {
        _nextRecord = record with { Timestamp = 0 };
    }

    protected override Models.IMURecordCabin? GetRecord(string? line = null)
    {
        var skipped = new List<double[]>(Math.Max(1, _skipRate));

        while (!_stream.EndOfStream)
        {
            double[]? values = null;

            line ??= _stream.ReadLine();
            if (line == null)
                break;

            try
            {
                values = line
                    .Split('\t', StringSplitOptions.RemoveEmptyEntries)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(double.Parse)
                    .ToArray();
            }
            catch (FormatException) { return null; }
            catch { AnsiConsole.MarkupLine($"[red]Invalid data[/]: {line}"); }

            if (values?.Length == 29)
            {
                if (skipped.Count < _skipRate)
                {
                    skipped.Add(values);
                }
                else
                {
                    if (_skipRate > 0)
                    {
                        values = Average(values, skipped);
                    }

                    var imuRecord = new Models.IMURecordCabin(
                        (long)(1000 * values[0]) - _startTime,
                        new Models.Acceleration(values[1], values[2], values[3]),
                        new Models.AngularVelocity(values[4], values[5], values[6]),
                        new Models.Orientation(values[8], values[7], 0),
                        new Models.Vector3D(values[9], values[10], values[11]),
                        new Models.Vector3D(values[12], values[13], values[14]),
                        values[15], values[16], values[17], (int)values[18],
                        new Models.Vector3D(values[19], values[20], values[21]),
                        new Models.FrontAxleSuspension(values[25], values[22], values[23], values[28]),
                        values[24], values[26], values[27]
                    );

                    return imuRecord;
                }
            }

            line = null;
        }

        return null;
    }

    #endregion
}
