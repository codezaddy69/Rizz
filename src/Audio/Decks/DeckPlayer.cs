using System;
using NAudio.Wave;
using Microsoft.Extensions.Logging;
using DJMixMaster.Controls;
using DJMixMaster.Audio.Providers;

namespace DJMixMaster.Audio
{
    public class DeckPlayer : IDisposable
    {
        private readonly ILogger logger;
        private readonly DeckPlayerAudioProcessing audioProcessing;
        private readonly DeckPlayerNavigation navigation;
        private readonly DeckPlayerCuePoints cuePoints;
        private DeckVolumeProvider? volumeProvider;
        private bool isDisposed;

        public event EventHandler<double>? PlaybackPositionChanged;
        public double CurrentPosition => navigation.CurrentPosition;
        public bool IsPlaying => navigation.IsPlaying;
        public float[] WaveformData => audioProcessing.WaveformData;

        public DeckPlayer(ILogger logger, Fader faderLeft, Fader faderRight)
        {
            this.logger = logger;
            audioProcessing = new DeckPlayerAudioProcessing(logger);
            navigation = new DeckPlayerNavigation(audioProcessing, logger);
            cuePoints = new DeckPlayerCuePoints(this, logger);
            volumeProvider = new DeckVolumeProvider(new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)).ToSampleProvider(), 1.0f, faderLeft ?? new Fader(), faderRight ?? new Fader(), logger);

            // Wire up events
            navigation.PlaybackPositionChanged += (s, position) => PlaybackPositionChanged?.Invoke(this, position);
        }

        public ISampleProvider GetSampleProvider()
        {
            if (!IsPlaying || audioProcessing.AudioFile == null)
            {
                return new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)).ToSampleProvider();
            }
            return volumeProvider ?? new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)).ToSampleProvider();
        }

        public float[] GetWaveformData()
        {
            return audioProcessing.WaveformData;
        }

        public double GetTrackLength()
        {
            return audioProcessing.TrackLength;
        }

        public void LoadAudioFile(string filePath)
        {
            audioProcessing.LoadAudioFile(filePath);
            if (volumeProvider == null)
            {
                volumeProvider = new DeckVolumeProvider(new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)).ToSampleProvider(), 1.0f, new Fader(), new Fader(), logger);
            }
        }

        public void Play()
        {
            if (audioProcessing.AudioFile == null)
            {
                logger.LogWarning("Cannot play: No audio file loaded");
                return;
            }

            navigation.SetPlaybackState(true);
            logger.LogDebug("Playback started");
        }

        public void Pause()
        {
            navigation.SetPlaybackState(false);
            logger.LogDebug("Playback paused");
        }

        public void Stop()
        {
            navigation.SetPlaybackState(false);
            Seek(TimeSpan.Zero);
            logger.LogDebug("Playback stopped");
        }

        public void SetVolume(float newVolume)
        {
            if (volumeProvider != null)
            {
                volumeProvider.Volume = newVolume;
                logger.LogDebug($"Volume set to: {newVolume}");
            }
            else
            {
                logger.LogWarning("VolumeProvider is null in SetVolume method.");
            }
        }

        public void Seek(TimeSpan position)
        {
            navigation.Seek(position);
        }

        public void FastForward(double seconds)
        {
            navigation.FastForward(seconds);
        }

        public void Rewind(double seconds)
        {
            navigation.Rewind(seconds);
        }

        public void SetSpeed(float newSpeed)
        {
            navigation.Speed = newSpeed;
        }

        public void AddCuePoint(double position)
        {
            cuePoints.AddCuePoint(position);
        }

        public void JumpToCuePoint(int index)
        {
            cuePoints.JumpToCuePoint(index);
        }

        public double[] GetCuePoints()
        {
            return cuePoints.GetCuePoints();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    navigation.StopPositionTracking();
                    audioProcessing.CleanupAudio();
                }
                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
