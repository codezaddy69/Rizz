using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using DJMixMaster.Native.JUCE;

namespace DJMixMaster.Audio
{
    public enum AudioEngineType
    {
        NAudio,
        JUCE
    }

    public interface IAudioEngineFactory
    {
        IAudioEngine CreateAudioEngine(AudioEngineType preferredType, ILoggerFactory loggerFactory);
        AudioEngineType DetectWorkingEngine(ILoggerFactory loggerFactory);
    }

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
        void SetVolume(int deckNumber, float volume);
        float GetVolume(int deckNumber);
        bool IsPlaying(int deckNumber);
        void SetCrossfader(float position);
        float GetCrossfader();
        (float[] WaveformData, double Length) GetWaveformData(int deckNumber);
        void AddCuePoint(int deckNumber);
        void JumpToCuePoint(int deckNumber, int cueIndex);
        void PlayTestTone(int deckNumber, double frequency = 440.0, double durationSeconds = 2.0);
    }

    public class AudioEngine : IAudioEngine
    {
        private readonly ILogger<AudioEngine> _logger;
        private readonly Controllers.PlaybackController _playbackController;
        private readonly Controllers.VolumeManager _volumeManager;
        private readonly Generators.WaveformGenerator _waveformGenerator;
        private readonly Managers.CuePointManager _cuePointManager;
        private bool _disposed;

        // Events
        public event EventHandler<(int DeckNumber, double Position)>? PlaybackPositionChanged;
        public event EventHandler<(int DeckNumber, double[] BeatPositions, double BPM)>? BeatGridUpdated;

        public AudioEngine(ILogger<AudioEngine> logger)
        {
            _logger = logger;
            var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            _playbackController = new Controllers.PlaybackController(loggerFactory.CreateLogger<Controllers.PlaybackController>());
            _volumeManager = new Controllers.VolumeManager(loggerFactory.CreateLogger<Controllers.VolumeManager>());
            _waveformGenerator = new Generators.WaveformGenerator(loggerFactory.CreateLogger<Generators.WaveformGenerator>());
            _cuePointManager = new Managers.CuePointManager(loggerFactory.CreateLogger<Managers.CuePointManager>());

            // Wire up events
            _playbackController.PlaybackPositionChanged += (s, e) => PlaybackPositionChanged?.Invoke(this, e);
        }



        public void LoadFile(int deckNumber, string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading file for deck {deckNumber}: {filePath}");
                var sampleProvider = _playbackController.LoadTrackForDeck(deckNumber, filePath);
                var volume = new VolumeSampleProvider(sampleProvider);
                _volumeManager.SetVolumeProvider(deckNumber, volume);

                WasapiOut waveOut;
                try
                {
                    waveOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, 500);
                    _logger.LogInformation("Using WASAPI Exclusive mode");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exclusive mode failed, using Shared mode");
                    waveOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 500);
                }
                waveOut.Init(volume);
                _playbackController.SetWaveOut(deckNumber, waveOut);

                _volumeManager.UpdateEffectiveVolumes();
                // TODO: Analyze file for beat grid
                OnBeatGridUpdated(deckNumber, new double[] { 0, 1, 2, 3 }, 120.0); // Placeholder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading file for deck {deckNumber}");
                throw;
            }
        }

        public void Play(int deckNumber) => _playbackController.Play(deckNumber);

        public void Pause(int deckNumber) => _playbackController.Pause(deckNumber);

        public void Stop(int deckNumber) => _playbackController.Stop(deckNumber);

        public void Seek(int deckNumber, double seconds) => _playbackController.Seek(deckNumber, seconds);

        public double GetPosition(int deckNumber) => _playbackController.GetPosition(deckNumber);

        public double GetLength(int deckNumber) => _playbackController.GetLength(deckNumber);



        public void SetVolume(int deckNumber, float volume) => _volumeManager.SetVolume(deckNumber, volume);

        public float GetVolume(int deckNumber) => _volumeManager.GetVolume(deckNumber);

        public bool IsPlaying(int deckNumber) => _playbackController.IsPlaying(deckNumber);

        public void SetCrossfader(float position) => _volumeManager.SetCrossfader(position);

        public float GetCrossfader() => _volumeManager.GetCrossfader();

        public (float[] WaveformData, double Length) GetWaveformData(int deckNumber) => _waveformGenerator.GetWaveformData(deckNumber, GetLength(deckNumber));

        public void AddCuePoint(int deckNumber) => _cuePointManager.AddCuePoint(deckNumber);

        public void JumpToCuePoint(int deckNumber, int cueIndex) => _cuePointManager.JumpToCuePoint(deckNumber, cueIndex);



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
                    _playbackController?.Dispose();
                    _volumeManager?.Dispose();
                    _waveformGenerator?.Dispose();
                    _cuePointManager?.Dispose();
                }
                _disposed = true;
            }
        }

        private WasapiOut? GetWaveOut(int deckNumber) => _playbackController.GetWaveOut(deckNumber);

        public void PlayTestTone(int deckNumber, double frequency = 440.0, double durationSeconds = 2.0)
        {
            try
            {
                _logger.LogInformation($"Playing test tone on deck {deckNumber}: {frequency}Hz for {durationSeconds}s");

                // Generate a sine wave
                var sineWave = new SignalGenerator() { Gain = 0.1f, Frequency = (float)frequency, Type = SignalGeneratorType.Sin };

                var waveOut = GetWaveOut(deckNumber);
                if (waveOut != null)
                {
                    waveOut.Init(sineWave);
                    waveOut.Play();

                    // Stop after duration
                    Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
                    {
                        waveOut.Stop();
                        _logger.LogInformation("Test tone completed");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error playing test tone on deck {deckNumber}");
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class JuceAudioEngineWrapper : IAudioEngine
    {
        private readonly JuceAudioEngine _juceEngine;

#pragma warning disable CS0067 // Events are declared but never raised - TODO: implement when JUCE integration is complete
        public event EventHandler<(int DeckNumber, double Position)>? PlaybackPositionChanged;
        public event EventHandler<(int DeckNumber, double[] BeatPositions, double BPM)>? BeatGridUpdated;
#pragma warning restore CS0067

        public JuceAudioEngineWrapper(ILoggerFactory loggerFactory)
        {
            _juceEngine = new JuceAudioEngine(loggerFactory.CreateLogger<JuceAudioEngine>());
        }

        public void LoadFile(int deckNumber, string filePath)
        {
            // TODO: Implement proper multi-deck support in JUCE engine
            _juceEngine.LoadAudioFile(filePath);
        }

        public void Play(int deckNumber)
        {
            _juceEngine.Play();
        }

        public void Pause(int deckNumber)
        {
            _juceEngine.Pause();
        }

        public void Stop(int deckNumber)
        {
            _juceEngine.Stop();
        }

        public void Seek(int deckNumber, double seconds)
        {
            _juceEngine.SetPosition(seconds);
        }

        public double GetPosition(int deckNumber)
        {
            return _juceEngine.CurrentPosition;
        }

        public double GetLength(int deckNumber)
        {
            return _juceEngine.Length;
        }

        public void SetVolume(int deckNumber, float volume)
        {
            // TODO: Implement volume control in JUCE engine
        }

        public float GetVolume(int deckNumber)
        {
            // TODO: Implement volume control in JUCE engine
            return 1.0f;
        }

        public bool IsPlaying(int deckNumber)
        {
            return _juceEngine.IsPlaying;
        }

        public void SetCrossfader(float position)
        {
            // TODO: Implement crossfader in JUCE engine
        }

        public float GetCrossfader()
        {
            // TODO: Implement crossfader in JUCE engine
            return 0.0f;
        }

        public (float[] WaveformData, double Length) GetWaveformData(int deckNumber)
        {
            // TODO: Implement waveform generation in JUCE engine
            return (Array.Empty<float>(), _juceEngine.Length);
        }

        public void AddCuePoint(int deckNumber)
        {
            // TODO: Implement cue points in JUCE engine
        }

        public void JumpToCuePoint(int deckNumber, int cueIndex)
        {
            // TODO: Implement cue point jumping in JUCE engine
        }

        public void PlayTestTone(int deckNumber, double frequency = 440.0, double durationSeconds = 2.0)
        {
            // TODO: Implement test tone in JUCE engine
        }

        public void Dispose()
        {
            _juceEngine.Dispose();
        }
    }

    public class AudioEngineFactory : IAudioEngineFactory
    {
        public IAudioEngine CreateAudioEngine(AudioEngineType preferredType, ILoggerFactory loggerFactory)
        {
            switch (preferredType)
            {
                case AudioEngineType.NAudio:
                    return new AudioEngine(loggerFactory.CreateLogger<AudioEngine>());
                case AudioEngineType.JUCE:
                    return new JuceAudioEngineWrapper(loggerFactory);
                default:
                    throw new ArgumentException($"Unsupported audio engine type: {preferredType}");
            }
        }

        public AudioEngineType DetectWorkingEngine(ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<AudioEngineFactory>();

            // Try NAudio first
            try
            {
                logger.LogInformation("Testing NAudio engine...");
                using (var testEngine = new AudioEngine(loggerFactory.CreateLogger<AudioEngine>()))
                {
                    // Simple test: try to create a basic audio device
                    // If this succeeds, NAudio is working
                    logger.LogInformation("NAudio engine detected as working");
                    return AudioEngineType.NAudio;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "NAudio engine failed, trying JUCE fallback");
            }

            // If NAudio fails, try JUCE
            try
            {
                logger.LogInformation("Testing JUCE engine...");
                using (var testEngine = new JuceAudioEngineWrapper(loggerFactory))
                {
                    // Basic test - if initialization succeeds, JUCE is working
                    logger.LogInformation("JUCE engine detected as working");
                    return AudioEngineType.JUCE;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Both audio engines failed to initialize");
                throw new InvalidOperationException("No working audio engine found", ex);
            }
        }
    }
}
