using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Wave.Asio;

namespace DJMixMaster.Audio
{

    public interface IAudioEngine : IDisposable
    {
        event EventHandler<(int DeckNumber, double Position)>? PlaybackPositionChanged;
        event EventHandler<(int DeckNumber, double[] BeatPositions, double BPM)>? BeatGridUpdated;

        void LoadFile(int deckNumber, string filePath);
        void Play(int deckNumber);
        void Pause(int deckNumber);
        void Stop(int deckNumber);
        void Seek(int deckNumber, double seconds);
        double GetPosition(int deckNumber);
        double GetLength(int deckNumber);
        int GetSampleRate(int deckNumber);
        void SetVolume(int deckNumber, float volume);
        float GetVolume(int deckNumber);
        bool IsPlaying(int deckNumber);
        AudioFileProperties? GetDeckProperties(int deckNumber);
        void SetCrossfader(float position);
        float GetCrossfader();
        (float[] WaveformData, double Length) GetWaveformData(int deckNumber);
        void AddCuePoint(int deckNumber);
        void JumpToCuePoint(int deckNumber, int cueIndex);
        void PlayTestTone(int deckNumber, double frequency = 440.0, double durationSeconds = 2.0);
        void UpdateAudioSettings(AudioSettings settings);
        AudioSettings GetCurrentSettings();
        void ShowAsioControlPanel();
        List<AsioDeviceInfo> EnumerateDevices();
        void UpdateOutputDevice(string deviceId);
        string GetSoundOutState();
    }

    /// <summary>
    /// Main audio engine coordinating playback, mixing, and crossfading for the DJ application.
    /// Follows Single Responsibility Principle by managing overall audio coordination.
    /// Depends on abstractions (interfaces) following Dependency Inversion Principle.
    /// </summary>
    public class AudioEngine : IAudioEngine
    {
        private readonly ILogger<AudioEngine> _logger;
        private readonly Deck _deck1;
        private readonly Deck _deck2;
    private MixingSampleProvider _realMixer;
    private ISampleProvider _mixer;
    private System.Timers.Timer? _healthTimer;
        private IWavePlayer? _soundOut;
        private readonly SampleToWaveProvider _waveProvider;
        private float _crossfader = 0.5f;
        private AudioSettings _currentSettings;
        private bool _disposed;
        public bool AsioFailed { get; private set; }

        // Events follow Observer pattern for loose coupling
        public event EventHandler<(int DeckNumber, double Position)>? PlaybackPositionChanged;
        public event EventHandler<(int DeckNumber, double[] BeatPositions, double BPM)>? BeatGridUpdated;

