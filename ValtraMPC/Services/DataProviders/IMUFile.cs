using System.Collections;
using ValtraMPC.DataProviders;

namespace ValtraMPC.Services.DataProviders;

internal abstract class IMUFile<T> : IDataProvider<T> where T : Models.IRecord
{
    public T Current => _nextRecord ?? throw new Exception();

    public double Progress => (double)_bytesRead / _stream.BaseStream.Length;

    /// <summary>Constructor</summary>
    /// <param name="filename">data filename</param>
    /// <param name="skipRate">number of lines to skip</param>
    public IMUFile(string filename, int skipRate = 0)
    {
        using (var file = File.OpenRead(filename))
        {
            _totalBytes = file.Length;
        }

        _stream = new StreamReader(filename);
        _skipRate = skipRate;

        // Skip comments at the beginning of the file
        var line = _stream.ReadLine();

        while (line != null && !_stream.EndOfStream)
        {
            _bytesRead += (long)line.Length + 2;

            if (line.Length > 0 && int.TryParse(line[0..1], out int _))
            {
                _nextRecord = GetRecord(line);
                if (_nextRecord != null)
                {
                    _startTime = _nextRecord.Timestamp;
                    SetInitialValues(_nextRecord);
                    break;
                }
                else
                {
                    throw new Exception($"Invalid data at the beginning of the file: {line}");
                }
            }

            line = _stream.ReadLine();
        }
    }


    #region IDataProvider implementation

    public void Dispose()
    {
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool MoveNext()
    {
        _nextRecord = GetRecord();
        return _nextRecord != null;
    }

    public void Reset()
    {
        _stream.BaseStream.Seek(0, SeekOrigin.Begin);
    }

    public abstract bool Get(long timestamp, out T? value);

    #endregion

    #region Internal

    long _totalBytes;
    protected long _bytesRead = 0;

    object IEnumerator.Current => Current;

    protected readonly StreamReader _stream;
    protected readonly int _skipRate;

    protected long _startTime;
    protected T? _nextRecord = default;

    protected abstract void SetInitialValues(T data);
    protected abstract T? GetRecord(string? line = null);

    protected static double[] Average(double[] last, List<double[]> previous)
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
