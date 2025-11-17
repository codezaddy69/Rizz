using System;

namespace DJMixMaster.Audio
{
    public class AudioFileProperties
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        public long TotalSamples { get; set; }
        public double Duration { get; set; }
        public int Bitrate { get; set; }
        public long FileSize { get; set; }

        public string FormatDescription =>
            $"{SampleRate}Hz, {Channels}ch, {BitsPerSample}bit";

        public string DetailedInfo =>
            $"{FormatDescription}, {Duration:F1}s, {Bitrate / 1000}kbps, {FileSize / 1024}KB";
    }
}