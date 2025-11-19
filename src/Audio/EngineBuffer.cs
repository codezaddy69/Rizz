using System;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class EngineBuffer
    {
        private readonly ILogger<EngineBuffer> _logger;
        private readonly CachingReader _reader;
        private readonly float[] _scratchBuffer;
        private bool _isPlaying;
        private long _currentFrame;

        public EngineBuffer(CachingReader reader, ILogger<EngineBuffer> logger, int bufferSize)
        {
            _reader = reader;
            _logger = logger;
            _scratchBuffer = new float[bufferSize * 2];
        }

        public double Length => _reader.Length;
        public int SampleRate => _reader.SampleRate;
        public double Position => _currentFrame / (double)SampleRate;

        public void Process(int frames, float[] outputBuffer)
        {
            if (!_isPlaying)
            {
                Array.Clear(outputBuffer, 0, outputBuffer.Length);
                return;
            }

            // Read from CachingReader
            int samplesRead = _reader.Read(_currentFrame, frames * 2, outputBuffer);
            _currentFrame += samplesRead / 2;

            if (samplesRead < frames * 2)
            {
                // Handle end of track
                _isPlaying = false;
                _logger.LogInformation("Track ended");
            }
        }

        public void Play()
        {
            _isPlaying = true;
            _logger.LogInformation("Playback started");
        }

        public void Pause()
        {
            _isPlaying = false;
            _logger.LogInformation("Playback paused");
        }

        public void Seek(long frame)
        {
            _currentFrame = frame;
            _logger.LogInformation("Seeked to frame {Frame}", frame);
        }
    }
}