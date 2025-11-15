using System;
using NAudio.Wave;
using Microsoft.Extensions.Logging;
using DJMixMaster.Controls;

namespace DJMixMaster.Audio.Providers
{
    public class DeckVolumeProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly ILogger logger;
        private readonly object volumeLock = new object();
        private float volume = 1.0f;
        private readonly Fader faderLeft;
        private readonly Fader faderRight;

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;
        
        public float Volume
        {
            get { lock (volumeLock) { return volume; } }
            set
            {
                lock (volumeLock)
                {
                    volume = Math.Clamp(value, 0f, 1f);
                    logger.LogDebug($"Volume set to {volume:F2}");
                }
            }
        }

        public DeckVolumeProvider(ISampleProvider sourceProvider, float initialVolume, Fader faderLeft, Fader faderRight, ILogger logger)
        {
            this.sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
            this.faderLeft = faderLeft ?? throw new ArgumentNullException(nameof(faderLeft));
            this.faderRight = faderRight ?? throw new ArgumentNullException(nameof(faderRight));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Volume = initialVolume;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);
            
            if (samplesRead > 0)
            {
                float leftVolume = (float)(faderLeft.Value / faderLeft.Maximum);
                float rightVolume = (float)(faderRight.Value / faderRight.Maximum);
                float currentVolume;

                lock (volumeLock)
                {
                    currentVolume = volume;
                }

                for (int i = 0; i < samplesRead; i += 2)
                {
                    if (i < samplesRead)
                        buffer[offset + i] *= currentVolume * leftVolume;     // Left channel
                    if (i + 1 < samplesRead)
                        buffer[offset + i + 1] *= currentVolume * rightVolume; // Right channel
                }
            }

            return samplesRead;
        }

        public void UpdateFaderVolumes()
        {
            float leftVolume = (float)(faderLeft.Value / faderLeft.Maximum);
            float rightVolume = (float)(faderRight.Value / faderRight.Maximum);
            logger.LogDebug($"Adjusting volume: Left = {leftVolume}, Right = {rightVolume}");
            
            Volume = (leftVolume + rightVolume) / 2;
        }
    }
}
