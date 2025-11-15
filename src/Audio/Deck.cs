using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class Deck : IDisposable
    {
        private readonly ILogger<Deck> _logger;
        private readonly int _deckNumber;
        private AudioFileReader? _reader;
        private VolumeSampleProvider? _volumeProvider;
        private float _crossfaderGain = 1.0f;
        private bool _isPlaying;
        private bool _disposed;

        public float Volume { get; set; } = 1.0f;
        public string? LoadedFile { get; private set; }
        public double Length => _reader?.TotalTime.TotalSeconds ?? 0;
        public double Position => _reader?.CurrentTime.TotalSeconds ?? 0;
        public ISampleProvider? VolumeProvider => _volumeProvider;

        public Deck(int deckNumber, ILogger<Deck> logger)
        {
            _deckNumber = deckNumber;
            _logger = logger;
        }

        public void LoadFile(string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading file for deck {_deckNumber}: {filePath}");

                // Dispose existing
                _reader?.Dispose();
                _volumeProvider?.Dispose();

                // Create new
                _reader = new AudioFileReader(filePath);
                _volumeProvider = new VolumeSampleProvider(_reader.ToSampleProvider());
                _volumeProvider.Volume = Volume;

                LoadedFile = filePath;
                _logger.LogInformation($"File loaded successfully for deck {_deckNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading file for deck {_deckNumber}");
                throw;
            }
        }

        public void Eject()
        {
            _logger.LogInformation($"Ejecting file from deck {_deckNumber}");
            _reader?.Dispose();
            _volumeProvider?.Dispose();
            _reader = null;
            _volumeProvider = null;
            LoadedFile = null;
            _isPlaying = false;
        }

        public void Play()
        {
            if (_reader != null)
            {
                _isPlaying = true;
                _logger.LogInformation($"Playing deck {_deckNumber}");
            }
        }

        public void Pause()
        {
            _isPlaying = false;
            _logger.LogInformation($"Pausing deck {_deckNumber}");
        }

        public void Stop()
        {
            _isPlaying = false;
            if (_reader != null)
            {
                _reader.CurrentTime = TimeSpan.Zero;
            }
            _logger.LogInformation($"Stopping deck {_deckNumber}");
        }

        public void Seek(double seconds)
        {
            if (_reader != null)
            {
                _reader.CurrentTime = TimeSpan.FromSeconds(seconds);
                _logger.LogInformation($"Seeking deck {_deckNumber} to {seconds}s");
            }
        }

        public void UpdateVolume()
        {
            if (_volumeProvider != null)
            {
                _volumeProvider.Volume = Volume * _crossfaderGain;
            }
        }

        public void UpdateCrossfaderGain(float crossfader)
        {
            _crossfaderGain = _deckNumber == 1 ? 1 - crossfader : crossfader;
            UpdateVolume();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reader?.Dispose();
                    _volumeProvider?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}