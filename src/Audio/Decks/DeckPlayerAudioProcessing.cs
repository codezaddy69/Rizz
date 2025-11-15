using System;
using System.IO;
using NAudio.Wave;
using NAudio.MediaFoundation;
using Microsoft.Extensions.Logging;
using DJMixMaster.Audio.Providers;

namespace DJMixMaster.Audio
{
    public class DeckPlayerAudioProcessing
    {
        private readonly ILogger logger;
        private readonly WaveFormat waveFormat;
        private DeckVolumeProvider? volumeProvider;
        private WaveStream? audioFile;
        private float[] waveformData = Array.Empty<float>();
        private double trackLength;

        public WaveStream? AudioFile => audioFile;
        public DeckVolumeProvider? VolumeProvider => volumeProvider;
        public double TrackLength => trackLength;
        public float[] WaveformData => waveformData;

        public DeckPlayerAudioProcessing(ILogger logger)
        {
            this.logger = logger;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        }

        public void LoadAudioFile(string filePath)
        {
            try
            {
                CleanupAudio();
                logger.LogInformation($"Loading audio file: {filePath}");
                
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                audioFile = extension switch
                {
                    ".mp3" => new MediaFoundationReader(filePath),
                    ".wav" => new WaveFileReader(filePath),
                    ".aiff" => new AiffFileReader(filePath),
                    _ => throw new NotSupportedException($"File format {extension} is not supported")
                };

                trackLength = audioFile.TotalTime.TotalSeconds;
                logger.LogInformation($"Track length: {trackLength}s");

                ConvertAudioFormat();
                GenerateWaveformData();

                logger.LogInformation("Audio file loaded successfully");
                logger.LogDebug($"Sample rate: {audioFile?.WaveFormat.SampleRate}");
                logger.LogDebug($"Channels: {audioFile?.WaveFormat.Channels}");
                logger.LogDebug($"Encoding: {audioFile?.WaveFormat.Encoding}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load audio file: {ex.Message}", ex);
                throw new Exception($"Failed to load audio file: {ex.Message}", ex);
            }
        }

        private void ConvertAudioFormat()
        {
            if (audioFile == null) return;

            if (audioFile.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat ||
                audioFile.WaveFormat.SampleRate != 44100)
            {
                var resampler = new MediaFoundationResampler(audioFile, waveFormat);
                var buffer = new byte[audioFile.Length];
                resampler.Read(buffer, 0, buffer.Length);
                audioFile = new RawSourceWaveStream(new MemoryStream(buffer), waveFormat);
            }
        }

        private void GenerateWaveformData(int sampleCount = 1000)
        {
            if (audioFile == null) return;

            try
            {
                var samples = new float[audioFile.Length / 4];
                audioFile.Position = 0;
                var waveProvider = audioFile.ToSampleProvider();
                waveProvider.Read(samples, 0, samples.Length);

                waveformData = new float[sampleCount];
                var samplesPerPoint = samples.Length / sampleCount;

                for (int i = 0; i < sampleCount; i++)
                {
                    var start = i * samplesPerPoint;
                    var end = Math.Min(start + samplesPerPoint, samples.Length);
                    float max = 0;

                    for (int j = start; j < end; j++)
                    {
                        var abs = Math.Abs(samples[j]);
                        if (abs > max) max = abs;
                    }

                    waveformData[i] = max;
                }

                audioFile.Position = 0;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to generate waveform data: {ex.Message}", ex);
                waveformData = Array.Empty<float>();
            }
        }

        public void CleanupAudio()
        {
            try
            {
                volumeProvider = null;
                if (audioFile != null)
                {
                    audioFile.Dispose();
                    audioFile = null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error during cleanup: {ex.Message}", ex);
            }
        }
    }
}
