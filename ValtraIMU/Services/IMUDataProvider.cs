namespace ValtraIMU.Services;

/// <summary>
/// IMU+GNSS data provider
/// </summary>
internal class IMUDataProvider
{
    /// <summary>
    /// Reads data from IMU+GNSS data log file
    /// </summary>
    /// <param name="filename">data filename</param>
    /// <param name="skipRate">number of lines to skip</param>
    /// <returns>IMU+GNSS dataset</returns>
    public static Models.IMUData[] LoaFromFile(string filename, int skipRate = 0)
    {
        var result = new List<Models.IMUData>(0x100000);

        float startTime = 0;

        var skipped = new List<float[]>(Math.Max(1, skipRate));

        using (var stream = new StreamReader(filename))
        {
            var line = stream.ReadLine();
            while (line != null)
            {
                if (line.Length > 0 && int.TryParse(line[0..1], out int _))
                    break;

                // Skip comments at the beginning of the file
                line = stream.ReadLine();
            }

            while (line != null && !stream.EndOfStream)
            {
                float[]? values = null;

                try
                {
                    values = line
                        .Split(' ')
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(float.Parse)
                        .ToArray();
                }
                catch (FormatException) { }
                catch { Console.WriteLine($"Invalid data: {line}"); }

                if (values?.Length == 22)
                {
                    if (startTime == 0)
                    {
                        startTime = values[1];
                    }

                    if (skipped.Count < skipRate)
                    {
                        skipped.Add(values);
                    }
                    else
                    {
                        if (skipRate > 0)
                        {
                            values = Average(values, skipped);
                            skipped.Clear();
                        }

                        var imuData = new Models.IMUData(values[0], values[1] - startTime,
                            new Models.Coordinates(values[2], values[3], values[4]),
                            new Models.Position(values[5], values[6]),
                            new Models.Orientation(values[7], values[8], values[9]),
                            new Models.Velocity(values[10], values[11], values[12]),
                            new Models.AbsoluteAcceleration(values[13], values[14], values[15]),
                            new Models.BodyAcceleration(values[16], values[17], values[18]),
                            new Models.AngularVelocity(values[19], values[20], values[21])
                        );

                        result.Add(imuData);
                    }
                }

                line = stream.ReadLine();
            }
        }

        return result.ToArray();
    }

    // Internal

    private static float[] Average(float[] last, List<float[]> previous)
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
}
