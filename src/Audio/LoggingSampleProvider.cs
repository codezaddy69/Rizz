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
    private long _totalReadTime = 0;
    private int _slowReadCount = 0;
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
            var readTime = readEnd - readStart;

            _readCount++;
            _totalSamplesRead += samplesRead;
            _totalReadTime += readTime;

            if (readTime > 5) // Log slow reads
            {
                _slowReadCount++;
                Console.WriteLine($"{_name} SLOW READ #{_readCount}: {samplesRead} samples in {readTime}ms (requested {count})");
            }
            else if (_readCount % 200 == 0) // Log periodic normal reads
            {
                Console.WriteLine($"{_name} Read #{_readCount}: {samplesRead} samples in {readTime}ms");
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
            double avgTime = _readCount > 0 ? (double)_totalReadTime / _readCount : 0;
            double avgSamples = _readCount > 0 ? (double)_totalSamplesRead / _readCount : 0;
            Console.WriteLine($"{_name} Stats: {_readCount} reads, {_totalSamplesRead} total samples, avg {avgSamples:F0} samples/read, avg {avgTime:F2}ms/read, {_slowReadCount} slow reads");
        }
    }
}