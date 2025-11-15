using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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
        private readonly Deck _deck1;
        private readonly Deck _deck2;
        private readonly MixingSampleProvider _mixer;
        private readonly WasapiOut _soundOut;
        private float _crossfader = 0.5f;
        private bool _disposed;

        // Events
        public event EventHandler<(int DeckNumber, double Position)>? PlaybackPositionChanged;
        public event EventHandler<(int DeckNumber, double[] BeatPositions, double BPM)>? BeatGridUpdated;

        public AudioEngine(ILogger<AudioEngine> logger)
        {
            _logger = logger;
            var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());

            _deck1 = new Deck(1, loggerFactory.CreateLogger<Deck>());
            _deck2 = new Deck(2, loggerFactory.CreateLogger<Deck>());
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            _mixer.AddMixerInput(_deck1.VolumeProvider);
            _mixer.AddMixerInput(_deck2.VolumeProvider);

            // Initialize sound out
            _soundOut = new WasapiOut();
            _soundOut.Init(_mixer);

            _logger.LogInformation("NAudio AudioEngine initialized");
        }



        public void LoadFile(int deckNumber, string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading file for deck {deckNumber}: {filePath}");

                Deck deck = deckNumber == 1 ? _deck1 : _deck2;
                deck.LoadFile(filePath);

                // Mixer already has the volume providers

                // TODO: Analyze file for beat grid
                OnBeatGridUpdated(deckNumber, new double[] { 0, 1, 2, 3 }, 120.0); // Placeholder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading file for deck {deckNumber}");
                throw;
            }
        }

        public void Play(int deckNumber)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            deck.Play();
            if (!_soundOut.PlaybackState.HasFlag(PlaybackState.Playing))
            {
                _soundOut.Play();
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



        public void SetVolume(int deckNumber, float volume)
        {
            Deck deck = deckNumber == 1 ? _deck1 : _deck2;
            deck.Volume = volume;
            deck.UpdateVolume();
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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
