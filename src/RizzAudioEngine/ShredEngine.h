#pragma once

#define SHRED_API __declspec(dllexport)

#include <string>

extern "C" {
    SHRED_API void InitializeEngine(bool isTestMode = false);
    SHRED_API int LoadFile(int deck, const char* filePath);
    SHRED_API void Play(int deck);
    SHRED_API void Pause(int deck);
    SHRED_API void Stop(int deck);
    SHRED_API void Seek(int deck, double seconds);
    SHRED_API double GetPosition(int deck);
    SHRED_API double GetLength(int deck);
    SHRED_API void SetVolume(int deck, float volume);
    SHRED_API void SetCrossfader(float value);
    SHRED_API void ShutdownEngine();
}