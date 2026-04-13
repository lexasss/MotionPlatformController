namespace ValtraIMU.Services;

internal class ButterworthFilter
{
    public enum FilterType
    {
        LowPass,
        HighPass,
        BandPass,
        BandStop
    }

    public ButterworthFilter(int order, double frequency, int channelCount, FilterType filterType, double cutoff, double cutoff2 = 0)
    {
        _filter = new IIR_Butterworth_CS_Library.IIR_Butterworth();

        var wa = cutoff / (frequency / 2);
        var wb = cutoff2 / (frequency / 2);
        _coefs = filterType switch
        {
            FilterType.LowPass => _filter.Lp2lp(wa, order),
            FilterType.HighPass => _filter.Lp2hp(wa, order),
            FilterType.BandPass => _filter.Lp2bp(wa, wb, order),
            FilterType.BandStop => _filter.Lp2bs(wa, wb, order),
            _ => throw new ArgumentException("Invalid filter type")
        };

        if (!_filter.Check_stability_iir(_coefs))
        {
            throw new InvalidOperationException("The filter is unstable.");
        }

        _size = order + 1;

        _prev = new double[channelCount][];
        for (int i = 0; i < channelCount; i++)
        {
            _prev[i] = new double[_size];
            Array.Clear(_prev[i], 0, _size);
        }
    }

    /// <see cref="IIR_Butterworth_CS_Library.IIR_Butterworth.Filter_Data(double[][], double[])"/>
    /// adapted for real-time processing of single values
    public double[] Process(double[] values)
    {
        double[] result = new double[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            if (_index == 0)
            {
                result[i] = values[i] * _coefs[0][0];

                for (int j = 1; j < _size; j++)
                {
                    _prev[i][j - 1] = values[i] * _coefs[0][j] - result[i] * _coefs[1][j];
                }
            }
            else
            {
                result[i] = values[i] * _coefs[0][0] + _prev[i][0];

                for (int j = 1; j < _size; j++)
                {
                    _prev[i][j - 1] = values[i] * _coefs[0][j] + _prev[i][j] - result[i] * _coefs[1][j];

                    if (j == _size - 1)
                    {
                        _prev[i][j - 1] = values[i] * _coefs[0][j] - result[i] * _coefs[1][j];
                    }
                }
            }
        }
        /* // debug
        if(_index >= 400 && _index < 900)
        {
            for (int i = 0; i < values.Length; i++)
            {
                System.Diagnostics.Debug.Write($" {values[i]:F3} {result[i]:F3}");
            }
            System.Diagnostics.Debug.WriteLine("");
        }*/

        _index++;

        return result;
    }

    // Internal

    readonly int _size;
    readonly IIR_Butterworth_CS_Library.IIR_Butterworth _filter;
    readonly double[][] _coefs;
    readonly double[][] _prev;

    int _index = 0;

    // Test method to verify the filter implementation
    public static void Test()
    {
        const int order = 10;
        const double frequency = 80;
        const double sampleRate = frequency * 10;
        const double cutoff = 0.4;
        const int signalLength = 500;

        double[] input = new double[signalLength];
        for (int i = 0; i < signalLength; i++)
        {
            input[i] = 2d + Math.Sin(2 * Math.PI * frequency * i / sampleRate);
        }

        var filter = new IIR_Butterworth_CS_Library.IIR_Butterworth();
        var coefs = filter.Lp2hp(cutoff / (frequency / 2), order);

        for (int j = 0; j < coefs[0].Length; j++)
        {
            System.Diagnostics.Debug.Write($" {coefs[0][j] / coefs[1][j]:F3}");
        }
        System.Diagnostics.Debug.WriteLine("");

        if (filter.Check_stability_iir(coefs))
        {
            System.Diagnostics.Debug.WriteLine("Filter is stable");
            double[] output = filter.Filter_Data(coefs, input);

            try
            {
                var flt = new ButterworthFilter(order, frequency, 1, FilterType.HighPass, cutoff);

                for (int i = 0; i < signalLength; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"{input[i]:F3} {output[i]:F3} {flt.Process([input[i]])[0]:F3}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Filter is NOT stable");
        }
    }
}
