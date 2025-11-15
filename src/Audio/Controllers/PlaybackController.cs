using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace DJMixMaster.Audio.Controllers
{
    public class PlaybackController
    {
        private readonly ILogger<PlaybackController> _logger;
        private WasapiOut? _waveOut1;
        private WasapiOut? _waveOut2;
        private MediaFoundationReader? _reader1;
        private MediaFoundationReader? _reader2;

        public event EventHandler<(int DeckNumber, double Position)>? PlaybackPositionChanged;

        public PlaybackController(ILogger<PlaybackController> logger)
        {
            _logger = logger;
        }

        public ISampleProvider LoadTrackForDeck(int deckNumber, string filePath)
        {
            // Dispose of existing resources first
            if (deckNumber == 1)
            {
                _waveOut1?.Stop();
                _waveOut1?.Dispose();
                _reader1?.Dispose();
            }
            else
            {
                _waveOut2?.Stop();
                _waveOut2?.Dispose();
                _reader2?.Dispose();
            }

            MediaFoundationReader reader;

            // Try MediaFoundationReader for better format support
            reader = new MediaFoundationReader(filePath);

            _logger.LogDebug($"Sample rate: {reader.WaveFormat.SampleRate}");

            double len = reader.TotalTime.TotalSeconds;
            if (double.IsNaN(len) || len <= 0)
            {
                reader.Dispose();
                if (deckNumber == 1) _reader1 = null;
                else _reader2 = null;
                throw new Exception("Unsupported audio file format or codec not available");
            }

            // Create sample provider
            var sampleProvider = reader.ToSampleProvider();

            // Store the reader
            if (deckNumber == 1)
            {
                _reader1 = reader;
            }
            else
            {
                _reader2 = reader;
            }

            return sampleProvider;
        }

        public void SetWaveOut(int deckNumber, WasapiOut waveOut)
        {
            if (deckNumber == 1)
            {
                _waveOut1 = waveOut;
            }
            else
            {
                _waveOut2 = waveOut;
            }
        }

        public void Play(int deckNumber)
        {
            try
            {
                _logger.LogInformation($"Playing deck {deckNumber}");
                var waveOut = GetWaveOut(deckNumber);
                if (waveOut != null)
                {
                    _logger.LogDebug($"Starting playback for deck {deckNumber} with WaveOut");
                    waveOut.Play();
                    StartPositionTracking(deckNumber);
                    _logger.LogDebug($"Playback started successfully for deck {deckNumber}");
                }
                else
                {
                    _logger.LogWarning($"WaveOut is null for deck {deckNumber}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error playing deck {deckNumber}");
                throw;
            }
        }

        public void Pause(int deckNumber)
        {
            try
            {
                _logger.LogInformation($"Pausing deck {deckNumber}");
                var waveOut = GetWaveOut(deckNumber);
                if (waveOut != null)
                {
                    waveOut.Pause();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pausing deck {deckNumber}");
                throw;
            }
        }

        public void Stop(int deckNumber)
        {
            try
            {
                _logger.LogInformation($"Stopping deck {deckNumber}");
                var waveOut = GetWaveOut(deckNumber);
                var reader = GetReader(deckNumber);
                if (waveOut != null)
                {
                    waveOut.Stop();
                }
                if (reader != null)
                {
                    reader.Position = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping deck {deckNumber}");
                throw;
            }
        }

        public void Seek(int deckNumber, double seconds)
        {
            try
            {
                var reader = GetReader(deckNumber);
                if (reader != null)
                {
                    reader.CurrentTime = TimeSpan.FromSeconds(seconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error seeking deck {deckNumber}");
                throw;
            }
        }

        public double GetPosition(int deckNumber)
        {
            var reader = GetReader(deckNumber);
            return reader?.CurrentTime.TotalSeconds ?? 0;
        }

        public double GetLength(int deckNumber)
        {
            var reader = GetReader(deckNumber);
            double length = reader?.TotalTime.TotalSeconds ?? 0;
            _logger.LogDebug($"GetLength for deck {deckNumber}: {length}s");
            return length;
        }

        public bool IsPlaying(int deckNumber)
        {
            var waveOut = GetWaveOut(deckNumber);
            return waveOut?.PlaybackState == PlaybackState.Playing;
        }


        private MediaFoundationReader? GetReader(int deckNumber) => deckNumber == 1 ? _reader1 : _reader2;

        private void StartPositionTracking(int deckNumber)
        {
            // Start a timer to update position
            var stopwatch = Stopwatch.StartNew();
            var timer = new System.Threading.Timer(_ =>
            {
                if (IsPlaying(deckNumber))
                {
                    var position = GetPosition(deckNumber);
                    OnPlaybackPositionChanged(deckNumber, position);

                    // Log performance every 10 seconds
                    if (stopwatch.Elapsed.TotalSeconds > 10)
                    {
                        _logger.LogDebug($"Deck {deckNumber} position tracking active, current position: {position:F2}s");
                        stopwatch.Restart();
                    }
                }
            }, null, 0, 500); // Changed to 500ms for efficiency
        }

        protected virtual void OnPlaybackPositionChanged(int deckNumber, double position)
        {
            PlaybackPositionChanged?.Invoke(this, (deckNumber, position));
        }

        public WasapiOut? GetWaveOut(int deckNumber) => deckNumber == 1 ? _waveOut1 : _waveOut2;

        public void Dispose()
        {
            _waveOut1?.Dispose();
            _waveOut2?.Dispose();
            _reader1?.Dispose();
            _reader2?.Dispose();
        }
    }
}