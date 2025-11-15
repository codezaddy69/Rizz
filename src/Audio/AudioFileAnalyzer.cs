using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Analyzes audio files to extract comprehensive information for optimization and diagnostics.
    /// </summary>
    public class AudioFileAnalyzer
    {
        private readonly ILogger _logger;

        public AudioFileAnalyzer(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes an audio file and returns comprehensive information.
        /// </summary>
        /// <param name="filePath">Path to the audio file.</param>
        /// <returns>AudioFileInfo with all extracted information.</returns>
        public AudioFileInfo AnalyzeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Audio file not found.", filePath);

            var info = new AudioFileInfo { FilePath = filePath };

            try
            {
                // Basic file information
                ExtractBasicFileInfo(filePath, info);

                // Audio format analysis
                ExtractAudioFormatInfo(filePath, info);

                // Advanced analysis
                AnalyzeCodecAndCompression(info);
                CalculatePerformanceMetrics(info);
                CheckCompatibility(info);

                _logger.LogInformation("Successfully analyzed audio file: {Info}", info);
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze audio file: {FilePath}", filePath);
                info.IsCorrupted = true;
                info.CompatibilityWarnings = new[] { $"Analysis failed: {ex.Message}" };
                return info;
            }
        }

        private void ExtractBasicFileInfo(string filePath, AudioFileInfo info)
        {
            var fileInfo = new FileInfo(filePath);
            info.FileSize = fileInfo.Length;
            info.FileName = fileInfo.Name;
        }

        private void ExtractAudioFormatInfo(string filePath, AudioFileInfo info)
        {
            using var reader = new AudioFileReader(filePath);
            info.SampleRate = reader.WaveFormat.SampleRate;
            info.Channels = reader.WaveFormat.Channels;
            info.BitsPerSample = reader.WaveFormat.BitsPerSample;
            info.Duration = reader.TotalTime;
        }

        private void AnalyzeCodecAndCompression(AudioFileInfo info)
        {
            string extension = Path.GetExtension(info.FilePath).ToLowerInvariant();

            switch (extension)
            {
                case ".mp3":
                    info.Codec = "MP3";
                    info.IsCompressed = true;
                    info.IsLossy = true;
                    break;
                case ".wav":
                    info.Codec = "WAV";
                    info.IsCompressed = false;
                    info.IsLossy = false;
                    break;
                case ".flac":
                    info.Codec = "FLAC";
                    info.IsCompressed = true;
                    info.IsLossy = false;
                    break;
                case ".aiff":
                case ".aif":
                    info.Codec = "AIFF";
                    info.IsCompressed = false;
                    info.IsLossy = false;
                    break;
                case ".ogg":
                    info.Codec = "OGG";
                    info.IsCompressed = true;
                    info.IsLossy = true;
                    break;
                default:
                    info.Codec = extension.ToUpperInvariant().TrimStart('.');
                    info.IsCompressed = true; // Assume compressed for unknown formats
                    info.IsLossy = true;
                    break;
            }

            // Estimate bitrate for compressed formats
            if (info.IsCompressed && info.Duration.TotalSeconds > 0)
            {
                info.Bitrate = (int)((info.FileSize * 8) / info.Duration.TotalSeconds);
            }
        }

        private void CalculatePerformanceMetrics(AudioFileInfo info)
        {
            // Memory usage estimation (uncompressed size)
            long bytesPerSecond = info.SampleRate * info.Channels * (info.BitsPerSample / 8);
            info.EstimatedMemoryUsage = (long)(bytesPerSecond * info.Duration.TotalSeconds);

            // Processing complexity score (0-1)
            info.ProcessingComplexity = 0.0;

            // Base complexity from compression
            if (info.IsCompressed)
                info.ProcessingComplexity += 0.4;

            // Sample rate factor
            if (info.SampleRate > 96000)
                info.ProcessingComplexity += 0.3;
            else if (info.SampleRate > 48000)
                info.ProcessingComplexity += 0.2;
            else if (info.SampleRate < 22050)
                info.ProcessingComplexity += 0.1;

            // Channel factor
            if (info.Channels > 2)
                info.ProcessingComplexity += 0.2;
            else if (info.Channels == 1)
                info.ProcessingComplexity += 0.1;

            // Bit depth factor
            if (info.BitsPerSample > 16)
                info.ProcessingComplexity += 0.1;

            // Clamp to 0-1 range
            info.ProcessingComplexity = Math.Max(0.0, Math.Min(1.0, info.ProcessingComplexity));

            // Recommended buffer size based on complexity
            if (info.ProcessingComplexity > 0.7)
                info.RecommendedBufferSize = 1024;
            else if (info.ProcessingComplexity > 0.4)
                info.RecommendedBufferSize = 512;
            else
                info.RecommendedBufferSize = 256;
        }

        private void CheckCompatibility(AudioFileInfo info)
        {
            var warnings = new List<string>();

            // Sample rate validation
            if (info.SampleRate < 8000)
                warnings.Add($"Very low sample rate ({info.SampleRate}Hz) may cause issues");
            else if (info.SampleRate > 192000)
                warnings.Add($"Very high sample rate ({info.SampleRate}Hz) may impact performance");

            // Channel validation
            if (info.Channels > 2)
                warnings.Add($"{info.Channels}-channel audio will be downmixed to stereo");
            else if (info.Channels < 1)
                warnings.Add("Invalid channel count detected");

            // File size check
            if (info.FileSize > 500 * 1024 * 1024) // 500MB
                warnings.Add("Very large file may cause memory issues");

            // Duration check
            if (info.Duration.TotalSeconds < 1)
                warnings.Add("Very short audio file detected");
            else if (info.Duration.TotalHours > 2)
                warnings.Add("Very long audio file may impact responsiveness");

            // Bit depth check
            if (info.BitsPerSample < 8 || info.BitsPerSample > 32)
                warnings.Add($"Unusual bit depth ({info.BitsPerSample}) detected");

            info.CompatibilityWarnings = warnings.ToArray();
            info.NeedsResampling = info.SampleRate != 44100;
        }
    }
}