        /// <summary>
        /// Initializes a new instance of the AudioEngine class.
        /// </summary>
        /// <param name="logger">Logger for this instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if logger is null.</exception>
        public AudioEngine(ILogger<AudioEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentSettings = new AudioSettings();

            try
            {
                var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());

                // Initialize decks (composition over inheritance)
                _deck1 = new Deck(1, loggerFactory.CreateLogger<Deck>());
                _deck2 = new Deck(2, loggerFactory.CreateLogger<Deck>());

                // Initialize mixer with float format for stereo output
                var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
                _realMixer = new MixingSampleProvider(waveFormat);
                _mixer = new TimedSampleProvider(_realMixer, "Mixer", _logger);
                _logger.LogInformation("Mixer initialized with format: {SampleRate}Hz, {Channels}ch", waveFormat.SampleRate, waveFormat.Channels);

                // Connect permanent deck providers to mixer for continuous pipeline
                _realMixer.SetProvider(0, _deck1.SampleProvider);
                _realMixer.SetProvider(1, _deck2.SampleProvider);
                _logger.LogInformation("Permanent deck inputs connected to mixer");

                // Convert to wave provider for output (abstraction layer)
                _waveProvider = new SampleToWaveProvider(_mixer);

                // Initialize output device with ASIO for low-latency pro audio
                _logger.LogInformation("Using ASIO for ultra-low latency audio output");

                // Log available ASIO devices
                string[] driverNames = AsioOut.GetDriverNames();
                _logger.LogInformation("Available ASIO Drivers: {Count}", driverNames.Length);
                for (int i = 0; i < driverNames.Length; i++)
                {
                    _logger.LogInformation("ASIO Driver {Index}: {Name}", i, driverNames[i]);
                }

                try
                {
                    _soundOut = new AsioOut(0); // First ASIO device (buffer size set by driver for low latency)

                    // Check sample rate support before Init
                    if (_soundOut is AsioOut asioOut)
                    {
                        bool supports44100 = asioOut.IsSampleRateSupported(44100);
                        bool supports48000 = asioOut.IsSampleRateSupported(48000);
                        bool supports96000 = asioOut.IsSampleRateSupported(96000);

                        _logger.LogInformation($"ASIO Driver: {asioOut.DriverName}");
                        _logger.LogInformation($"ASIO Sample Rates - 44100Hz: {supports44100}, 48000Hz: {supports48000}, 96000Hz: {supports96000}");

                        // Special handling for ASIO4ALL
                        if (asioOut.DriverName.Contains("ASIO4ALL"))
                        {
                            bool isConfigured = supports44100 || supports48000 || supports96000;
                            if (!isConfigured)
                            {
                                _logger.LogError("ASIO4ALL is not configured. No sample rates are supported. Falling back to WaveOut.");
                                throw new Exception("ASIO4ALL not configured - fallback to WaveOut");
                            }
                        }
                        else if (!supports44100)
                        {
                            _logger.LogWarning("ASIO driver does not support 44100Hz. Consider using 48000Hz or 96000Hz.");
                        }
                    }

                    _soundOut.Init(_waveProvider);

                    if (_soundOut is AsioOut asioOut2)
                    {
                        // Set channel offset from settings
                        asioOut2.ChannelOffset = _currentSettings.ChannelOffset;

                        // Log additional capabilities (after Init)
                        _logger.LogInformation($"ASIO Buffer Size: {asioOut2.FramesPerBuffer} samples");
                        _logger.LogInformation($"ASIO Playback Latency: {asioOut2.PlaybackLatency} samples");
                        _logger.LogInformation($"ASIO Channel Offset: {asioOut2.ChannelOffset}");
                        _logger.LogInformation($"ASIO Output Channels: {asioOut2.NumberOfOutputChannels}");
                    }
                    _logger.LogInformation("ASIO initialized successfully on first available device");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ASIO initialization failed - falling back to WaveOut. Reason: {Message}", ex.Message);
                    AsioFailed = true;
                    _soundOut = new WaveOut();
                    _soundOut.Init(_waveProvider);
                    _logger.LogInformation("WaveOut initialized as fallback");
                }

                _logger.LogInformation("NAudio AudioEngine initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AudioEngine");
                Dispose();
                throw;
            }
        }



        public void LoadFile(int deckNumber, string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading file for deck {deckNumber}: {filePath}");

                Deck deck = deckNumber == 1 ? _deck1 : _deck2;
                deck.LoadFile(filePath);

                // Provider is permanently connected to mixer, no need to update

                // Analyze file for beat grid
                double[] beatPositions;
                double bpm = 120.0;
                if (double.IsNaN(deck.Length) || deck.Length <= 0)
                {
                    _logger.LogWarning($"Invalid track length for deck {deckNumber}: {deck.Length}. Using empty beat grid.");
                    beatPositions = new double[0];
                }
                else
                {
                    // Simple beat detection: assume 120 BPM, beats every 0.5 seconds
                    int numBeats = (int)(deck.Length / 0.5);
                    beatPositions = new double[numBeats];
                    for (int i = 0; i < numBeats; i++)
                    {
                        beatPositions[i] = i * 0.5;
                    }
                }
                OnBeatGridUpdated(deckNumber, beatPositions, bpm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading file for deck {deckNumber}");
                throw;
            }
        }

