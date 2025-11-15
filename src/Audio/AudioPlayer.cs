using System;
using System.IO;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class AudioPlayer
    {
        private IWavePlayer waveOutDevice;
        private AudioFileReader? audioFileReader;
        private readonly ILogger<AudioPlayer> logger;

        public AudioPlayer(ILogger<AudioPlayer> logger)
        {
            this.logger = logger;
            waveOutDevice = new WaveOutEvent();
        }

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                logger.LogError($"File not found: {filePath}");
                throw new FileNotFoundException("Audio file not found.", filePath);
            }

            audioFileReader = new AudioFileReader(filePath);
            waveOutDevice.Init(audioFileReader);
            logger.LogInformation($"Loaded audio file: {filePath}");
        }

        public void Play()
        {
            if (waveOutDevice.PlaybackState != PlaybackState.Playing)
            {
                waveOutDevice.Play();
                logger.LogInformation("Playback started.");
            }
        }

        public void Pause()
        {
            if (waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                waveOutDevice.Pause();
                logger.LogInformation("Playback paused.");
            }
        }

        public void Stop()
        {
            if (waveOutDevice.PlaybackState != PlaybackState.Stopped)
            {
                waveOutDevice.Stop();
                logger.LogInformation("Playback stopped.");
            }
        }

        public void Dispose()
        {
            waveOutDevice.Dispose();
            audioFileReader?.Dispose();
            logger.LogInformation("Audio resources disposed.");
        }
    }
}
