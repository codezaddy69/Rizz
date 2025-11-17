using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// A sample provider that loops the underlying AudioFileReader indefinitely.
    /// </summary>
    public class LoopingSampleProvider : ISampleProvider
    {
        private ISampleProvider _source;
        private AudioFileReader? _reader;

        public LoopingSampleProvider(ISampleProvider source, AudioFileReader? reader)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = reader; // Can be null for silent sources
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Switches the audio source without changing the provider reference.
        /// </summary>
        /// <param name="newSource">The new sample provider source.</param>
        /// <param name="newReader">The associated audio file reader (null for silent sources).</param>
        public void SetSource(ISampleProvider newSource, AudioFileReader? newReader)
        {
            _source = newSource ?? throw new ArgumentNullException(nameof(newSource));
            _reader = newReader; // Can be null
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_source == null) return 0; // Safety check

            int totalRead = 0;
            while (totalRead < count)
            {
                int read = _source.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    // End of source, loop back if we have a reader
                    if (_reader != null)
                    {
                        _reader.Position = 0;
                        continue;
                    }
                    else
                    {
                        // Silent source, just return zeros for remaining buffer
                        Array.Clear(buffer, offset + totalRead, count - totalRead);
                        totalRead = count;
                        break;
                    }
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
        private readonly AudioFileAnalyzer _analyzer;
        private AudioFileReader? _audioFileReader;
        private VolumeSampleProvider? _volumeProvider;
        private LoopingSampleProvider _loopingProvider;
        private PlayingSampleProvider _playingProvider;
        private SilentSampleProvider _silentProvider;
        private AudioFileInfo? _currentFileInfo;
        private AudioFileProperties? _currentFileProperties;
        private float _baseVolume = 1.0f;
        private float _crossfaderGain = 1.0f;
        private bool _isPlaying;
        private bool _disposed;

        /// <summary>
        /// Gets the sample provider for this deck, used in mixing.
        /// Always returns a valid provider (never null) for permanent pipeline.
        /// </summary>
        public ISampleProvider SampleProvider => _playingProvider;

        /// <summary>
        /// Gets information about the currently loaded audio file.
        /// </summary>
        public AudioFileInfo? CurrentFileInfo => _currentFileInfo;

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
        /// Gets the properties of the currently loaded audio file.
        /// </summary>
        public AudioFileProperties? FileProperties => _currentFileProperties;

        /// <summary>
        /// Initializes a new instance of the Deck class.
        /// </summary>
        /// <param name="deckNumber">The deck number identifier.</param>
        /// <param name="logger">Logger for this deck instance.</param>
        public Deck(int deckNumber, ILogger<Deck> logger)
        {
            _deckNumber = deckNumber;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analyzer = new AudioFileAnalyzer(logger);

            // Initialize permanent silent provider chain for continuous pipeline
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            _silentProvider = new SilentSampleProvider(waveFormat);
            _loopingProvider = new LoopingSampleProvider(_silentProvider, null);
            _playingProvider = new PlayingSampleProvider(_loopingProvider, () => _isPlaying);
            _volumeProvider = new VolumeSampleProvider(_playingProvider);
            UpdateEffectiveVolume();
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
                Console.WriteLine($"Deck {_deckNumber} loading file: {filePath}");
                _logger.LogInformation("Loading file for deck {DeckNumber}: {FilePath}", _deckNumber, filePath);

                // Analyze file comprehensively
                Console.WriteLine($"Analyzing file: {filePath}");
                _currentFileInfo = _analyzer.AnalyzeFile(filePath);
                Console.WriteLine($"Analysis complete: {_currentFileInfo.FormatDescription}");

                // Log analysis results
                _logger.LogInformation("File analysis complete: {Info}", _currentFileInfo);
                if (_currentFileInfo.CompatibilityWarnings.Length > 0)
                {
                    _logger.LogWarning("Compatibility warnings for deck {DeckNumber}: {Warnings}",
                        _deckNumber, string.Join(", ", _currentFileInfo.CompatibilityWarnings));
                }

                 // Dispose existing resources
                 DisposeResources();

                 // Load new file
                 _audioFileReader = new AudioFileReader(filePath);

                 // Extract audio file properties
                 var waveFormat = _audioFileReader.WaveFormat;
                 _currentFileProperties = new AudioFileProperties
                 {
                     SampleRate = waveFormat.SampleRate,
                     Channels = waveFormat.Channels,
                     BitsPerSample = waveFormat.BitsPerSample,
                     TotalSamples = _audioFileReader.Length / waveFormat.BlockAlign,
                     Duration = _audioFileReader.TotalTime.TotalSeconds,
                     Bitrate = waveFormat.SampleRate * waveFormat.Channels * waveFormat.BitsPerSample,
                     FileSize = new System.IO.FileInfo(filePath).Length
                 };

                 // Log format details for debugging
                 _logger.LogInformation("Deck {DeckNumber} original format: {SampleRate}Hz, {Channels}ch, {Bits}bit",
                     _deckNumber, _audioFileReader.WaveFormat.SampleRate, _audioFileReader.WaveFormat.Channels, _audioFileReader.WaveFormat.BitsPerSample);
                ISampleProvider sampleProvider = _audioFileReader.ToSampleProvider();

                // Ensure stereo output BEFORE resampling
                if (_audioFileReader.WaveFormat.Channels == 1)
                {
                    sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
                    _logger.LogInformation("Converted mono to stereo for deck {DeckNumber}", _deckNumber);
                }

                // Resample to 44100 Hz if necessary
                if (_audioFileReader.WaveFormat.SampleRate != 44100)
                {
                    _logger.LogInformation("Resampling deck {DeckNumber} from {SampleRate}Hz to 44100Hz using WDL resampler", _deckNumber, _audioFileReader.WaveFormat.SampleRate);
                    try
                    {
                        sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 44100);
                        _logger.LogInformation("WDL resampling initialized successfully for deck {DeckNumber}", _deckNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize WDL resampler for deck {DeckNumber}", _deckNumber);
                        throw;
                    }
                }

                _logger.LogInformation("Deck {DeckNumber} processing complete: {Format} → 44100Hz stereo",
                    _deckNumber, _currentFileInfo?.FormatDescription ?? "unknown");
                _logger.LogInformation("Deck {DeckNumber} final sample provider format: {SampleRate}Hz, {Channels}ch",
                    _deckNumber, sampleProvider.WaveFormat.SampleRate, sampleProvider.WaveFormat.Channels);
                // Switch the source in the permanent provider chain
                _loopingProvider.SetSource(sampleProvider, _audioFileReader);
                // Volume provider is already set up permanently

                LoadedFile = filePath;
                _isPlaying = false;

                _logger.LogInformation("File loaded successfully for deck {DeckNumber}: {Info}", _deckNumber, _currentFileInfo);
                Console.WriteLine($"Deck {_deckNumber} file loaded successfully: {Path.GetFileName(filePath)}");
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
            _currentFileInfo = null;
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