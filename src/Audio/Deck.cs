using System;
using CSCore;
using CSCore.Codecs;
using CSCore.Streams;

using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    internal class VolumeSampleProvider : ISampleSource
    {
        private ISampleSource _source;
        public float Volume { get; set; } = 1.0f;
        public WaveFormat WaveFormat => _source.WaveFormat;
        public bool CanSeek => false;
        public long Position { get => 0; set {} }
        public long Length => 0;

        public VolumeSampleProvider(ISampleSource source)
        {
            _source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            for (int i = 0; i < read; i++)
            {
                buffer[offset + i] *= Volume;
            }
            return read;
        }

        public void Dispose() => _source.Dispose();
    }

    public class Deck : IDisposable
    {
        private readonly ILogger<Deck> _logger;
        private readonly int _deckNumber;
        private IWaveSource? _waveSource;
        private VolumeSampleProvider? _volumeProvider;
        private bool _isPlaying;
        private bool _disposed;

        public float Volume { get; set; } = 1.0f;
        public string? LoadedFile { get; private set; }
        public double Length => _waveSource?.GetLength().TotalSeconds ?? 0;
        public double Position => _waveSource?.GetPosition().TotalSeconds ?? 0;
        public bool IsPlaying => _isPlaying;

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

                // Dispose existing source
                _waveSource?.Dispose();
                _volumeProvider?.Dispose();

                // Create new source
                _waveSource = CodecFactory.Instance.GetCodec(filePath);
                ISampleSource sampleSource = _waveSource.ToSampleSource();
                _volumeProvider = new VolumeSampleProvider(sampleSource);
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
            _waveSource?.Dispose();
            _volumeProvider?.Dispose();
            _waveSource = null;
            _volumeProvider = null;
            LoadedFile = null;
            _isPlaying = false;
        }

        public void Play()
        {
            if (_waveSource != null)
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
            Seek(0);
            _logger.LogInformation($"Stopping deck {_deckNumber}");
        }

        public void Seek(double seconds)
        {
            if (_waveSource != null)
            {
                _waveSource.SetPosition(TimeSpan.FromSeconds(seconds));
                _logger.LogInformation($"Seeking deck {_deckNumber} to {seconds}s");
            }
        }

        public ISampleSource? GetSampleSource()
        {
            return _volumeProvider;
        }

        public void UpdateVolume()
        {
            if (_volumeProvider != null)
            {
                _volumeProvider.Volume = Volume;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _waveSource?.Dispose();
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