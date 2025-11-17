using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace DJMixMaster.Audio
{
    public class MixingSampleProvider : ISampleProvider
    {
        private readonly List<ISampleProvider?> _providers = new();
        private readonly WaveFormat _waveFormat;
        private readonly StreamWriter _logWriter;
        private int _readCount = 0;

        public WaveFormat WaveFormat => _waveFormat;

        public MixingSampleProvider(WaveFormat waveFormat)
        {
            _waveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));

            // Create timestamped log file
            string logFileName = $"log{DateTime.Now:yyyyMMdd_HHmm}.txt";
            _logWriter = new StreamWriter(logFileName, false);
            _logWriter.WriteLine($"Session started: {DateTime.Now}");
            _logWriter.WriteLine($"WaveFormat: {_waveFormat.SampleRate}Hz, {_waveFormat.BitsPerSample}bit, {_waveFormat.Channels}ch");
            _logWriter.Flush();
        }

        public void SetProvider(int index, ISampleProvider? provider)
        {
            while (_providers.Count <= index)
            {
                _providers.Add(null);
            }
            _providers[index] = provider;
            _logWriter.WriteLine($"Set provider {index}: {(provider != null ? "active" : "null")}");
            _logWriter.Flush();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            // Clear buffer
            Array.Clear(buffer, offset, count);
            int maxRead = 0;
            float maxPreSum = 0f;
            float maxPostSum = 0f;

            foreach (var provider in _providers)
            {
                if (provider != null)
                {
                    float[] tempBuffer = new float[count];
                    int read = provider.Read(tempBuffer, 0, count);

                    // Normalize each provider to prevent overdriving (soft limit at 0.8)
                    float maxLevel = 0f;
                    for (int i = 0; i < read; i++)
                    {
                        maxLevel = Math.Max(maxLevel, Math.Abs(tempBuffer[i]));
                    }
                    float gain = maxLevel > 0.8f ? 0.8f / maxLevel : 1.0f;

                    // Track max pre-sum
                    for (int i = 0; i < read; i++)
                    {
                        float sample = tempBuffer[i] * gain;
                        maxPreSum = Math.Max(maxPreSum, Math.Abs(sample));
                        buffer[offset + i] += sample;
                        maxPostSum = Math.Max(maxPostSum, Math.Abs(buffer[offset + i]));
                    }
                    maxRead = Math.Max(maxRead, read);
                }
            }

            // Soft clipping to prevent harsh distortion
            float maxPostClamp = 0f;
            for (int i = 0; i < maxRead; i++)
            {
                float sample = buffer[offset + i];
                // Soft clip: tanh approximation for smoother limiting
                sample = (float)(Math.Tanh(sample) * 0.9); // Scale to avoid hard 1.0
                buffer[offset + i] = sample;
                maxPostClamp = Math.Max(maxPostClamp, Math.Abs(sample));
            }

            // Log every 100 reads to avoid spam
            if (_readCount++ % 100 == 0)
            {
                _logWriter.WriteLine($"Read {_readCount}: samples={maxRead}, pre-sum max={maxPreSum:F3}, post-sum max={maxPostSum:F3}, post-clamp max={maxPostClamp:F3}");
                _logWriter.Flush();
            }

            return maxRead;
        }

        ~MixingSampleProvider()
        {
            _logWriter?.Dispose();
        }
    }
}