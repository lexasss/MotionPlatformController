using SharpDialogs;
using System.Collections;

namespace ValtraIMU.DataProviders;

/// <summary>
/// Reads data from IMU+GNSS data log file
/// </summary>
internal class IMUFile : IDataProvider<Models.IMUData>
{
    public Models.IMUData Current => _nextData ?? throw new Exception();

    /// <summary>Constructor</summary>
    /// <param name="filename">data filename</param>
    /// <param name="skipRate">number of lines to skip</param>
    public IMUFile(string filename, int skipRate = 0)
    {
        _stream = new StreamReader(filename);
        _skipRate = skipRate;

        // Skip comments at the beginning of the file
        var line = _stream.ReadLine();
        while (line != null && !_stream.EndOfStream)
        {
            if (line.Length > 0 && int.TryParse(line[0..1], out int _))
            {
                _nextData = GetData(line);
                if (_nextData != null)
                {
                    _startTime = _nextData.Time;
                    _nextData = _nextData with { Time = 0 };
                    break;
                }
            }

            line = _stream.ReadLine();
        }
    }

    /// <summary>
    /// Create an instance of <see cref="IMUFile"/> based on the settings.
    /// If the filename is "sim", returns null, which indicates that the simulation mode is active and no data provider is needed.
    /// If the filename is not set or the file does not exist, shows the file open dialog to select the data file.
    /// This action replaces the filename in the settings.
    /// </summary>
    /// <param name="settings">Settings</param>
    /// <returns>an instance of <see cref="IMUFile"/></returns>
    public static IMUFile? Create(ref Settings settings)
    {
        IMUFile? result = null;
        var filename = settings.Filename.Value ?? "";

        // If the filename is "sim", we are in simulation mode, and no data provider is needed.
        if (filename.ToLower() == "sim")
        {
            return null;
        }

        // If the filename is not set or the file does not exist, show the file open dialog.
        if (!File.Exists(filename))
        {
            settings.Filename.Value = SharpFileOpenDialog.ShowSingleSelect(IntPtr.Zero, "Valtra IMU+GNSS data");
        }

        if (File.Exists(filename))
        {
            Console.Write($"Loading data from {settings.Filename}...  ");
            result = new IMUFile(filename, settings.SkipRate);
            Console.WriteLine("done.");
        }

        return result;
    }

    #region IDataProvider implementation

    public void Dispose()
    {
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool MoveNext()
    {
        _nextData = GetData();
        return _nextData != null;
    }

    public void Reset()
    {
        _stream.BaseStream.Seek(0, SeekOrigin.Begin);
    }

    public bool Get(long timestamp, out Models.IMUData? value)
    {
        bool result;
        while (result = MoveNext())
        {
            if (Current.Time >= timestamp)
                break;
        }
        value = result ? Current : null;
        return result;
    }

    #endregion

    #region Internal

    object IEnumerator.Current => Current;

    readonly StreamReader _stream;
    readonly int _skipRate;
    readonly long _startTime;

    Models.IMUData? _nextData = null;

    private Models.IMUData? GetData(string? line = null)
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
                    .Split(' ')
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(double.Parse)
                    .ToArray();
            }
            catch (FormatException) { }
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

                    var imuData = new Models.IMUData((int)values[0], (long)(1000*values[1]) - _startTime,
                        new Models.Coordinates(values[2], values[3], values[4]),
                        new Models.Position(values[5], values[6]),
                        new Models.Orientation(values[7], values[8], values[9]),
                        new Models.Velocity(values[10], values[11], values[12]),
                        new Models.AbsoluteAcceleration(values[13], values[14], values[15]),
                        new Models.BodyAcceleration(values[16], values[17], values[18]),
                        new Models.AngularVelocity(values[19], values[20], values[21])
                    );

                    return imuData;
                }
            }

            line = null;
        }

        return null;
    }
    
    private static double[] Average(double[] last, List<double[]> previous)
    {
        foreach (var data in previous)
        {
            for (int i = 0; i < last.Length; i++)
            {
                last[i] += data[i];
            }
        }

        for (int i = 0; i < last.Length; i++)
        {
            last[i] /= previous.Count + 1;
        }

        return last;
    }

    #endregion
}
