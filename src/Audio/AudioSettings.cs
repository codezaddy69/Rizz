using System.Collections.Generic;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Comprehensive audio settings for DJMixMaster
    /// </summary>
    public class AudioSettings
    {
        // Device Selection
        public string SelectedAsioDevice { get; set; } = "ASIO4ALL v2";
        public List<AsioDeviceInfo> AvailableDevices { get; set; } = new();

        // Buffer & Latency
        public int BufferSize { get; set; } = 512;
        public int BufferOffset { get; set; } = 10;
        public int KernelBuffers { get; set; } = 2;
        public bool UseHardwareBuffer { get; set; } = false;
        public bool AllowPullMode { get; set; } = true;

        // Audio Processing
        public bool AlwaysResample { get; set; } = true;
        public bool Force16Bit { get; set; } = false;
        public int SampleRate { get; set; } = 44100;
        public string PreferredFormat { get; set; } = "Float32";

        // Crossfader
        public string CrossfaderCurve { get; set; } = "Linear";
        public float CrossfaderRange { get; set; } = 1.0f;
        public float CrossfaderSensitivity { get; set; } = 1.0f;
        public string CrossfaderMode { get; set; } = "Additive";
        public bool CenterDetent { get; set; } = false;

        // Output Routing
        public string MasterOutputMode { get; set; } = "StereoMix";
        public string CueOutputSource { get; set; } = "Deck1";
        public string BoothOutputSource { get; set; } = "Disabled";
        public float MasterOutputLevel { get; set; } = 0.0f;
        public float HeadphoneMix { get; set; } = 0.5f;

        // Performance
        public string ProcessPriority { get; set; } = "High";
        public bool CpuAffinity { get; set; } = false;
        public int MaxThreads { get; set; } = 4;

        // Diagnostics
        public string LogLevel { get; set; } = "Info";
        public bool EnablePerfMonitoring { get; set; } = false;
        public bool ShowBufferStatus { get; set; } = false;
        public string LogFilePath { get; set; } = "logs/audio.log";

        // File & Playback
        public bool AutoLoadCuePoints { get; set; } = true;
        public bool RememberLastPosition { get; set; } = false;
        public bool GaplessPlayback { get; set; } = true;
        public float PreBufferTime { get; set; } = 2.0f;
        public int MaxFileSize { get; set; } = 500;

        // UI
        public string Theme { get; set; } = "DarkNeon";
        public float WaveformZoom { get; set; } = 1.0f;
        public bool AutoScrollWaveform { get; set; } = true;
        public string KeyboardLayout { get; set; } = "Standard";
        public bool ConfirmOnDelete { get; set; } = false;
    }

    /// <summary>
    /// Information about an ASIO device
    /// </summary>
    public class AsioDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public int MaxChannels { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "Available"; // Available, In Use, Unavailable

        public override string ToString()
        {
            return $"{Name} ({Status})";
        }
    }
}