using System;
using System.Runtime.InteropServices;
using System.IO;

namespace DJMixMaster.Audio
{
    public static class ShredEngineInterop
    {
        // Platform-specific library name
        private static readonly string DllName = GetPlatformLibraryName();

        private static string GetPlatformLibraryName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "ShredEngine.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "libShredEngine.so";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "libShredEngine.dylib";
            }
            throw new PlatformNotSupportedException("Unsupported platform");
        }

        // Load library from custom path if needed
        private static IntPtr LoadLibrary()
        {
            string libraryPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On Linux, look for the library in bin directory relative to assembly location
                var assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;
                libraryPath = Path.Combine(assemblyLocation, DllName);

                if (!File.Exists(libraryPath))
                {
                    // Try looking in the project's bin directory
                    var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", ".."));
                    libraryPath = Path.Combine(projectRoot, "bin", DllName);
                }
            }
            else
            {
                // On Windows/macOS, let P/Invoke search system paths
                return IntPtr.Zero;
            }

            if (!File.Exists(libraryPath))
            {
                throw new DllNotFoundException($"Could not find {DllName} at {libraryPath}");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return dlopen(libraryPath, RTLD_NOW);
            }

            return IntPtr.Zero;
        }

        private const int RTLD_NOW = 2;

        [DllImport("libdl.so.2", CharSet = CharSet.Ansi)]
        private static extern IntPtr dlopen(string filename, int flags);

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

        static ShredEngineInterop()
        {
            // Pre-load library on Linux to ensure proper path resolution
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                LoadLibrary();
            }
        }
    }
}
