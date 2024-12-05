using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DJMixMaster.Audio
{
    public class AudioEngine : IDisposable
    {
        private IWavePlayer? outputDevice;
        private readonly Dictionary<int, DeckPlayer> decks;
        private MixingSampleProvider? mixer;
        private const int SAMPLE_RATE = 44100;
        private const int CHANNELS = 2;
        private bool isDisposed;

        public event EventHandler<(int DeckNumber, double Position)>? PlaybackPositionChanged;
        public event EventHandler<(int DeckNumber, List<double> BeatPositions, double BPM)>? BeatGridUpdated;

        public AudioEngine()
        {
            try
            {
                decks = new Dictionary<int, DeckPlayer>
                {
                    { 1, new DeckPlayer(SAMPLE_RATE, CHANNELS) },
                    { 2, new DeckPlayer(SAMPLE_RATE, CHANNELS) }
                };

                // Wire up playback position tracking for each deck
                foreach (var deck in decks)
                {
                    int deckNumber = deck.Key;
                    deck.Value.PlaybackPositionChanged += (s, position) => 
                        PlaybackPositionChanged?.Invoke(this, (deckNumber, position));
                }

                InitializeAudioOutput();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize audio engine: {ex.Message}", ex);
            }
        }

        private void InitializeAudioOutput()
        {
            try
            {
                var format = WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, CHANNELS);
                mixer = new MixingSampleProvider(format);
                mixer.ReadFully = true;

                foreach (var deck in decks.Values)
                {
                    mixer.AddMixerInput(deck.GetSampleProvider());
                }

                outputDevice?.Dispose();
                outputDevice = new WasapiOut();
                outputDevice.Init(mixer);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize audio output: {ex.Message}", ex);
            }
        }

        public void LoadTrack(int deckNumber, string filePath)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].LoadTrack(filePath);
                
                // Notify about the beat grid
                BeatGridUpdated?.Invoke(this, (
                    deckNumber,
                    decks[deckNumber].BeatPositions,
                    decks[deckNumber].BPM
                ));

                // Reinitialize audio output to update the mixer inputs
                InitializeAudioOutput();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load track on deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void Play(int deckNumber)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].Play();
                if (outputDevice?.PlaybackState != PlaybackState.Playing)
                {
                    outputDevice?.Play();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to play deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void Pause(int deckNumber)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].Pause();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to pause deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void SetVolume(int deckNumber, float volume)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].SetVolume(volume);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set volume for deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void SetCrossfader(float position)
        {
            try
            {
                // -1 to 1 range, where -1 is full left, 0 is center, 1 is full right
                float leftVolume = (position <= 0) ? 1.0f : 1.0f - position;
                float rightVolume = (position >= 0) ? 1.0f : 1.0f + position;

                decks[1].SetVolume(leftVolume);
                decks[2].SetVolume(rightVolume);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set crossfader: {ex.Message}", ex);
            }
        }

        public void Seek(int deckNumber, TimeSpan position)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].Seek(position);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to seek on deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void AddCuePoint(int deckNumber)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].AddCuePoint();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add cue point on deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void JumpToCuePoint(int deckNumber, int cueIndex)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].JumpToCuePoint(cueIndex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to jump to cue point on deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public (float[] WaveformData, double TrackLength) GetWaveformData(int deckNumber)
        {
            if (!decks.ContainsKey(deckNumber))
                return (Array.Empty<float>(), 0);

            try
            {
                return (decks[deckNumber].GetWaveformData(), decks[deckNumber].GetTrackLength());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get waveform data for deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                outputDevice?.Dispose();
                foreach (var deck in decks.Values)
                {
                    deck.Dispose();
                }
            }
        }
    }
}
