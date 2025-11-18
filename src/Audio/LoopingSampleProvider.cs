using NAudio.Wave;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Sample provider that loops the source indefinitely
    /// </summary>
    public class LoopingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly object? _lockObject;

        public LoopingSampleProvider(ISampleProvider source, object? lockObject = null)
        {
            _source = source;
            _lockObject = lockObject;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = _source.Read(buffer, offset + totalRead, count - totalRead);
                Console.WriteLine($"LoopingSampleProvider: source.Read returned {read} samples");
                if (read == 0)
                {
                    // End of source, reset to beginning
                    Console.WriteLine($"LoopingSampleProvider: end of source, resetting");
                    if (_lockObject != null)
                    {
                        lock (_lockObject)
                        {
                            Reset();
                        }
                    }
                    else
                    {
                        Reset();
                    }
                }
                else
                {
                    totalRead += read;
                }
            }
            Console.WriteLine($"LoopingSampleProvider: total read {totalRead} samples");
            return totalRead;
        }

        private void Reset()
        {
            // For AudioFileReader, reset position
            if (_source is RawSourceWaveStream raw && raw.Position != 0)
            {
                raw.Position = 0;
            }
        }

        public void SetSource(ISampleProvider newSource, WaveStream? waveStream = null)
        {
            // This is a simplified version - in practice, you'd need to handle source switching
            // For now, assume source doesn't change
        }
    }
}