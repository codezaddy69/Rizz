using System;
using System.Diagnostics;
using NAudio.Wave;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Sample provider that logs performance metrics for debugging buffer issues
    /// </summary>
    public class LoggingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly string _name;
        private int _readCount = 0;
        private long _totalSamplesRead = 0;
        private Stopwatch _stopwatch = new Stopwatch();

        public LoggingSampleProvider(ISampleProvider source, string name)
        {
            _source = source;
            _name = name;
            _stopwatch.Start();
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var readStart = _stopwatch.ElapsedMilliseconds;
            int samplesRead = _source.Read(buffer, offset, count);
            var readEnd = _stopwatch.ElapsedMilliseconds;

            _readCount++;
            _totalSamplesRead += samplesRead;

            // Log every 100 reads or if read takes >1ms
            if (_readCount % 100 == 0 || (readEnd - readStart) > 1)
            {
                Console.WriteLine($"{_name} Read #{_readCount}: {samplesRead} samples in {readEnd - readStart}ms (requested {count})");
            }

            // Log if no samples read (end of stream)
            if (samplesRead == 0)
            {
                Console.WriteLine($"{_name} Read #{_readCount}: END OF STREAM (0 samples)");
            }

            return samplesRead;
        }

        public void LogStats()
        {
            Console.WriteLine($"{_name} Stats: {_readCount} reads, {_totalSamplesRead} total samples, avg {(double)_totalSamplesRead / _readCount:F0} samples/read");
        }
    }
}