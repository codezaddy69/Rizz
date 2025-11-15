using System;
using Microsoft.Extensions.Logging;
using NAudio.Wave.SampleProviders;

namespace DJMixMaster.Audio.Controllers
{
    public class VolumeManager
    {
        private readonly ILogger<VolumeManager> _logger;
        private float _crossfaderPosition = 0.0f; // -1 = full left, 0 = center, 1 = full right
        private float _deck1BaseVolume = 1.0f;
        private float _deck2BaseVolume = 1.0f;
        private VolumeSampleProvider? _volume1;
        private VolumeSampleProvider? _volume2;

        public VolumeManager(ILogger<VolumeManager> logger)
        {
            _logger = logger;
        }

        public void SetVolumeProvider(int deckNumber, VolumeSampleProvider volumeProvider)
        {
            if (deckNumber == 1) _volume1 = volumeProvider;
            else _volume2 = volumeProvider;
            UpdateEffectiveVolumes();
        }

        public void UpdateEffectiveVolumes()
        {
            // Apply crossfader to volumes with proper clamping
            float leftCrossfaderMultiplier = Math.Max(0, 1.0f - _crossfaderPosition);
            float rightCrossfaderMultiplier = Math.Max(0, 1.0f + _crossfaderPosition);

            if (_volume1 != null)
            {
                var newVolume1 = Math.Min(1.0f, _deck1BaseVolume * leftCrossfaderMultiplier);
                if (Math.Abs(_volume1.Volume - newVolume1) > 0.01f) // Only update if significant change
                {
                    _volume1.Volume = newVolume1;
                }
            }
            if (_volume2 != null)
            {
                var newVolume2 = Math.Min(1.0f, _deck2BaseVolume * rightCrossfaderMultiplier);
                if (Math.Abs(_volume2.Volume - newVolume2) > 0.01f) // Only update if significant change
                {
                    _volume2.Volume = newVolume2;
                }
            }
        }

        public void SetVolume(int deckNumber, float volume)
        {
            try
            {
                _logger.LogDebug($"Setting volume for deck {deckNumber} to {volume}");
                SetBaseVolume(deckNumber, volume);
                UpdateEffectiveVolumes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting volume for deck {deckNumber}");
                throw;
            }
        }

        public float GetVolume(int deckNumber)
        {
            return GetBaseVolume(deckNumber);
        }

        public void SetCrossfader(float position)
        {
            try
            {
                _logger.LogDebug($"Setting crossfader position to {position}");
                _crossfaderPosition = Math.Clamp(position, -1.0f, 1.0f); // Clamp to valid range
                UpdateEffectiveVolumes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting crossfader position");
                throw;
            }
        }

        public float GetCrossfader()
        {
            return _crossfaderPosition;
        }

        private float GetBaseVolume(int deckNumber) => deckNumber == 1 ? _deck1BaseVolume : _deck2BaseVolume;
        private void SetBaseVolume(int deckNumber, float volume)
        {
            if (deckNumber == 1) _deck1BaseVolume = volume;
            else _deck2BaseVolume = volume;
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}