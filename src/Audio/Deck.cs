using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// A sample provider that loops the underlying source indefinitely.
    /// </summary>
    public class LoopingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;

        public LoopingSampleProvider(ISampleProvider source)
        {
            _source = source;
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = _source.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    // End of source, loop back
                    _source.Position = 0;
                    continue;
                }
                totalRead += read;
            }
            return totalRead;
        }
    }

    /// <summary>
    /// Represents an individual audio deck responsible for loading, playing, and controlling a single audio file.
    /// Follows Single Responsibility Principle by handling only deck-specific operations.
    /// </summary>
    public class Deck : IDisposable
    {
        private readonly ILogger<Deck> _logger;
        private readonly int _deckNumber;
        private AudioFileReader? _audioFileReader;
        private VolumeSampleProvider? _volumeProvider;
        private float _baseVolume = 1.0f;
        private float _crossfaderGain = 1.0f;
        private bool _isPlaying;
        private bool _disposed;

        /// <summary>
        /// Gets the sample provider for this deck, used in mixing.
        /// </summary>
        public ISampleProvider? SampleProvider => _volumeProvider;

        /// <summary>
        /// Gets or sets the base volume level (0.0 to 2.0, clamped).
        /// </summary>
        public float Volume
        {
            get => _baseVolume;
            set
            {
                _baseVolume = Math.Clamp(value, 0.0f, 2.0f);
                UpdateEffectiveVolume();
            }
        }

        /// <summary>
        /// Gets the currently loaded file path.
        /// </summary>
        public string? LoadedFile { get; private set; }

        /// <summary>
        /// Gets the total length of the loaded audio in seconds.
        /// </summary>
        public double Length => _audioFileReader?.TotalTime.TotalSeconds ?? 0;

        /// <summary>
        /// Gets the sample rate of the loaded audio in Hz.
        /// </summary>
        public int SampleRate => _audioFileReader?.WaveFormat.SampleRate ?? 0;

        /// <summary>
        /// Gets the current playback position in seconds.
        /// </summary>
        public double Position => _audioFileReader?.CurrentTime.TotalSeconds ?? 0;

        /// <summary>
        /// Gets whether the deck is currently playing.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Initializes a new instance of the Deck class.
        /// </summary>
        /// <param name="deckNumber">The deck number identifier.</param>
        /// <param name="logger">Logger for this deck instance.</param>
        public Deck(int deckNumber, ILogger<Deck> logger)
        {
            _deckNumber = deckNumber;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads an audio file into this deck.
        /// </summary>
        /// <param name="filePath">Path to the audio file.</param>
        /// <exception cref="ArgumentNullException">Thrown if filePath is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        public void LoadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException("Audio file not found.", filePath);

            try
            {
                _logger.LogInformation("Loading file for deck {DeckNumber}: {FilePath}", _deckNumber, filePath);

                // Dispose existing resources
                DisposeResources();

                // Load new file
                _audioFileReader = new AudioFileReader(filePath);
                ISampleProvider sampleProvider = _audioFileReader.ToSampleProvider();

                // Resample to 44100 Hz if necessary
                if (_audioFileReader.WaveFormat.SampleRate != 44100)
                {
                    _logger.LogInformation("Resampling deck {DeckNumber} from {SampleRate}Hz to 44100Hz", _deckNumber, _audioFileReader.WaveFormat.SampleRate);
                    var targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2); // Always resample to stereo
                    var waveProvider = new SampleToWaveProvider(sampleProvider);
                    var resampler = new MediaFoundationResampler(waveProvider, targetFormat);
                    resampler.ResamplerQuality = 60; // High quality
                    sampleProvider = resampler.ToSampleProvider();
                }

                // Ensure stereo output
                if (_audioFileReader.WaveFormat.Channels == 1)
                {
                    sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
                    _logger.LogInformation("Converted mono to stereo for deck {DeckNumber}", _deckNumber);
                }

                _logger.LogInformation("Final audio format for deck {DeckNumber}: 44100Hz, 2ch", _deckNumber);
                var loopingProvider = new LoopingSampleProvider(sampleProvider);
                _volumeProvider = new VolumeSampleProvider(loopingProvider);
                UpdateEffectiveVolume(); // Apply current volume settings

                LoadedFile = filePath;
                _isPlaying = false;

                // Log file properties for debugging
                _logger.LogInformation("File loaded successfully for deck {DeckNumber}: {SampleRate}Hz, {BitsPerSample}bit, {Channels}ch, {TotalTime.TotalSeconds:F1}s",
                    _deckNumber, _audioFileReader.WaveFormat.SampleRate, _audioFileReader.WaveFormat.BitsPerSample,
                    _audioFileReader.WaveFormat.Channels, _audioFileReader.TotalTime.TotalSeconds);

                _logger.LogInformation("File loaded successfully for deck {DeckNumber}", _deckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading file for deck {DeckNumber}", _deckNumber);
                DisposeResources();
                throw;
            }
        }

        /// <summary>
        /// Ejects the current audio file from this deck.
        /// </summary>
        public void Eject()
        {
            _logger.LogInformation("Ejecting file from deck {DeckNumber}", _deckNumber);

            DisposeResources();
            LoadedFile = null;
            _isPlaying = false;
        }

        /// <summary>
        /// Updates the effective volume combining base volume and crossfader gain.
        /// </summary>
        private void UpdateEffectiveVolume()
        {
            if (_volumeProvider != null)
            {
                _volumeProvider.Volume = _baseVolume * _crossfaderGain;
            }
        }

        /// <summary>
        /// Starts playback of the loaded audio.
        /// </summary>
        public void Play()
        {
            if (_audioFileReader != null)
            {
                _isPlaying = true;
                _logger.LogInformation("Playing deck {DeckNumber}", _deckNumber);
            }
            else
            {
                _logger.LogWarning("Cannot play deck {DeckNumber}: no file loaded", _deckNumber);
            }
        }

        /// <summary>
        /// Updates the crossfader gain for this deck.
        /// </summary>
        /// <param name="crossfader">Crossfader position (0.0 to 1.0).</param>
        public void UpdateCrossfaderGain(float crossfader)
        {
            _crossfaderGain = _deckNumber == 1 ? 1 - crossfader : crossfader;
            UpdateEffectiveVolume();
        }

        /// <summary>
        /// Pauses playback of the loaded audio.
        /// </summary>
        public void Pause()
        {
            _isPlaying = false;
            _logger.LogInformation("Pausing deck {DeckNumber}", _deckNumber);
        }

        /// <summary>
        /// Stops playback and resets position to the beginning.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            if (_audioFileReader != null)
            {
                _audioFileReader.CurrentTime = TimeSpan.Zero;
            }
            _logger.LogInformation("Stopping deck {DeckNumber}", _deckNumber);
        }

        /// <summary>
        /// Seeks to a specific position in the audio.
        /// </summary>
        /// <param name="seconds">Position in seconds.</param>
        public void Seek(double seconds)
        {
            if (_audioFileReader != null)
            {
                _audioFileReader.CurrentTime = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, Length));
                _logger.LogInformation("Seeking deck {DeckNumber} to {Seconds}s", _deckNumber, seconds);
            }
        }

        /// <summary>
        /// Disposes of this deck and releases all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    DisposeResources();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes of internal resources.
        /// </summary>
        private void DisposeResources()
        {
            _audioFileReader?.Dispose();
            _audioFileReader = null;
            _volumeProvider = null;
        }
    }
}