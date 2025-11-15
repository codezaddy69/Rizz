using System;
using System.Runtime.InteropServices;
using System.IO;

namespace DJMixMaster.Native.JUCE
{
    /// <summary>
    /// Provides P/Invoke wrappers for the JUCE native DLL functions.
    /// This class handles the communication between our C# code and the native JUCE library.
    /// </summary>
    public static class JuceNative
    {
        private const string DllName = "juce_dll.dll";
        private static readonly string DllPath;

        static JuceNative()
        {
            // Calculate the path to the native DLL based on the platform
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string libraryPath = Path.Combine(basePath, "lib", "native", Environment.Is64BitProcess ? "x64" : "x86");
            DllPath = Path.Combine(libraryPath, DllName);

            if (!File.Exists(DllPath))
            {
                throw new DllNotFoundException($"Failed to find JUCE native library at: {DllPath}");
            }

            // Load the DLL
            LoadLibrary(DllPath);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        #region Audio Device Management

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JUCE_CreateAudioDeviceManager();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JUCE_DeleteAudioDeviceManager(IntPtr manager);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool JUCE_InitializeAudioDevice(IntPtr manager, [MarshalAs(UnmanagedType.LPWStr)] string outputDeviceName, int numInputChannels, int numOutputChannels, double sampleRate, int bufferSize);

        #endregion

        #region Audio Format Management

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JUCE_CreateAudioFormatManager();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JUCE_DeleteAudioFormatManager(IntPtr manager);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JUCE_CreateAudioFormatReader(IntPtr formatManager, [MarshalAs(UnmanagedType.LPWStr)] string filePath);

        #endregion

        #region Audio Processing

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JUCE_CreateAudioProcessorGraph();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JUCE_DeleteAudioProcessorGraph(IntPtr graph);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JUCE_CreateAudioGraphNode(IntPtr graph);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool JUCE_ConnectAudioNodes(IntPtr graph, IntPtr sourceNode, int sourceChannel, IntPtr destNode, int destChannel);

        #endregion

        #region VST Plugin Support

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JUCE_LoadVSTPlugin(IntPtr graph, [MarshalAs(UnmanagedType.LPWStr)] string pluginPath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool JUCE_ConnectVSTPlugin(IntPtr graph, IntPtr pluginNode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JUCE_SetVSTParameter(IntPtr plugin, int parameterIndex, float value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float JUCE_GetVSTParameter(IntPtr plugin, int parameterIndex);

        #endregion

        #region Playback Control

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JUCE_StartPlayback(IntPtr deviceManager);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JUCE_StopPlayback(IntPtr deviceManager);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JUCE_SetPlaybackPosition(IntPtr deviceManager, double positionInSeconds);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double JUCE_GetPlaybackPosition(IntPtr deviceManager);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetPosition();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetLength();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsPlaying();

        #endregion
    }
}
