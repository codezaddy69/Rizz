using System;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Sample provider that logs performance metrics for debugging buffer issues
    /// </summary>
    public class TimedSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly string _name;
        private readonly ILogger _logger;
        private int _readCount = 0;
        private long _totalReadTime = 0;
        private int _slowReadCount = 0;

        public TimedSampleProvider(ISampleProvider source, string name, ILogger logger)
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
            _totalReadTime += (long)readTime.TotalMilliseconds;

            // Log slow reads
            if (readTime.TotalMilliseconds > 5)
            {
                _slowReadCount++;
                _logger.LogWarning("{Name} SLOW READ #{Count}: {Samples} samples in {Time}ms (requested {Requested})",
                    _name, _readCount, samplesRead, readTime.TotalMilliseconds, count);
            }
            else if (_readCount % 10 == 0) // Periodic logging at Info (more frequent for troubleshooting)
            {
                double avgTime = _readCount > 0 ? (double)_totalReadTime / _readCount : 0;
                string logMsg = $"{_name} Read #{_readCount}: {samplesRead} samples in {readTime.TotalMilliseconds:F2}ms, avg {avgTime:F2}ms, {_slowReadCount} slow reads";
                _logger.LogInformation(logMsg);
                Console.WriteLine(logMsg);
            }

            // Log if no samples read (end of stream)
            if (samplesRead == 0)
            {
                _logger.LogWarning("{Name} Read #{Count}: END OF STREAM (0 samples)", _name, _readCount);
            }
            else if (samplesRead == 0 && _readCount > 1)
            {
                _logger.LogError("{Name} Read failure: 0 samples returned on read #{Count}", _name, _readCount);
            }

            return samplesRead;
        }
    }
}