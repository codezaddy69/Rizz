using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using DJMixMaster.Controls;

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
        public event EventHandler<(int DeckNumber, double[] BeatPositions, double BPM)>? BeatGridUpdated;

        public AudioEngine()
        {
            try
            {
                Logger.LogDebug("Initializing AudioEngine...");
                var faderLeft = new FaderControl();
                var faderRight = new FaderControl();
                decks = new Dictionary<int, DeckPlayer>
                {
                    { 1, new DeckPlayer(faderLeft, faderRight) },
                    { 2, new DeckPlayer(faderLeft, faderRight) }
                };

                InitializeAudioOutput();

                // Wire up playback position tracking for each deck
                foreach (var deck in decks)
                {
                    int deckNumber = deck.Key;
                    deck.Value.PlaybackPositionChanged += (s, position) => 
                        PlaybackPositionChanged?.Invoke(this, (deckNumber, position));
                }

                Logger.LogDebug("AudioEngine initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize AudioEngine: {ex}", ex);
                throw new Exception($"Failed to initialize audio engine: {ex.Message}", ex);
            }
        }

        private void InitializeAudioOutput()
        {
            try
            {
                Logger.LogDebug("Initializing audio output...");
                
                // Clean up existing output
                if (outputDevice != null)
                {
                    Logger.LogDebug("Cleaning up existing output device");
                    outputDevice.Stop();
                    outputDevice.Dispose();
                    outputDevice = null;
                }

                // Create mixer with default format
                var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
                mixer = new MixingSampleProvider(format)
                {
                    ReadFully = true
                };

                // Get the default audio device
                var enumerator = new MMDeviceEnumerator();
                var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                Logger.LogDebug($"Using audio device: {defaultDevice.FriendlyName}");

                // Create output device using WASAPI
                outputDevice = new WasapiOut(defaultDevice, NAudio.CoreAudioApi.AudioClientShareMode.Shared, true, 50);
                Logger.LogDebug($"Created WASAPI output with format: {format}");
                
                // Initialize output
                if (outputDevice != null && mixer != null)
                {
                    outputDevice.Init(mixer);
                    Logger.LogDebug("Audio output initialized successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize audio output", ex);
                throw;
            }
        }

        public void LoadFile(int deckNumber, string filePath)
        {
            if (!decks.ContainsKey(deckNumber))
            {
                Logger.LogWarning($"Invalid deck number: {deckNumber}");
                return;
            }

            try
            {
                Logger.LogDebug($"Loading file into deck {deckNumber}: {filePath}");
                
                // Load the file into the deck
                decks[deckNumber].LoadAudioFile(filePath);

                // Reinitialize audio output with the new deck
                InitializeAudioOutput();

                // Add the deck's provider to the mixer
                if (mixer != null)
                {
                    var provider = decks[deckNumber].GetSampleProvider();
                    Logger.LogDebug($"Adding deck {deckNumber} to mixer");
                    mixer.AddMixerInput(provider);
                }

                // Raise the beat grid updated event
                BeatGridUpdated?.Invoke(this, (deckNumber, Array.Empty<double>(), 0.0));
                Logger.LogDebug($"File loaded successfully into deck {deckNumber}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load file into deck {deckNumber}", ex);
                throw;
            }
        }

        public void Play(int deckNumber)
        {
            if (!decks.ContainsKey(deckNumber))
            {
                Logger.LogWarning($"Invalid deck number: {deckNumber}");
                return;
            }

            try
            {
                Logger.LogDebug($"Playing deck {deckNumber}");
                
                // Ensure output is initialized
                if (outputDevice == null || mixer == null)
                {
                    Logger.LogDebug("Reinitializing audio output");
                    InitializeAudioOutput();
                }

                // Start deck playback
                decks[deckNumber].Play();
                
                // Start output if not playing
                if (outputDevice?.PlaybackState != PlaybackState.Playing)
                {
                    Logger.LogDebug("Starting output device");
                    outputDevice?.Play();
                    Logger.LogDebug($"Output state: {outputDevice?.PlaybackState}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to play deck {deckNumber}", ex);
                throw;
            }
        }

        public void Pause(int deckNumber)
        {
            if (!decks.ContainsKey(deckNumber)) return;

            try
            {
                decks[deckNumber].Pause();
                
                // Check if both decks are paused
                bool allDecksPaused = true;
                foreach (var deck in decks)
                {
                    if (deck.Value.IsPlaying)
                    {
                        allDecksPaused = false;
                        break;
                    }
                }

                // If all decks are paused, pause the output device
                if (allDecksPaused && outputDevice != null)
                {
                    outputDevice.Pause();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error pausing track: {ex}", ex);
                throw new Exception($"Failed to pause track: {ex.Message}", ex);
            }
        }

        public void SetVolume(int deckNumber, float volume)
        {
            if (!decks.ContainsKey(deckNumber))
            {
                Logger.LogWarning($"Deck {deckNumber} does not exist in the decks dictionary.");
                return;
            }

            try
            {
                Logger.LogDebug($"Setting volume for deck {deckNumber} to {volume}");
                decks[deckNumber].SetVolume(volume);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to set volume for deck {deckNumber}: {ex}", ex);
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
                Logger.LogError($"Failed to set crossfader: {ex}", ex);
                throw new Exception($"Failed to set crossfader: {ex.Message}", ex);
            }
        }

        public void Seek(int deckNumber, TimeSpan position)
        {
            if (!decks.ContainsKey(deckNumber))
            {
                Logger.LogWarning($"Invalid deck number: {deckNumber}");
                return;
            }

            try
            {
                Logger.LogDebug($"Seeking deck {deckNumber} to {position}");
                decks[deckNumber].Seek(position);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to seek on deck {deckNumber}: {ex}", ex);
                throw new Exception($"Failed to seek on deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void AddCuePoint(int deckNumber)
        {
            if (!decks.ContainsKey(deckNumber))
            {
                Logger.LogWarning($"Invalid deck number: {deckNumber}");
                return;
            }

            try
            {
                Logger.LogDebug($"Adding cue point on deck {deckNumber}");
                decks[deckNumber].AddCuePoint();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to add cue point on deck {deckNumber}: {ex}", ex);
                throw new Exception($"Failed to add cue point on deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void JumpToCuePoint(int deckNumber, int cueIndex)
        {
            if (!decks.ContainsKey(deckNumber))
            {
                Logger.LogWarning($"Invalid deck number: {deckNumber}");
                return;
            }

            try
            {
                Logger.LogDebug($"Jumping to cue point {cueIndex} on deck {deckNumber}");
                decks[deckNumber].JumpToCuePoint(cueIndex);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to jump to cue point on deck {deckNumber}: {ex}", ex);
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
                Logger.LogError($"Failed to get waveform data for deck {deckNumber}: {ex}", ex);
                throw new Exception($"Failed to get waveform data for deck {deckNumber}: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                try
                {
                    outputDevice?.Stop();
                    outputDevice?.Dispose();
                    foreach (var deck in decks.Values)
                    {
                        deck.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error during disposal: {ex}", ex);
                }
            }
        }
    }
}
