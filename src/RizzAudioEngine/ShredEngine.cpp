#include "ScratchBuffer.h"
#include "ClubMixer.h"
#include "Selekta.h"
#include <iostream>
#include <memory>
#include <chrono>
#include <ctime>
#include <portaudio.h>
#include <fstream>
#include <cmath>

#define SHREDENGINE_EXPORTS
#include "ShredEngine.h"

static std::unique_ptr<Selekta> g_ampManager;
static std::unique_ptr<ClubMixer> g_mixer;
static std::unique_ptr<ScratchBuffer> g_deck1;
static std::unique_ptr<ScratchBuffer> g_deck2;
static PaStream* g_stream = nullptr;
static bool g_isTestMode = false;

// Audio callback
static int audioCallback(const void* inputBuffer, void* outputBuffer, unsigned long framesPerBuffer,
                        const PaStreamCallbackTimeInfo* timeInfo, PaStreamCallbackFlags statusFlags, void* userData) {
    static unsigned long phase = 0;
    static bool first = true;
    if (first) {
        std::cout << "[Callback] Started, framesPerBuffer=" << framesPerBuffer << ", testMode=" << g_isTestMode << std::endl;
        first = false;
    }
    float* out = (float*)outputBuffer;

    if (g_isTestMode) {
        // Generate sine for test
        for (unsigned long i = 0; i < framesPerBuffer; ++i) {
            float sample = sin(phase * 0.1) * 0.5f;
            out[i * 2] = sample;
            out[i * 2 + 1] = sample;
            phase++;
        }
    } else {
        // Get audio from decks
        std::vector<float> left1(framesPerBuffer), right1(framesPerBuffer);
        std::vector<float> left2(framesPerBuffer), right2(framesPerBuffer);

        if (g_deck1) g_deck1->getAudio(left1.data(), right1.data(), framesPerBuffer);
        if (g_deck2) g_deck2->getAudio(left2.data(), right2.data(), framesPerBuffer);

        // Mix
        if (g_mixer) {
            for (unsigned long i = 0; i < framesPerBuffer; ++i) {
                float l = left1[i] + left2[i];
                float r = right1[i] + right2[i];
                g_mixer->mix(&l, &r, 1);
                out[i * 2] = l;
                out[i * 2 + 1] = r;
            }
        } else {
            for (unsigned long i = 0; i < framesPerBuffer; ++i) {
                out[i * 2] = left1[i] + left2[i];
                out[i * 2 + 1] = right1[i] + right2[i];
            }
        }
    }

    return paContinue;
}

// Improvement 1: Timestamped logging
void logWithTimestamp(const std::string& message) {
    auto now = std::chrono::system_clock::now();
    std::time_t now_time = std::chrono::system_clock::to_time_t(now);
    std::cout << "[" << std::ctime(&now_time) << "] " << message << std::endl;
}


SHRED_API void InitializeEngine(bool isTestMode) {
    try {
        g_isTestMode = isTestMode;
        logWithTimestamp("ShredEngine initialization starting");
        g_ampManager = std::make_unique<Selekta>();
        logWithTimestamp("Selekta created");
        g_mixer = std::make_unique<ClubMixer>();
        logWithTimestamp("ClubMixer created");
        g_deck1 = std::make_unique<ScratchBuffer>();
        logWithTimestamp("Deck 1 (ScratchBuffer) created");
        g_deck2 = std::make_unique<ScratchBuffer>();
        logWithTimestamp("Deck 2 (ScratchBuffer) created");

        // Open audio stream - prefer ASIO device
        int deviceIndex = Pa_GetDefaultOutputDevice();
        int numDevices = Pa_GetDeviceCount();
        std::cout << "[ShredEngine] Num devices: " << numDevices << ", default: " << deviceIndex << std::endl;
        for (int i = 0; i < numDevices; ++i) {
            const PaDeviceInfo* deviceInfo = Pa_GetDeviceInfo(i);
            if (deviceInfo) {
                std::cout << "[ShredEngine] Device " << i << ": " << deviceInfo->name << " (out: " << deviceInfo->maxOutputChannels << ")" << std::endl;
                if (std::string(deviceInfo->name).find("ASIO") != std::string::npos && deviceInfo->maxOutputChannels > 0) {
                    deviceIndex = i;
                    std::cout << "[ShredEngine] Selected ASIO device: " << i << std::endl;
                    break;
                } else if (std::string(deviceInfo->name).find("Realtek") != std::string::npos && deviceInfo->maxOutputChannels > 0) {
                    deviceIndex = i;
                    std::cout << "[ShredEngine] Selected Realtek device: " << i << std::endl;
                    break;
                }
            }
        }
        PaStreamParameters outputParameters;
        outputParameters.device = deviceIndex;
        if (outputParameters.device == paNoDevice) {
            std::cout << "[ShredEngine] No output device found" << std::endl;
            throw std::runtime_error("No output device");
        }
        const PaDeviceInfo* deviceInfo = Pa_GetDeviceInfo(deviceIndex);
        std::cout << "[ShredEngine] Using device: " << deviceInfo->name << std::endl;
        outputParameters.channelCount = 2;
        outputParameters.sampleFormat = paFloat32;
        outputParameters.suggestedLatency = deviceInfo->defaultLowOutputLatency;
        outputParameters.hostApiSpecificStreamInfo = nullptr;

        PaError err = Pa_OpenStream(&g_stream, nullptr, &outputParameters, 44100, 512, paClipOff, audioCallback, nullptr);
        if (err != paNoError) {
            logWithTimestamp("Failed to open stream: " + std::string(Pa_GetErrorText(err)));
            throw std::runtime_error("Failed to open stream");
        }

        err = Pa_StartStream(g_stream);
        if (err != paNoError) {
            logWithTimestamp("Failed to start stream: " + std::string(Pa_GetErrorText(err)));
            throw std::runtime_error("Failed to start stream");
        }

        logWithTimestamp("Audio stream opened and started");
        logWithTimestamp("ShredEngine initialized successfully");
    } catch (const std::exception& e) {
        logWithTimestamp("ShredEngine initialization failed: " + std::string(e.what()));
    }
}