        public void Play(int deckNumber)
        {
            try
            {
                _logger.LogInformation("Play requested for deck {Deck}", deckNumber);
                var playStart = DateTime.Now;
                Console.WriteLine($"Starting playback on deck {deckNumber}");
                Deck deck = deckNumber == 1 ? _deck1 : _deck2;
                Console.WriteLine($"Deck {deckNumber} IsPlaying before: {deck.IsPlaying}");
                _logger.LogInformation("Pre-play: Deck playing={Playing}, Output state={State}",
                    deck.IsPlaying, _soundOut?.PlaybackState);
                deck.Play();
                System.Threading.Thread.Sleep(10); // Allow pipeline to prime
                Console.WriteLine($"Deck {deckNumber} IsPlaying after: {deck.IsPlaying}");
                if (_soundOut != null && _soundOut.PlaybackState != PlaybackState.Playing)
                {
                    Console.WriteLine($"SoundOut state before play: {_soundOut.PlaybackState}");
                    _soundOut.Play();
                    var playTime = DateTime.Now - playStart;
                    _logger.LogInformation("Playback started for deck {DeckNumber} in {Time}ms, ASIO state: {State}", deckNumber, playTime.TotalMilliseconds, _soundOut.PlaybackState);
                    Console.WriteLine($"Playback started on deck {deckNumber}, output state: {_soundOut.PlaybackState}");
                }
                else if (_soundOut != null)
                {
                    _logger.LogInformation("SoundOut already playing, state: {State}", _soundOut.PlaybackState);
                    Console.WriteLine($"SoundOut already playing, state: {_soundOut.PlaybackState}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting playback on deck {DeckNumber}", deckNumber);
            }
        }

        public void Pause(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            deck.Pause();
            // If both decks are paused, stop sound out
            if (!_deck1.IsPlaying && !_deck2.IsPlaying)
            {
                _soundOut?.Pause();
            }
        }

        public void Stop(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            deck.Stop();
            if (!_deck1.IsPlaying && !_deck2.IsPlaying)
            {
                _soundOut?.Stop();
            }
        }

        public void Seek(int deckNumber, double seconds)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            deck.Seek(seconds);
        }

        public double GetPosition(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            return deck.Position;
        }

        public double GetLength(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            return deck.Length;
        }

        public int GetSampleRate(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            return deck.SampleRate;
        }



