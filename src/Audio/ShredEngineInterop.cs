using System;
using System.Runtime.InteropServices;

namespace DJMixMaster.Audio
{
    public static class ShredEngineInterop
    {
        private const string DllName = "ShredEngine.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitializeEngine(bool isTestMode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadFile(int deck, string filePath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Play(int deck);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Pause(int deck);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Stop(int deck);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Seek(int deck, double seconds);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetPosition(int deck);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetLength(int deck);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetVolume(int deck, float volume);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCrossfader(float value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMasterVolume(float volume);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCrossfaderCurve(int curveType);

        // Clipping Protection Methods
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetClippingProtectionEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetDeckVolumeCapEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetPeakDetectionEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetSoftKneeCompressorEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLookAheadLimiterEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetRmsMonitoringEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetAutoGainReductionEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetBrickwallLimiterEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetClippingIndicatorEnabled(bool enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetClippingThreshold(float threshold);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCompressorRatio(float ratio);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLimiterAttackTime(float attackMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLimiterReleaseTime(float releaseMs);

        // Monitoring getters
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetCurrentPeakLevel();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetCurrentRmsLevel();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsClipping();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ShutdownEngine();
    }
}