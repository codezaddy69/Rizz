#include "DJEngine.h"
#include "ScratchBuffer.h"
#include "ClubMixer.h"
#include "Selekta.h"
#include <iostream>
#include <memory>
#include <chrono>
#include <ctime>

#define SHREDENGINE_EXPORTS
#include "ShredEngine.h"

static std::unique_ptr<Selekta> g_ampManager;
static std::unique_ptr<ClubMixer> g_mixer;
static std::unique_ptr<ScratchBuffer> g_deck1;
static std::unique_ptr<ScratchBuffer> g_deck2;

// Improvement 1: Timestamped logging
void logWithTimestamp(const std::string& message) {
    auto now = std::chrono::system_clock::now();
    std::time_t now_time = std::chrono::system_clock::to_time_t(now);
    std::cout << "[" << std::ctime(&now_time) << "] " << message << std::endl;
}

// Audio callback
static int audioCallback(const void* inputBuffer, void* outputBuffer, unsigned long framesPerBuffer,
                        const PaStreamCallbackTimeInfo* timeInfo, PaStreamCallbackFlags statusFlags, void* userData) {
    float* out = (float*)outputBuffer;
    // For now, generate test tone
    static double phase = 0.0;
    for (unsigned long i = 0; i < framesPerBuffer; ++i) {
        float sample = (float)(sin(phase) * 0.1);
        out[i * 2] = sample;
        out[i * 2 + 1] = sample;
        phase += 0.1;
    }
    return paContinue;
}

extern "C" {

SHRED_API void InitializeEngine() {
    try {
        g_ampManager = std::make_unique<Selekta>();
        g_mixer = std::make_unique<ClubMixer>();
        g_deck1 = std::make_unique<ScratchBuffer>();
        g_deck2 = std::make_unique<ScratchBuffer>();
        logWithTimestamp("ShredEngine initialized successfully");
    } catch (const std::exception& e) {
        logWithTimestamp("ShredEngine initialization failed: " + std::string(e.what()));
    }
}

SHRED_API int LoadFile(int deck, const char* filePath) {
    std::cout << "Loading file " << filePath << " for deck " << deck << std::endl;
    // TODO: Implement file loading with BeatSource
    return 0; // Success
}

SHRED_API void Play(int deck) {
    // Improvement 2: Null pointer checks
    if (deck == 1) {
        if (g_deck1) g_deck1->play();
        else logWithTimestamp("Deck 1 not initialized");
    } else if (deck == 2) {
        if (g_deck2) g_deck2->play();
        else logWithTimestamp("Deck 2 not initialized");
    } else {
        logWithTimestamp("Invalid deck number: " + std::to_string(deck));
    }
    logWithTimestamp("Playing deck " + std::to_string(deck));
}

DJ_API void Pause(int deck) {
    if (deck == 1 && g_deck1) g_deck1->pause();
    else if (deck == 2 && g_deck2) g_deck2->pause();
    std::cout << "Pausing deck " << deck << std::endl;
}

DJ_API void Stop(int deck) {
    if (deck == 1 && g_deck1) g_deck1->pause();
    else if (deck == 2 && g_deck2) g_deck2->pause();
    std::cout << "Stopping deck " << deck << std::endl;
}

DJ_API void Seek(int deck, double seconds) {
    long frame = (long)(seconds * 44100);
    if (deck == 1 && g_deck1) g_deck1->seek(frame);
    else if (deck == 2 && g_deck2) g_deck2->seek(frame);
    std::cout << "Seeking deck " << deck << " to " << seconds << "s" << std::endl;
}

DJ_API double GetPosition(int deck) {
    if (deck == 1 && g_deck1) return g_deck1->getPosition();
    else if (deck == 2 && g_deck2) return g_deck2->getPosition();
    return 0.0;
}

DJ_API double GetLength(int deck) {
    if (deck == 1 && g_deck1) return g_deck1->getLength();
    else if (deck == 2 && g_deck2) return g_deck2->getLength();
    return 0.0;
}

DJ_API void SetVolume(int deck, float volume) {
    if (g_mixer) g_mixer->setVolume(deck, volume);
    std::cout << "Setting volume for deck " << deck << " to " << volume << std::endl;
}

DJ_API void SetCrossfader(float value) {
    if (g_mixer) g_mixer->setCrossfader(value);
    std::cout << "Setting crossfader to " << value << std::endl;
}

SHRED_API void ShutdownEngine() {
    g_deck2.reset();
    g_deck1.reset();
    g_mixer.reset();
    g_ampManager.reset();
    std::cout << "ShredEngine shutdown" << std::endl;
}

}