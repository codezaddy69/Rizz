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
        void SetCrossfader(float position);
        float GetCrossfader();
        (float[] WaveformData, double Length) GetWaveformData(int deckNumber);
        void AddCuePoint(int deckNumber);
        void JumpToCuePoint(int deckNumber, int cueIndex);
        void PlayTestTone(int deckNumber, double frequency = 440.0, double durationSeconds = 2.0);
        void UpdateAudioSettings(AudioSettings settings);
        AudioSettings GetCurrentSettings();
        List<AsioDeviceInfo> EnumerateDevices();
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
        private readonly MixingSampleProvider _mixer;
        private readonly IWavePlayer _soundOut;
        private readonly SampleToWaveProvider _waveProvider;
        private float _crossfader = 0.5f;
        private AudioSettings _currentSettings;
        private bool _disposed;

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
                _mixer = new MixingSampleProvider(waveFormat);
                _logger.LogInformation("Mixer initialized with format: {SampleRate}Hz, {Channels}ch", waveFormat.SampleRate, waveFormat.Channels);

                // Connect permanent deck providers to mixer for continuous pipeline
                _mixer.SetProvider(0, _deck1.SampleProvider);
                _mixer.SetProvider(1, _deck2.SampleProvider);
                _logger.LogInformation("Permanent deck inputs connected to mixer");

                // Convert to wave provider for output (abstraction layer)
                _waveProvider = new SampleToWaveProvider(_mixer);

                // Initialize output device with ASIO for low-latency pro audio
                _logger.LogInformation("Using ASIO for ultra-low latency audio output");
                try
                {
                    _soundOut = new AsioOut(0); // First ASIO device (buffer size set by driver for low latency)
                    _soundOut.Init(_waveProvider);
                    _logger.LogInformation("ASIO initialized successfully on first available device");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ASIO initialization failed - falling back to WaveOut");
                    _soundOut = new WaveOut();
                    _soundOut.Init(_waveProvider);
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
                Deck deck = deckNumber == 1 ? _deck1 : _deck2;
                deck.Play();
                if (_soundOut.PlaybackState != PlaybackState.Playing)
                {
                    _soundOut.Play();
                    _logger.LogInformation("Started playback on deck {DeckNumber}, ASIO state: {State}", deckNumber, _soundOut.PlaybackState);
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
                _soundOut.Pause();
            }
        }

        public void Stop(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            deck.Stop();
            if (!_deck1.IsPlaying && !_deck2.IsPlaying)
            {
                _soundOut.Stop();
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
                // ASIO settings would be applied here
                _logger.LogInformation("ASIO settings updated - some changes may require restart");
            }
        }

        public AudioSettings GetCurrentSettings()
        {
            return _currentSettings;
        }

        public List<AsioDeviceInfo> EnumerateDevices()
        {
            var devices = new List<AsioDeviceInfo>();
            try
            {
                string[] driverNames = AsioOut.GetDriverNames();
                foreach (string driverName in driverNames)
                {
                    devices.Add(new AsioDeviceInfo
                    {
                        Id = driverName,
                        Name = driverName,
                        DriverName = driverName,
                        Status = "Available"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate ASIO devices");
            }
            return devices;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
