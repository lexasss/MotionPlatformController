using ValtraIMU.Services.DataProviders;

namespace ValtraIMU.DataProviders;

/// <summary>
/// Reads data from IMU+GNSS data log file collected with front-attached device
/// </summary>
internal class IMUFileFront(string filename, int skipRate = 0) : IMUFile<Models.IMURecordFront>(filename, skipRate)
{
    /// <summary>
    /// Creates an instance of <see cref="IMUFileFront"/> based on the settings.
    /// </summary>
    /// <param name="settings">Settings</param>
    /// <returns>an instance of <see cref="IMUFileFront"/>, or null if the filename is not set or the file does not exist</returns>
    public static IMUFileFront? Create(ref Settings settings)
    {
        IMUFileFront? result = null;

        var filename = settings.Filename.Value;
        if (File.Exists(filename))
        {
            Console.Write($"Loading as IMU Front...  ");
            try
            {
                result = new IMUFileFront(filename, settings.SkipRate);
                Console.WriteLine("done.");
            }
            catch { Console.WriteLine("failed."); }
        }

        return result;
    }

    public override bool Get(long timestamp, out Models.IMURecordFront? record)
    {
        bool result;
        while (result = MoveNext())
        {
            if (Current.Time >= timestamp)
                break;
        }
        record = result ? Current : null;
        return result;
    }

    #region Internal

    protected override void SetInitialValues(Models.IMURecordFront record)
    {
        _nextRecord = record with { Time = 0 };
    }

    protected override Models.IMURecordFront? GetRecord(string? line = null)
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
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(double.Parse)
                    .ToArray();
            }
            catch (FormatException) { return null; }
            catch { Console.WriteLine($"Invalid data: {line}"); }

            if (values?.Length == 22)
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

                    var imuRecord = new Models.IMURecordFront((int)values[0], (long)(1000*values[1]) - _startTime,
                        new Models.Coordinates(values[2], values[3], values[4]),
                        new Models.Position(values[5], values[6]),
                        new Models.Orientation(values[7], values[8], values[9]),
                        new Models.AbsoluteVelocity(values[10], values[11], values[12]),
                        new Models.AbsoluteAcceleration(values[13], values[14], values[15]),
                        new Models.Vector3D(values[16], values[17], values[18]),
                        new Models.AngularVelocity3D(values[19], values[20], values[21])
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
