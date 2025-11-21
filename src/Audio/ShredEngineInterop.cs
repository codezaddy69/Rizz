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
        public static extern void ShutdownEngine();
    }
}