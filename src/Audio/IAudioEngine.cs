using System;

namespace DJMixMaster.Audio
{
    public interface IAudioEngine : IDisposable
    {
        event Action<object?, (int, double)>? PlaybackPositionChanged;
        event Action<object?, (int, double[], double)>? BeatGridUpdated;

        void LoadFile(int deckNumber, string filePath);
        void Play(int deckNumber);
        void Pause(int deckNumber);
        void Stop(int deckNumber);
        void Seek(int deckNumber, double seconds);
        double GetPosition(int deckNumber);
        double GetLength(int deckNumber);
        int GetSampleRate(int deckNumber);
        void SetVolume(int deckNumber, float volume);
        float GetVolume(int deckNumber);
        bool IsPlaying(int deckNumber);
        object GetDeckProperties(int deckNumber);
        void SetCrossfader(float value);
        float GetCrossfader();
        (float[] WaveformData, double TrackLength) GetWaveformData(int deckNumber);
        void AddCuePoint(int deckNumber);
        void JumpToCuePoint(int deckNumber, int cueIndex);
        void PlayTestTone(int deckNumber, double frequency = 440, double duration = 1);
        void UpdateAudioSettings(AudioSettings settings);
        AudioSettings GetCurrentSettings();
        void ShowAsioControlPanel();
        object EnumerateDevices();
        void UpdateOutputDevice(string device);
        string GetSoundOutState();
    }
}