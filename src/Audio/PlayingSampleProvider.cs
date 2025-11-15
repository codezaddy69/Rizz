using NAudio.Wave;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// A sample provider wrapper that returns silence when playback is paused.
    /// Ensures paused decks contribute silence to the mix instead of continuing to play.
    /// </summary>
    public class PlayingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly Func<bool> _isPlayingGetter;

        /// <summary>
        /// Initializes a new instance of the PlayingSampleProvider class.
        /// </summary>
        /// <param name="source">The source sample provider to wrap.</param>
        /// <param name="isPlayingGetter">Function to check if playback is active.</param>
        public PlayingSampleProvider(ISampleProvider source, Func<bool> isPlayingGetter)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _isPlayingGetter = isPlayingGetter ?? throw new ArgumentNullException(nameof(isPlayingGetter));
            WaveFormat = source.WaveFormat;
        }

        /// <summary>
        /// Gets the wave format of this sample provider.
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads samples from the source if playing, otherwise returns silence.
        /// </summary>
        /// <param name="buffer">The buffer to fill with samples.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The number of samples to read.</param>
        /// <returns>The number of samples read.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            if (_isPlayingGetter())
            {
                return _source.Read(buffer, offset, count);
            }
            else
            {
                // Return silence when paused
                Array.Clear(buffer, offset, count);
                return count;
            }
        }
    }
}