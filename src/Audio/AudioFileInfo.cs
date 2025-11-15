using System;
using System.IO;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Comprehensive information about an audio file for optimization and diagnostics.
    /// </summary>
    public class AudioFileInfo
    {
        // Basic File Properties
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }

        // Audio Format Properties
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        public TimeSpan Duration { get; set; }

        // Advanced Audio Properties
        public string Codec { get; set; } = string.Empty;
        public int? Bitrate { get; set; } // Null for uncompressed formats
        public bool IsCompressed { get; set; }
        public bool IsLossy { get; set; }

        // Performance Metrics
        public long EstimatedMemoryUsage { get; set; }
        public double ProcessingComplexity { get; set; } // 0-1 scale
        public int RecommendedBufferSize { get; set; }

        // Diagnostic Information
        public bool IsCorrupted { get; set; }
        public bool NeedsResampling { get; set; }
        public string[] CompatibilityWarnings { get; set; } = Array.Empty<string>();

        // Computed Properties
        public string FormatDescription => $"{Codec} {SampleRate}Hz {Channels}ch {BitsPerSample}bit";
        public string DurationString => Duration.ToString(@"mm\:ss");
        public string FileSizeString => FileSize < 1024 * 1024
            ? $"{FileSize / 1024} KB"
            : $"{FileSize / (1024 * 1024)} MB";

        public override string ToString()
        {
            return $"{FileName} - {FormatDescription} - {DurationString} - {FileSizeString}";
        }
    }
}