SHRED_API int LoadFile(int deck, const char* filePath) {
    try {
        bool success = false;
        if (deck == 1 && g_deck1) {
            success = g_deck1->loadFile(filePath);
        } else if (deck == 2 && g_deck2) {
            success = g_deck2->loadFile(filePath);
        } else {
            std::cout << "[ShredEngine] Invalid deck number: " << deck << std::endl;
            return -1;
        }
        if (success) {
            std::cout << "[ShredEngine] File loaded successfully on deck " << deck << std::endl;
            return 0;
        } else {
            std::cout << "[ShredEngine] Failed to load file on deck " << deck << std::endl;
            return -1;
        }
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in LoadFile: " << e.what() << std::endl;
        return -1;
    }
}

SHRED_API void Play(int deck) {
    try {
        if (deck == 1 && g_deck1) g_deck1->play();
        else if (deck == 2 && g_deck2) g_deck2->play();
        else std::cout << "[ShredEngine] Invalid deck or not initialized: " << deck << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in Play: " << e.what() << std::endl;
    }
}

SHRED_API void Pause(int deck) {
    try {
        if (deck == 1 && g_deck1) g_deck1->pause();
        else if (deck == 2 && g_deck2) g_deck2->pause();
        else std::cout << "[ShredEngine] Invalid deck or not initialized: " << deck << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in Pause: " << e.what() << std::endl;
    }
}

SHRED_API void Stop(int deck) {
    try {
        if (deck == 1 && g_deck1) g_deck1->pause();
        else if (deck == 2 && g_deck2) g_deck2->pause();
        else std::cout << "[ShredEngine] Invalid deck or not initialized: " << deck << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in Stop: " << e.what() << std::endl;
    }
}

SHRED_API void Seek(int deck, double seconds) {
    try {
        long frame = (long)(seconds * 44100);
        if (deck == 1 && g_deck1) g_deck1->seek(frame);
        else if (deck == 2 && g_deck2) g_deck2->seek(frame);
        else std::cout << "[ShredEngine] Invalid deck or not initialized: " << deck << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in Seek: " << e.what() << std::endl;
    }
}

SHRED_API double GetPosition(int deck) {
    try {
        if (deck == 1 && g_deck1) return g_deck1->getPosition();
        else if (deck == 2 && g_deck2) return g_deck2->getPosition();
        return 0.0;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in GetPosition: " << e.what() << std::endl;
        return 0.0;
    }
}

SHRED_API double GetLength(int deck) {
    try {
        if (deck == 1 && g_deck1) return g_deck1->getLength();
        else if (deck == 2 && g_deck2) return g_deck2->getLength();
        return 0.0;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in GetLength: " << e.what() << std::endl;
        return 0.0;
    }
}

SHRED_API void SetVolume(int deck, float volume) {
    try {
        if (g_mixer) g_mixer->setVolume(deck, volume);
        else std::cout << "[ShredEngine] Mixer not initialized" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetVolume: " << e.what() << std::endl;
    }
}





SHRED_API void SetCrossfader(float value) {
    try {
        if (g_mixer) g_mixer->setCrossfader(value);
        else std::cout << "[ShredEngine] Mixer not initialized" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetCrossfader: " << e.what() << std::endl;
    }
}

SHRED_API void ShutdownEngine() {
    try {
        std::cout << "[ShredEngine] Shutdown starting" << std::endl;
        if (g_stream) {
            Pa_StopStream(g_stream);
            Pa_CloseStream(g_stream);
            g_stream = nullptr;
            std::cout << "[ShredEngine] Audio stream stopped and closed" << std::endl;
        }
        g_deck2.reset();
        std::cout << "[ShredEngine] Deck 2 destroyed" << std::endl;
        g_deck1.reset();
        std::cout << "[ShredEngine] Deck 1 destroyed" << std::endl;
        g_mixer.reset();
        std::cout << "[ShredEngine] Mixer destroyed" << std::endl;
        g_ampManager.reset();
        std::cout << "[ShredEngine] Selekta destroyed" << std::endl;
        std::cout << "[ShredEngine] Shutdown complete" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in ShutdownEngine: " << e.what() << std::endl;
    }
}

