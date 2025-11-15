using System;
using CSCore;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class MixingProvider : ISampleSource
    {
        private readonly ILogger<MixingProvider> _logger;
        private ISampleSource? _deck1Source;
        private ISampleSource? _deck2Source;
        private float _crossfader = 0.5f; // 0 = deck1 full, 1 = deck2 full

        public float Crossfader
        {
            get => _crossfader;
            set
            {
                _crossfader = Math.Clamp(value, 0f, 1f);
                _logger.LogDebug($"Crossfader set to {_crossfader}");
            }
        }

        public WaveFormat WaveFormat { get; }

        public MixingProvider(WaveFormat waveFormat, ILogger<MixingProvider> logger)
        {
            WaveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
            _logger = logger;
        }

        public void SetDeckSource(int deckNumber, ISampleSource? source)
        {
            if (deckNumber == 1)
            {
                _deck1Source = source;
                _logger.LogInformation("Deck 1 source set");
            }
            else if (deckNumber == 2)
            {
                _deck2Source = source;
                _logger.LogInformation("Deck 2 source set");
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            // Clear buffer
            Array.Clear(buffer, offset, count);

            int samplesRead = 0;

            // Read from deck 1
            if (_deck1Source != null)
            {
                float[] deck1Buffer = new float[count];
                int deck1Read = _deck1Source.Read(deck1Buffer, 0, count);
                float gain1 = 1f - _crossfader; // More gain when crossfader is towards deck1

                for (int i = 0; i < deck1Read; i++)
                {
                    buffer[offset + i] += deck1Buffer[i] * gain1;
                }
                samplesRead = Math.Max(samplesRead, deck1Read);
            }

            // Read from deck 2
            if (_deck2Source != null)
            {
                float[] deck2Buffer = new float[count];
                int deck2Read = _deck2Source.Read(deck2Buffer, 0, count);
                float gain2 = _crossfader; // More gain when crossfader is towards deck2

                for (int i = 0; i < deck2Read; i++)
                {
                    buffer[offset + i] += deck2Buffer[i] * gain2;
                }
                samplesRead = Math.Max(samplesRead, deck2Read);
            }

            return samplesRead;
        }

        public void Dispose()
        {
            _deck1Source?.Dispose();
            _deck2Source?.Dispose();
        }
    }
}