        public void SetVolume(int deckNumber, float volume)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            deck.Volume = volume;
        }

        public float GetVolume(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            return deck.Volume;
        }

        public bool IsPlaying(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            return deck.IsPlaying;
        }

        public AudioFileProperties? GetDeckProperties(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            return deck.FileProperties;
        }

        private void OnAsioDriverReset(object? sender, EventArgs e)
        {
            _logger.LogWarning("ASIO Driver reset detected - buffer settings may have changed");
            if (_soundOut is AsioOut asioOut)
            {
                _logger.LogInformation("ASIO Post-Reset: Buffer {Buffer} samples, Latency {Latency} samples",
                    asioOut.FramesPerBuffer, asioOut.PlaybackLatency);
            }
        }

        private void StartHealthMonitoring()
        {
            _healthTimer = new System.Timers.Timer(5000); // Every 5 seconds
            _healthTimer.Elapsed += (s, e) =>
            {
                // Pipeline health
                var deck1Props = GetDeckProperties(1);
                var deck2Props = GetDeckProperties(2);

                _logger.LogInformation("Pipeline Health: Deck1 Playing={Playing1}, Deck2 Playing={Playing2}, Output State={State}",
                    _deck1.IsPlaying, _deck2.IsPlaying, _soundOut?.PlaybackState);

                if (deck1Props != null)
                {
                    _logger.LogDebug("Deck1: {Format}, Length: {Len:F1}s",
                        deck1Props.FormatDescription, deck1Props.Duration);
                }
                if (deck2Props != null)
                {
                    _logger.LogDebug("Deck2: {Format}, Length: {Len:F1}s",
                        deck2Props.FormatDescription, deck2Props.Duration);
                }

                // ASIO specific
                if (_soundOut is AsioOut asioOut)
                {
                    _logger.LogInformation("ASIO Health Check: Buffer {Buffer} samples, State {State}, Latency {Latency}ms, Driver {Driver}",
                        asioOut.FramesPerBuffer, asioOut.PlaybackState,
                        (double)asioOut.PlaybackLatency / asioOut.FramesPerBuffer * 1000, asioOut.DriverName);
                    _logger.LogDebug("ASIO Detailed: ChannelOffset={Offset}, OutputChannels={Channels}, SampleRateSupported44100={Supported}",
                        asioOut.ChannelOffset, asioOut.NumberOfOutputChannels, asioOut.IsSampleRateSupported(44100));
                }
                else if (_soundOut != null)
                {
                    _logger.LogInformation("Audio Health Check: State {State}, Type {Type}", _soundOut.PlaybackState, _soundOut.GetType().Name);

                    // Check if audio is actually producing output
                    var mixerSample = _realMixer.Read(new float[1024], 0, 1024);
                    float maxLevel = 0f;
                    for (int i = 0; i < mixerSample; i++)
                    {
                        maxLevel = Math.Max(maxLevel, Math.Abs(i < 1024 ? 0f : 0f)); // Placeholder - need actual buffer
                    }
                    _logger.LogInformation("Audio Output Check: Samples read={Samples}, Max level={Level:F3}", mixerSample, maxLevel);
                    if (maxLevel < 0.001f)
                    {
                        _logger.LogWarning("POTENTIAL SILENT OUTPUT: Audio pipeline active but no signal detected. Check Windows audio settings.");
                    }
                }
            };
            _healthTimer.Start();
            _logger.LogInformation("Audio health monitoring started");
        }

        public void SetCrossfader(float position)
        {
            _crossfader = position;
            UpdateCrossfader();
        }

        public float GetCrossfader() => _crossfader;

        private void UpdateCrossfader()
        {
            _deck1.UpdateCrossfaderGain(_crossfader);
            _deck2.UpdateCrossfaderGain(_crossfader);
        }

        public (float[] WaveformData, double Length) GetWaveformData(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            // TODO: Implement waveform generation from CSCore audio data
            return (Array.Empty<float>(), deck.Length);
        }

        public void AddCuePoint(int deckNumber)
        {
            // TODO: Implement cue points
            _logger.LogInformation($"Adding cue point for deck {deckNumber} (placeholder)");
        }

        public void JumpToCuePoint(int deckNumber, int cueIndex)
        {
            // TODO: Implement cue point jumping
            _logger.LogInformation($"Jumping to cue point {cueIndex} for deck {deckNumber} (placeholder)");
        }



        protected virtual void OnPlaybackPositionChanged(int deckNumber, double position)
        {
            PlaybackPositionChanged?.Invoke(this, (deckNumber, position));
        }

        protected virtual void OnBeatGridUpdated(int deckNumber, double[] beatPositions, double bpm)
        {
            BeatGridUpdated?.Invoke(this, (deckNumber, beatPositions, bpm));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _soundOut?.Dispose();
                    _deck1?.Dispose();
                    _deck2?.Dispose();
                }
                _disposed = true;
            }
        }

        public void PlayTestTone(int deckNumber, double frequency = 440.0, double durationSeconds = 2.0)
        {
            try
            {
                _logger.LogInformation($"Playing test tone on deck {deckNumber}: {frequency}Hz for {durationSeconds}s");

                // TODO: Implement test tone using CSCore SineGenerator
                _logger.LogInformation("Test tone not yet implemented with CSCore");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error playing test tone on deck {deckNumber}");
            }
        }

        public void UpdateAudioSettings(AudioSettings settings)
        {
            _currentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger.LogInformation("Audio settings updated");

            // Apply settings that can be changed at runtime
            // Note: Some settings may require restart
            if (_soundOut is AsioOut asioOut)
            {
                // Apply channel offset if changed
                if (asioOut.ChannelOffset != settings.ChannelOffset)
                {
                    _logger.LogInformation($"Updating ASIO ChannelOffset from {asioOut.ChannelOffset} to {settings.ChannelOffset}");
                    asioOut.ChannelOffset = settings.ChannelOffset;
                }
                _logger.LogInformation("ASIO settings updated - some changes may require restart");
            }
        }

        public AudioSettings GetCurrentSettings()
        {
            return _currentSettings;
        }

        public void ShowAsioControlPanel()
        {
            if (_soundOut is AsioOut asioOut)
            {
                try
                {
                    asioOut.ShowControlPanel();
                    _logger.LogInformation("ASIO control panel shown for driver: {Driver}", asioOut.DriverName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to show ASIO control panel");
                }
            }
            else
            {
                _logger.LogWarning("Cannot show ASIO control panel - not using ASIO output");
            }
        }

        public List<AsioDeviceInfo> EnumerateDevices()
        {
            var devices = new List<AsioDeviceInfo>();
            try
            {
                string[] driverNames = AsioOut.GetDriverNames();
                foreach (string driverName in driverNames)
                {
                    try
                    {
                        using (var asio = new AsioOut(driverName))
                        {
                            devices.Add(new AsioDeviceInfo
                            {
                                Id = driverName,
                                Name = driverName,
                                DriverName = driverName,
                                MaxInputChannels = asio.DriverInputChannelCount,
                                MaxOutputChannels = asio.DriverOutputChannelCount,
                                SampleRate = 44100, // Default, could query more
                                FramesPerBuffer = asio.FramesPerBuffer,
                                PlaybackLatency = asio.PlaybackLatency,
                                Supports44100 = asio.IsSampleRateSupported(44100),
                                Supports48000 = asio.IsSampleRateSupported(48000),
                                Status = "Available"
                            });
                        }
                    }
                    catch
                    {
                        // If can't create, mark as unavailable
                        devices.Add(new AsioDeviceInfo
                        {
                            Id = driverName,
                            Name = driverName,
                            DriverName = driverName,
                            Status = "Unavailable"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate ASIO devices");
            }
            return devices;
        }

        public void UpdateOutputDevice(string deviceId)
        {
            try
            {
                Console.WriteLine($"UpdateOutputDevice called with: {deviceId}");
                _logger.LogInformation("Updating output device to: {DeviceId}", deviceId);

                // Dispose current output
                if (_soundOut != null)
                {
                    _soundOut.Stop();
                    _soundOut.Dispose();
                    _soundOut = null;
                    Console.WriteLine("Disposed current sound output");
                }

                // Determine device type and initialize new output
                if (deviceId.StartsWith("ASIO:"))
                {
                    var asioId = deviceId.Substring(5);
                    _soundOut = new AsioOut(asioId);
                    var asioOut = (AsioOut)_soundOut;
                    asioOut.ChannelOffset = _currentSettings.ChannelOffset;

                    // Handle ASIO events
                    asioOut.DriverResetRequest += OnAsioDriverReset;

                    // Log ASIO metrics
                    _logger.LogInformation("ASIO Driver: {Driver}", asioOut.DriverName);
                    _logger.LogInformation("ASIO Buffer Size: {Buffer} samples", asioOut.FramesPerBuffer);
                    _logger.LogInformation("ASIO Playback Latency: {Latency} samples ({Ms:F1}ms)", asioOut.PlaybackLatency, (double)asioOut.PlaybackLatency / asioOut.FramesPerBuffer * 1000);
                    _logger.LogInformation("ASIO Sample Rate Supported (44100): {Supported}", asioOut.IsSampleRateSupported(44100));

                    Console.WriteLine($"Initialized ASIO output: {asioId}, Buffer: {asioOut.FramesPerBuffer}, ChannelOffset: {asioOut.ChannelOffset}");
                    _logger.LogInformation("Switched to ASIO device: {Device}", asioId);
                }
                else if (deviceId.StartsWith("WaveOut:"))
                {
                    var waveOutNum = int.Parse(deviceId.Substring(8));
                    _soundOut = new WaveOutEvent { DeviceNumber = waveOutNum };
                    Console.WriteLine($"Initialized WaveOut output: device {waveOutNum}");
                    _logger.LogInformation("Switched to WaveOut device: {DeviceNumber}", waveOutNum);
                }
                else
                {
                    throw new ArgumentException($"Invalid device ID format: {deviceId}");
                }

                // Reinitialize with mixer
                _soundOut.Init(_waveProvider);
                Console.WriteLine("Reinitialized sound output with wave provider");
                _logger.LogInformation("Output device updated successfully");

                // Start health monitoring
                StartHealthMonitoring();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update output device to {DeviceId}", deviceId);
                Console.WriteLine($"Error updating output device: {ex.Message}");
                throw;
            }
        }

        public string GetSoundOutState()
        {
            return _soundOut?.PlaybackState.ToString() ?? "NoOutput";
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
