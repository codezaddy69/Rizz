using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class EngineMixer
    {
        private readonly ILogger<EngineMixer> _logger;
        private readonly List<EngineChannel> _channels = new();
        private readonly float[] _mainBuffer;
        private readonly float[] _boothBuffer;
        private readonly float[] _headphoneBuffer;
        private readonly int _bufferSize;

        public EngineMixer(ILogger<EngineMixer> logger, int sampleRate, int bufferSize)
        {
            _logger = logger;
            _bufferSize = bufferSize;
            _mainBuffer = new float[bufferSize * 2]; // Stereo
            _boothBuffer = new float[bufferSize * 2];
            _headphoneBuffer = new float[bufferSize * 2];
        }

        public void AddChannel(EngineChannel channel)
        {
            _channels.Add(channel);
            _logger.LogInformation("Added channel {Group}", channel.Group);
        }

        public void Process(int frames)
        {
            // Clear buffers
            Array.Clear(_mainBuffer, 0, _mainBuffer.Length);
            Array.Clear(_boothBuffer, 0, _boothBuffer.Length);
            Array.Clear(_headphoneBuffer, 0, _headphoneBuffer.Length);

            // Process each channel
            foreach (var channel in _channels)
            {
                channel.Process(frames);

                // Mix to main output
                var channelBuffer = channel.GetBuffer();
                for (int i = 0; i < frames * 2; i++)
                {
                    _mainBuffer[i] += channelBuffer[i] * channel.Volume;
                }
            }

            _logger.LogDebug("Processed {Frames} frames", frames);
        }

        public float[] GetMainBuffer() => _mainBuffer;
        public float[] GetBoothBuffer() => _boothBuffer;
        public float[] GetHeadphoneBuffer() => _headphoneBuffer;
    }

    public class EngineChannel
    {
        private readonly ILogger _logger;
        private readonly float[] _buffer;
        private EngineBuffer? _engineBuffer;

        public string Group { get; }
        public float Volume { get; set; } = 1.0f;

        public EngineChannel(string group, ILogger logger, int bufferSize)
        {
            Group = group;
            _logger = logger;
            _buffer = new float[bufferSize * 2];
        }

        public void SetEngineBuffer(EngineBuffer buffer)
        {
            _engineBuffer = buffer;
        }

        public void Process(int frames)
        {
            if (_engineBuffer != null)
            {
                _engineBuffer.Process(frames, _buffer);
            }
        }

        public float[] GetBuffer() => _buffer;
    }
}