using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DJMixMaster.Audio
{
    public class AudioEngine : IDisposable
    {
        private IWavePlayer outputDevice;
        private readonly Dictionary<int, DeckPlayer> decks;
        private MixingSampleProvider mixer;
        private const int SAMPLE_RATE = 44100;
        private const int CHANNELS = 2;

        public AudioEngine()
        {
            decks = new Dictionary<int, DeckPlayer>
            {
                { 1, new DeckPlayer(SAMPLE_RATE, CHANNELS) },
                { 2, new DeckPlayer(SAMPLE_RATE, CHANNELS) }
            };

            InitializeAudioOutput();
        }

        private void InitializeAudioOutput()
        {
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, CHANNELS));
            mixer.ReadFully = true;

            foreach (var deck in decks.Values)
            {
                mixer.AddMixerInput(deck.GetSampleProvider());
            }

            outputDevice = new WasapiOut();
            outputDevice.Init(mixer);
        }

        public void LoadTrack(int deckNumber, string filePath)
        {
            if (decks.ContainsKey(deckNumber))
            {
                decks[deckNumber].LoadTrack(filePath);
            }
        }

        public void Play(int deckNumber)
        {
            if (decks.ContainsKey(deckNumber))
            {
                decks[deckNumber].Play();
                if (outputDevice.PlaybackState != PlaybackState.Playing)
                {
                    outputDevice.Play();
                }
            }
        }

        public void Pause(int deckNumber)
        {
            if (decks.ContainsKey(deckNumber))
            {
                decks[deckNumber].Pause();
            }
        }

        public void SetVolume(int deckNumber, float volume)
        {
            if (decks.ContainsKey(deckNumber))
            {
                decks[deckNumber].SetVolume(volume);
            }
        }

        public void SetCrossfader(float position)
        {
            // -1 to 1 range, where -1 is full left, 0 is center, 1 is full right
            float leftVolume = (position <= 0) ? 1.0f : 1.0f - position;
            float rightVolume = (position >= 0) ? 1.0f : 1.0f + position;

            decks[1].SetVolume(leftVolume);
            decks[2].SetVolume(rightVolume);
        }

        public void Seek(int deckNumber, TimeSpan position)
        {
            if (decks.ContainsKey(deckNumber))
            {
                decks[deckNumber].Seek(position);
            }
        }

        public float[] GetWaveformData(int deckNumber)
        {
            return decks.ContainsKey(deckNumber) ? decks[deckNumber].GetWaveformData() : Array.Empty<float>();
        }

        public void Dispose()
        {
            outputDevice?.Dispose();
            foreach (var deck in decks.Values)
            {
                deck.Dispose();
            }
        }
    }
}
