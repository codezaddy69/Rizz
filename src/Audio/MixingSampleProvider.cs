using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// A sample provider that mixes multiple audio streams with built-in clipping prevention.
    /// Follows Single Responsibility Principle by handling only audio mixing.
    /// Implements ISampleProvider for compatibility with NAudio pipeline.
    /// </summary>
    public class MixingSampleProvider : ISampleProvider
    {
        private readonly List<ISampleProvider?> _providers = new();
        private readonly WaveFormat _waveFormat;

        /// <summary>
        /// Gets the wave format of the mixed output.
        /// </summary>
        public WaveFormat WaveFormat => _waveFormat;

        /// <summary>
        /// Initializes a new instance of the MixingSampleProvider class.
        /// </summary>
        /// <param name="waveFormat">The wave format for the output.</param>
        /// <exception cref="ArgumentNullException">Thrown if waveFormat is null.</exception>
        public MixingSampleProvider(WaveFormat waveFormat)
        {
            _waveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
        }

        /// <summary>
        /// Adds a sample provider to the mix.
        /// </summary>
        /// <param name="provider">The sample provider to add.</param>
        /// <param name="index">The index at which to add the provider.</param>
        public void SetProvider(int index, ISampleProvider? provider)
        {
            while (_providers.Count <= index)
            {
                _providers.Add(null);
            }
            _providers[index] = provider;
        }

        /// <summary>
        /// Reads samples from all providers, mixes them, and applies clipping prevention.
        /// </summary>
        /// <param name="buffer">The buffer to fill with mixed samples.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            // Clear the buffer to prepare for mixing
            Array.Clear(buffer, offset, count);

            int maxSamplesRead = 0;

            // Read from each provider and mix
            foreach (var provider in _providers)
            {
                if (provider != null)
                {
                    float[] tempBuffer = new float[count];
                    int samplesRead = provider.Read(tempBuffer, 0, count);

                    // Mix into the main buffer
                    for (int i = 0; i < samplesRead; i++)
                    {
                        buffer[offset + i] += tempBuffer[i];
                    }

                    maxSamplesRead = Math.Max(maxSamplesRead, samplesRead);
                }
            }

            // Apply clipping prevention to avoid distortion
            for (int i = 0; i < maxSamplesRead; i++)
            {
                buffer[offset + i] = Math.Clamp(buffer[offset + i], -1.0f, 1.0f);
            }

            return maxSamplesRead;
        }
    }
}