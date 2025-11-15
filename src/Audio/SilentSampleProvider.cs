using NAudio.Wave;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// A sample provider that generates continuous silence (zero samples).
    /// Used to maintain a permanent audio stream in the mixer pipeline.
    /// </summary>
    public class SilentSampleProvider : ISampleProvider
    {
        private readonly WaveFormat _waveFormat;

        /// <summary>
        /// Initializes a new instance of the SilentSampleProvider class.
        /// </summary>
        /// <param name="waveFormat">The wave format for the silence stream.</param>
        public SilentSampleProvider(WaveFormat waveFormat)
        {
            _waveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
        }

        /// <summary>
        /// Gets the wave format of this sample provider.
        /// </summary>
        public WaveFormat WaveFormat => _waveFormat;

        /// <summary>
        /// Reads silence (zeros) into the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to fill with silence.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The number of samples to write.</param>
        /// <returns>The number of samples written (always equals count).</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            // Fill buffer with silence (zeros) for continuous stream
            Array.Clear(buffer, offset, count);
            return count;
        }
    }
}