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
        private IWaveProvider? _currentWaveProvider;
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
    private System.Timers.Timer? _positionTimer;

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
        public double Length => (_currentWaveProvider as WaveStream)?.TotalTime.TotalSeconds ?? 0;

        /// <summary>
        /// Gets the sample rate of the loaded audio in Hz.
        /// </summary>
        public int SampleRate => _currentWaveProvider?.WaveFormat.SampleRate ?? 0;

        /// <summary>
        /// Gets the current playback position in seconds.
        /// </summary>
        public double Position => (_currentWaveProvider as WaveStream)?.CurrentTime.TotalSeconds ?? 0;

        /// <summary>
        /// Gets whether the deck is currently playing.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Gets the properties of the currently loaded audio file.
        /// </summary>
        public AudioFileProperties? FileProperties => _currentFileProperties;

        private void StartPositionLogging()
        {
            _positionTimer = new System.Timers.Timer(2000); // Every 2 seconds
            _positionTimer.Elapsed += (s, e) =>
            {
                  if (_isPlaying && _currentWaveProvider != null)
                  {
                      var waveStream = _currentWaveProvider as WaveStream;
                      if (waveStream != null)
                      {
                          _logger.LogDebug("Deck {Deck} Position: {Position:F2}s / {Length:F2}s, Looping: {Looping}, Playing: {Playing}",
                              _deckNumber, waveStream.CurrentTime.TotalSeconds,
                              waveStream.TotalTime.TotalSeconds, true, _isPlaying); // Looping is always true for now
                      }
                  }
            };
            _positionTimer.Start();
        }

        private void StopPositionLogging()
        {
            _positionTimer?.Stop();
            _positionTimer?.Dispose();
            _positionTimer = null;
        }

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

                   // Create audio source with validation and fallback
                   ISampleProvider validatedReader = null;
                   IWaveProvider selectedWaveProvider = null;
                   WaveFormat selectedWaveFormat = null;
                   string selectedReaderType = "";
                   float validatedAmplitude = 0f;

                  string extension = Path.GetExtension(filePath).ToLowerInvariant();

                   // Primary reader selection based on format
                   switch (extension)
                   {
                       case ".wav":
                           // WaveFileReader primary
                           try
                           {
                               var waveReader = new WaveFileReader(filePath);
                               var waveSampleProvider = waveReader.ToSampleProvider();
                               var (isValid, amplitude) = ValidateAudioContent(waveSampleProvider);

                                if (isValid)
                                {
                                    selectedWaveProvider = waveReader;
                                    selectedWaveFormat = waveReader.WaveFormat;
                                    validatedReader = waveSampleProvider;
                                    selectedReaderType = "WaveFileReader";
                                    validatedAmplitude = amplitude;
                                }
                               else
                               {
                                   waveReader.Dispose();
                                   throw new Exception("WaveFileReader validation failed - silent data");
                               }
                           }
                           catch (Exception ex)
                           {
                               _logger.LogWarning(ex, "WaveFileReader failed for WAV {File}, falling back", filePath);
                           }
                           break;

                       case ".mp3":
                           // Mp3FileReader primary
                           try
                           {
                               var mp3Reader = new Mp3FileReader(filePath);
                               var mp3SampleProvider = mp3Reader.ToSampleProvider();
                               var (mp3Valid, mp3Amplitude) = ValidateAudioContent(mp3SampleProvider);

                                if (mp3Valid)
                                {
                                    selectedWaveProvider = mp3Reader;
                                    selectedWaveFormat = mp3Reader.WaveFormat;
                                    validatedReader = mp3SampleProvider;
                                    selectedReaderType = "Mp3FileReader";
                                    validatedAmplitude = mp3Amplitude;
                                }
                               else
                               {
                                   mp3Reader.Dispose();
                                   throw new Exception("Mp3FileReader validation failed - silent data");
                               }
                           }
                           catch (Exception ex)
                           {
                               _logger.LogWarning(ex, "Mp3FileReader failed for MP3 {File}, falling back", filePath);
                           }
                           break;

                       default:
                           // AudioFileReader primary
                           try
                           {
                               var reader = new AudioFileReader(filePath);
                               var (isValid, amplitude) = ValidateAudioContent(reader);

                                if (isValid)
                                {
                                    selectedWaveProvider = reader;
                                    selectedWaveFormat = reader.WaveFormat;
                                    validatedReader = reader.ToSampleProvider();
                                    selectedReaderType = "AudioFileReader";
                                    validatedAmplitude = amplitude;
                                }
                               else
                               {
                                   reader.Dispose();
                                   throw new Exception("AudioFileReader validation failed - silent data");
                               }
                           }
                           catch (Exception ex)
                           {
                               _logger.LogWarning(ex, "AudioFileReader failed for {File}, falling back", filePath);
                           }
                           break;
                   }

                   // Fallback to MediaFoundationReader if primary failed
                   if (validatedReader == null)
                   {
                       try
                       {
                           var mfReader = new MediaFoundationReader(filePath);
                           var mfSampleProvider = mfReader.ToSampleProvider();
                           var (mfValid, mfAmplitude) = ValidateAudioContent(mfSampleProvider);

                            if (mfValid)
                            {
                                selectedWaveProvider = mfReader;
                                selectedWaveFormat = mfReader.WaveFormat;
                                validatedReader = mfSampleProvider;
                                selectedReaderType = "MediaFoundationReader";
                                validatedAmplitude = mfAmplitude;
                            }
                           else
                           {
                               mfReader.Dispose();
                               throw new Exception("MediaFoundationReader validation failed - silent data");
                           }
                       }
                       catch (Exception ex)
                       {
                           _logger.LogError(ex, "All reader attempts failed for {File}", filePath);
                           throw new InvalidOperationException($"No compatible audio reader found for {filePath}", ex);
                       }
                   }

                 // Log results
                 _logger.LogInformation("Audio reader selected: {Reader} for {File}, amplitude: {Amplitude:F3}",
                     selectedReaderType, filePath, validatedAmplitude);
                  Console.WriteLine($"Audio Reader: {selectedReaderType}, File: {Path.GetFileName(filePath)}, Amplitude: {validatedAmplitude:F3}");

                  _currentWaveProvider = selectedWaveProvider;

                  var chainStart = DateTime.Now;

                  // Extract audio file properties
                  var waveFormat = selectedWaveFormat;
                   var waveStream = selectedWaveProvider as WaveStream;
                   long totalSamples = waveStream?.Length / waveFormat.BlockAlign ?? 0;
                   double durationSeconds = waveStream?.TotalTime.TotalSeconds ?? 0;

                 _currentFileProperties = new AudioFileProperties
                 {
                     SampleRate = waveFormat.SampleRate,
                     Channels = waveFormat.Channels,
                     BitsPerSample = waveFormat.BitsPerSample,
                     TotalSamples = totalSamples,
                     Duration = durationSeconds,
                     Bitrate = waveFormat.SampleRate * waveFormat.Channels * waveFormat.BitsPerSample,
                     FileSize = new System.IO.FileInfo(filePath).Length
                 };

                 // Log format details for debugging
                 _logger.LogInformation("Deck {DeckNumber} format: {SampleRate}Hz, {Channels}ch, {Bits}bit, Reader: {Reader}",
                     _deckNumber, waveFormat.SampleRate, waveFormat.Channels, waveFormat.BitsPerSample, selectedReaderType);
                  ISampleProvider sampleProvider = validatedReader;
                 _logger.LogInformation("Creating TimedSampleProvider: Deck{Deck}_Reader", _deckNumber);
                 sampleProvider = new TimedSampleProvider(sampleProvider, $"Deck{_deckNumber}_Reader", _logger);

                  // Ensure stereo output BEFORE resampling
                  if (waveFormat.Channels == 1)
                 {
                     var convertStart = DateTime.Now;
                     sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
                     var convertTime = DateTime.Now - convertStart;
                     _logger.LogInformation("Mono→Stereo conversion for deck {DeckNumber}: {Time}ms", _deckNumber, convertTime.TotalMilliseconds);
                 }

                  // Resample to 44100 Hz if necessary
                   if (waveFormat.SampleRate != 44100)
                   {
                       var resampleStart = DateTime.Now;
                       _logger.LogInformation("Resampling deck {DeckNumber} from {SampleRate}Hz to 44100Hz using WDL", _deckNumber, waveFormat.SampleRate);
                      try
                      {
                          sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 44100);
                          sampleProvider = new TimedSampleProvider(sampleProvider, $"Deck{_deckNumber}_Resampler", _logger);
                          var resampleTime = DateTime.Now - resampleStart;
                          _logger.LogInformation("WDL resampler initialized for deck {DeckNumber}: {Time}ms", _deckNumber, resampleTime.TotalMilliseconds);
                      }
                      catch (Exception ex)
                      {
                          _logger.LogError(ex, "Failed to initialize WDL resampler for deck {DeckNumber}", _deckNumber);
                          throw;
                      }
                  }

                 var chainBuildTime = DateTime.Now - chainStart; // Assume chainStart defined earlier
                 _logger.LogInformation("Deck {DeckNumber} processing complete: {Format} → 44100Hz stereo, Chain build: {Time}ms",
                     _deckNumber, _currentFileInfo?.FormatDescription ?? "unknown", chainBuildTime.TotalMilliseconds);
                 _logger.LogInformation("Deck {DeckNumber} final sample provider format: {SampleRate}Hz, {Channels}ch",
                     _deckNumber, sampleProvider.WaveFormat.SampleRate, sampleProvider.WaveFormat.Channels);
                 // Switch the source in the permanent provider chain
                  _loopingProvider.SetSource(sampleProvider, _currentWaveProvider as WaveStream);

                  // Recreate playing provider with timing wrapper
                  _logger.LogInformation("Creating TimedSampleProvider: Deck{Deck}_Looping", _deckNumber);
                  var timedLooping = new TimedSampleProvider(_loopingProvider, $"Deck{_deckNumber}_Looping", _logger);
                  _logger.LogInformation("Creating PlayingSampleProvider with loop support for deck {Deck}", _deckNumber);
                  var playingProvider = new PlayingSampleProvider(timedLooping, () => _isPlaying);
                  _playingProvider = playingProvider;
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
        /// Validates that an audio reader contains real audio data (non-zero amplitude).
        /// </summary>
        private (bool isValid, float maxAmplitude) ValidateAudioContent(ISampleProvider reader)
        {
            const int testBufferSize = 4096; // Test larger buffer for better validation
            float[] testBuffer = new float[testBufferSize];

            try
            {
                int samplesRead = reader.Read(testBuffer, 0, testBufferSize);

                if (samplesRead == 0)
                {
                    return (false, 0f); // No data read
                }

                // Calculate maximum absolute amplitude
                float maxAmplitude = 0f;
                for (int i = 0; i < samplesRead; i++)
                {
                    maxAmplitude = Math.Max(maxAmplitude, Math.Abs(testBuffer[i]));
                }

                // Consider valid if amplitude > threshold (avoid noise floor issues)
                bool isValid = maxAmplitude > 0.001f; // Adjust threshold as needed

                return (isValid, maxAmplitude);
            }
            catch (Exception)
            {
                return (false, 0f);
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
            _logger.LogInformation("Deck {DeckNumber} Play() called - was playing: {WasPlaying}", _deckNumber, _isPlaying);
            _isPlaying = true;
            StartPositionLogging();
            _logger.LogInformation("Deck {DeckNumber} now playing: {IsPlaying}", _deckNumber, _isPlaying);
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
            _logger.LogInformation("Deck {DeckNumber} Pause() called - was playing: {WasPlaying}", _deckNumber, _isPlaying);
            _isPlaying = false;
            _logger.LogInformation("Deck {DeckNumber} paused: {IsPlaying}", _deckNumber, _isPlaying);
        }

        /// <summary>
        /// Stops playback and resets position to the beginning.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            StopPositionLogging();
            _logger.LogInformation("Stopping deck {DeckNumber}", _deckNumber);
        }

        /// <summary>
        /// Seeks to a specific position in the audio.
        /// </summary>
        /// <param name="seconds">Position in seconds.</param>
        public void Seek(double seconds)
        {
            var waveStream = _currentWaveProvider as WaveStream;
            if (waveStream != null)
            {
                waveStream.CurrentTime = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, Length));
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
            (_currentWaveProvider as IDisposable)?.Dispose();
            _currentWaveProvider = null;
            _volumeProvider = null;
        }
    }
}