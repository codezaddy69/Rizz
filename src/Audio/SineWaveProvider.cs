using System;
using System.IO;
using NAudio.Wave;

namespace DJMixMaster.Audio
{
    public class SineWaveProvider : ISampleProvider
    {
        private readonly double _frequency;
        private readonly double _duration;
        private readonly float _volume;
        private long _position;
        private readonly long _totalSamples;

        public SineWaveProvider(double frequency, double duration, float volume)
        {
            _frequency = frequency;
            _duration = duration;
            _volume = volume;
            _totalSamples = (long)(duration * 44100); // Assume 44.1kHz
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;
            for (int i = 0; i < count / 2 && _position < _totalSamples; i++)
            {
                double t = _position / 44100.0;
                float sample = (float)(Math.Sin(2 * Math.PI * _frequency * t) * _volume);
                buffer[offset + i * 2] = sample;     // Left
                buffer[offset + i * 2 + 1] = sample; // Right
                _position++;
                samplesRead += 2;
            }
            if (_position >= _totalSamples && samplesRead == 0)
            {
                Console.WriteLine("Test tone finished");
                File.AppendAllText("debug.log", $"{DateTime.Now}: Test tone finished\n");
            }
            if (samplesRead > 0 && _position % 4410 == 0) // Log every 0.1 seconds
            {
                Console.WriteLine($"Sine wave: pos {_position}, samples {samplesRead}");
                File.AppendAllText("debug.log", $"{DateTime.Now}: Sine read {samplesRead} samples\n");
            }
            return samplesRead;
        }
    }
}