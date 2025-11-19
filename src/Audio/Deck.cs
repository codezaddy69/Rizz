using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{


    public class Deck : IDisposable
    {
        private readonly ILogger<Deck> _logger;
        private readonly int _deckNumber;
        private readonly AudioFileAnalyzer _analyzer;
        private CachingReader? _cachingReader;
        private EngineBuffer? _engineBuffer;
        private AudioFileInfo? _currentFileInfo;
    private AudioFileProperties? _currentFileProperties;
    private float _baseVolume = 1.0f;
    private float _crossfaderGain = 1.0f;
    private bool _isPlaying;
    private bool _disposed;
        /// <summary>
        /// Gets the sample provider for this deck, used in mixing.
        /// Rizz uses EngineBuffer, so this returns a dummy provider.
        /// </summary>
        public ISampleProvider SampleProvider => new SilentSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

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
            }
        }

        /// <summary>
        /// Gets the currently loaded file path.
        /// </summary>
        public string? LoadedFile { get; private set; }

        /// <summary>
        /// Gets the total length of the loaded audio in seconds.
        /// </summary>
        public double Length => _engineBuffer?.Length ?? 0;

        /// <summary>
        /// Gets the sample rate of the loaded audio in Hz.
        /// </summary>
        public int SampleRate => _engineBuffer?.SampleRate ?? 0;

        /// <summary>
        /// Gets the current playback position in seconds.
        /// </summary>
        public double Position => _engineBuffer?.Position ?? 0;

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

            Console.WriteLine($"Deck {_deckNumber} loading file: {filePath}");
            _logger.LogInformation("Loading file for deck {DeckNumber}: {FilePath}", _deckNumber, filePath);

            // Analyze file
            _currentFileInfo = _analyzer.AnalyzeFile(filePath);
            _logger.LogInformation("File analysis complete: {Info}", _currentFileInfo);

            // Dispose existing resources
            DisposeResources();

            // Create Rizz components
            var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            _cachingReader = new CachingReader(filePath, loggerFactory.CreateLogger<CachingReader>());
            _engineBuffer = new EngineBuffer(_cachingReader, loggerFactory.CreateLogger<EngineBuffer>(), 1024);

            LoadedFile = filePath;
            _isPlaying = false;

            // Update file properties (simplified)
            _currentFileProperties = new AudioFileProperties
            {
                SampleRate = 44100,
                Channels = 2,
                BitsPerSample = 16,
                Duration = 0,
                TotalSamples = 0
            };

            _logger.LogInformation("File loaded successfully for deck {DeckNumber}: {Info}", _deckNumber, _currentFileInfo);
            Console.WriteLine($"Deck {_deckNumber} file loaded successfully: {Path.GetFileName(filePath)}");
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


        /// <summary>
        /// Starts playback of the loaded audio.
        /// </summary>
        public void Play()
        {
            _engineBuffer?.Play();
            _isPlaying = true;
            _logger.LogInformation("Deck {DeckNumber} Play() called", _deckNumber);
        }

        /// <summary>
        /// Updates the crossfader gain for this deck.
        /// </summary>
        /// <param name="crossfader">Crossfader position (0.0 to 1.0).</param>
        public void UpdateCrossfaderGain(float crossfader)
        {
            _crossfaderGain = _deckNumber == 1 ? 1 - crossfader : crossfader;
        }

        /// <summary>
        /// Pauses playback of the loaded audio.
        /// </summary>
        public void Pause()
        {
            _engineBuffer?.Pause();
            _isPlaying = false;
            _logger.LogInformation("Deck {DeckNumber} Pause() called", _deckNumber);
        }

        /// <summary>
        /// Stops playback and resets position to the beginning.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _logger.LogInformation("Stopping deck {DeckNumber}", _deckNumber);
        }

        /// <summary>
        /// Seeks to a specific position in the audio.
        /// </summary>
        /// <param name="seconds">Position in seconds.</param>
        public void Seek(double seconds)
        {
            if (_engineBuffer != null)
            {
                long frame = (long)(seconds * _engineBuffer.SampleRate);
                _engineBuffer.Seek(frame);
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
        /// Plays a test tone for debugging.
        /// </summary>
        public void PlayTestTone(double frequency, double duration)
        {
            // Generate sine wave and play through EngineBuffer
            // For now, just log
            Console.WriteLine($"Playing test tone: {frequency}Hz for {duration}s");
        }

        /// <summary>
        /// Disposes of internal resources.
        /// </summary>
        private void DisposeResources()
        {
            _cachingReader?.Dispose();
            _cachingReader = null;
            _engineBuffer = null;
        }
    }
}