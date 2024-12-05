using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace DJMixMaster.Audio
{
    public class DeckPlayer : IDisposable
    {
        private AudioFileReader audioFile;
        private VolumeSampleProvider volumeProvider;
        private WaveFormat waveFormat;
        private float[] waveformData;
        private float volume = 1.0f;
        private float speed = 1.0f;

        public DeckPlayer(int sampleRate, int channels)
        {
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }

        public void LoadTrack(string filePath)
        {
            DisposeCurrentTrack();

            audioFile = new AudioFileReader(filePath);
            volumeProvider = new VolumeSampleProvider(audioFile.ToSampleProvider());
            volumeProvider.Volume = volume;

            // Generate waveform data
            GenerateWaveformData();
        }

        private void GenerateWaveformData()
        {
            if (audioFile == null) return;

            // Reset position to start
            audioFile.Position = 0;

            // Calculate number of samples for visualization
            int samplesPerPixel = (int)(audioFile.Length / (audioFile.WaveFormat.BlockAlign * 1000));
            waveformData = new float[1000]; // Store 1000 points for visualization

            float maxValue = 0;
            int readCount = 0;
            float[] buffer = new float[samplesPerPixel];

            for (int i = 0; i < waveformData.Length; i++)
            {
                readCount = audioFile.Read(buffer, 0, buffer.Length);
                if (readCount == 0) break;

                float sum = 0;
                for (int j = 0; j < readCount; j++)
                {
                    float abs = Math.Abs(buffer[j]);
                    sum += abs;
                    if (abs > maxValue) maxValue = abs;
                }

                waveformData[i] = sum / readCount;
            }

            // Normalize waveform data
            if (maxValue > 0)
            {
                for (int i = 0; i < waveformData.Length; i++)
                {
                    waveformData[i] /= maxValue;
                }
            }

            // Reset position to start
            audioFile.Position = 0;
        }

        public void Play()
        {
            if (audioFile != null && audioFile.Position >= audioFile.Length)
            {
                audioFile.Position = 0;
            }
        }

        public void Pause()
        {
            // Handled by AudioEngine
        }

        public void SetVolume(float newVolume)
        {
            volume = Math.Clamp(newVolume, 0f, 1f);
            if (volumeProvider != null)
            {
                volumeProvider.Volume = volume;
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Math.Clamp(newSpeed, 0.5f, 2f);
            // Implement pitch/speed control
        }

        public void Seek(TimeSpan position)
        {
            if (audioFile != null)
            {
                audioFile.CurrentTime = position;
            }
        }

        public ISampleProvider GetSampleProvider()
        {
            return volumeProvider ?? new SilenceProvider(waveFormat).ToSampleProvider();
        }

        public float[] GetWaveformData()
        {
            return waveformData ?? Array.Empty<float>();
        }

        private void DisposeCurrentTrack()
        {
            audioFile?.Dispose();
            audioFile = null;
            volumeProvider = null;
        }

        public void Dispose()
        {
            DisposeCurrentTrack();
        }
    }
}
