using System;
using System.Diagnostics;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Sample provider that logs performance metrics for debugging buffer issues
    /// </summary>
    public class LoggingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly string _name;
        private readonly ILogger _logger;
        private int _readCount = 0;
        private long _totalSamplesRead = 0;
        private long _totalReadTime = 0;
        private int _slowReadCount = 0;

        public LoggingSampleProvider(ISampleProvider source, string name, ILogger logger)
        {
            _source = source;
            _name = name;
            _logger = logger;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var readStart = DateTime.Now;
            int samplesRead = _source.Read(buffer, offset, count);
            var readEnd = DateTime.Now;
            var readTime = readEnd - readStart;

            _readCount++;
            _totalSamplesRead += samplesRead;
            _totalReadTime += (long)readTime.TotalMilliseconds;

            if (readTime.TotalMilliseconds > 5) // Log slow reads
            {
                _slowReadCount++;
                _logger.LogWarning("{Name} SLOW READ #{Count}: {Samples} samples in {Time}ms (requested {Requested})",
                    _name, _readCount, samplesRead, readTime.TotalMilliseconds, count);
            }
            else if (_readCount % 500 == 0) // Periodic logging
            {
                double avgTime = _readCount > 0 ? (double)_totalReadTime / _readCount : 0;
                _logger.LogDebug("{Name} Read #{Count}: {Samples} samples, avg {Avg:F2}ms, {Slow} slow reads",
                    _name, _readCount, samplesRead, avgTime, _slowReadCount);
            }

            // Log if no samples read (end of stream)
            if (samplesRead == 0)
            {
                _logger.LogWarning("{Name} Read #{Count}: END OF STREAM (0 samples)", _name, _readCount);
            }

            return samplesRead;
        }

        public void LogPerformanceStats()
        {
            double avgTime = _readCount > 0 ? (double)_totalReadTime / _readCount : 0;
            double avgSamples = _readCount > 0 ? (double)_totalSamplesRead / _readCount : 0;
            _logger.LogInformation("{Name} Performance Stats: {Reads} reads, {TotalSamples} total samples, avg {AvgSamples:F0} samples/read, avg {AvgTime:F2}ms/read, {SlowReads} slow reads",
                _name, _readCount, _totalSamplesRead, avgSamples, avgTime, _slowReadCount);
        }
    }
}