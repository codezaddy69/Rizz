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

static std::ofstream logFile("shredengine.log", std::ios::app);

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
        logFile << "[Callback] Started, framesPerBuffer=" << framesPerBuffer << ", testMode=" << g_isTestMode << std::endl;
        logFile.flush();
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

        // Bus-based mixing: Assign decks to LEFT/RIGHT buses, apply crossfader to buses
        float deck1_vol = g_mixer ? g_mixer->getDeckVolume(0) : 1.0f;
        float deck2_vol = g_mixer ? g_mixer->getDeckVolume(1) : 1.0f;
        float master_gain = g_mixer ? g_mixer->getMasterVolume() : 1.0f;
        float crossfader = g_mixer ? g_mixer->getCrossfader() : 0.0f;
        logFile << "Deck vols: " << deck1_vol << ", " << deck2_vol << ", crossfader: " << crossfader << std::endl;

        // Crossfader gains for buses (additive style, but bus-based)
        float left_bus_gain = 1.0f - std::max(0.0f, crossfader);
        float right_bus_gain = 1.0f - std::max(0.0f, -crossfader);

        for (unsigned long i = 0; i < framesPerBuffer; ++i) {
            // Deck 1 to LEFT bus, Deck 2 to RIGHT bus
            float left_bus_l = left1[i] * deck1_vol;
            float left_bus_r = right1[i] * deck1_vol;
            float right_bus_l = left2[i] * deck2_vol;
            float right_bus_r = right2[i] * deck2_vol;

            // Mix buses with crossfader gains
            out[i * 2] = (left_bus_l * left_bus_gain + right_bus_l * right_bus_gain) * master_gain;
            out[i * 2 + 1] = (left_bus_r * left_bus_gain + right_bus_r * right_bus_gain) * master_gain;
        }

        // Apply output DSP (clipping protection)
        if (g_mixer) {
            std::vector<float> left_out(framesPerBuffer), right_out(framesPerBuffer);
            for (unsigned long i = 0; i < framesPerBuffer; ++i) {
                left_out[i] = out[i * 2];
                right_out[i] = out[i * 2 + 1];
            }
            g_mixer->applyOutputDSP(left_out.data(), right_out.data(), framesPerBuffer);
            for (unsigned long i = 0; i < framesPerBuffer; ++i) {
                out[i * 2] = left_out[i];
                out[i * 2 + 1] = right_out[i];
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
    logFile << "InitializeEngine called" << std::endl;
    logFile.flush();
    try {
        g_isTestMode = isTestMode;
        logFile << "DLL built at " << __DATE__ << " " << __TIME__ << std::endl;
        logFile.flush();
        logWithTimestamp("Starting ShredEngine boot");
        logWithTimestamp("ShredEngine initialization starting");
        logFile << "Starting Selekta boot" << std::endl;
        logFile.flush();
        g_ampManager = std::make_unique<Selekta>();
        logWithTimestamp("Selekta created");
        logFile << "Starting ClubMixer boot" << std::endl;
        logFile.flush();
        g_mixer = std::make_unique<ClubMixer>();
        logWithTimestamp("ClubMixer created");
        logFile << "Starting ScratchBuffer deck 1 boot" << std::endl;
        logFile.flush();
        g_deck1 = std::make_unique<ScratchBuffer>();
        logFile << "[ShredEngine] Deck 1 (ScratchBuffer) created" << std::endl;
        logFile.flush();
        logFile << "Starting ScratchBuffer deck 2 boot" << std::endl;
        logFile.flush();
        g_deck2 = std::make_unique<ScratchBuffer>();
        logFile << "[ShredEngine] Deck 2 (ScratchBuffer) created" << std::endl;
        logFile.flush();

        // Open audio stream - prefer ASIO device
        int deviceIndex = Pa_GetDefaultOutputDevice();
        int numDevices = Pa_GetDeviceCount();
        logFile << "[ShredEngine] Num devices: " << numDevices << ", default: " << deviceIndex << std::endl;
        for (int i = 0; i < numDevices; ++i) {
            const PaDeviceInfo* deviceInfo = Pa_GetDeviceInfo(i);
            if (deviceInfo) {
                logFile << "[ShredEngine] Device " << i << ": " << deviceInfo->name << " (out: " << deviceInfo->maxOutputChannels << ")" << std::endl;
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
        logFile << "[ShredEngine] Using device: " << deviceInfo->name << std::endl;
        logFile.flush();
        outputParameters.channelCount = 2;
        outputParameters.sampleFormat = paFloat32;
        outputParameters.suggestedLatency = deviceInfo->defaultLowOutputLatency;
        outputParameters.hostApiSpecificStreamInfo = nullptr;

        logFile << "Starting audio stream boot" << std::endl;
        logFile.flush();
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
        if (deck == 1 && g_deck1) {
            g_deck1->play();
            std::cout << "[ShredEngine] Play started on deck 1" << std::endl;
        } else if (deck == 2 && g_deck2) {
            g_deck2->play();
            std::cout << "[ShredEngine] Play started on deck 2" << std::endl;
        } else std::cout << "[ShredEngine] Invalid deck or not initialized: " << deck << std::endl;
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
        if (g_mixer) g_mixer->setVolume(deck - 1, volume);  // Convert 1-based to 0-based indexing
        std::cout << "[ShredEngine] Volume set on deck " << deck << " to " << volume << std::endl;
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

SHRED_API void SetMasterVolume(float volume) {
    try {
        if (g_mixer) g_mixer->setMasterVolume(volume);
        else std::cout << "[ShredEngine] Mixer not initialized" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetMasterVolume: " << e.what() << std::endl;
    }
}

SHRED_API void SetCrossfaderCurve(int curveType) {
    try {
        if (g_mixer) g_mixer->setCrossfaderCurve(curveType);
        else std::cout << "[ShredEngine] Mixer not initialized" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetCrossfaderCurve: " << e.what() << std::endl;
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
        logFile << "[ShredEngine] Deck 2 destroyed" << std::endl;
        logFile.flush();
        g_deck1.reset();
        logFile << "[ShredEngine] Deck 1 destroyed" << std::endl;
        logFile.flush();
        g_mixer.reset();
        logFile << "[ShredEngine] Mixer destroyed" << std::endl;
        logFile.flush();
        g_ampManager.reset();
        logFile << "[ShredEngine] Selekta destroyed" << std::endl;
        logFile.flush();
        std::cout << "[ShredEngine] Shutdown complete" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in ShutdownEngine: " << e.what() << std::endl;
    }
}

// ============================================================================
// Clipping Protection Interop Functions
// ============================================================================

SHRED_API void SetClippingProtectionEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setClippingProtectionEnabled(enabled);
        std::cout << "[ShredEngine] Clipping protection " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetClippingProtectionEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetDeckVolumeCapEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setDeckVolumeCapEnabled(enabled);
        std::cout << "[ShredEngine] Deck volume cap " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetDeckVolumeCapEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetPeakDetectionEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setPeakDetectionEnabled(enabled);
        std::cout << "[ShredEngine] Peak detection " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetPeakDetectionEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetSoftKneeCompressorEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setSoftKneeCompressorEnabled(enabled);
        std::cout << "[ShredEngine] Soft knee compressor " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetSoftKneeCompressorEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetLookAheadLimiterEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setLookAheadLimiterEnabled(enabled);
        std::cout << "[ShredEngine] Look-ahead limiter " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetLookAheadLimiterEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetRmsMonitoringEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setRmsMonitoringEnabled(enabled);
        std::cout << "[ShredEngine] RMS monitoring " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetRmsMonitoringEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetAutoGainReductionEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setAutoGainReductionEnabled(enabled);
        std::cout << "[ShredEngine] Auto gain reduction " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetAutoGainReductionEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetBrickwallLimiterEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setBrickwallLimiterEnabled(enabled);
        std::cout << "[ShredEngine] Brickwall limiter " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetBrickwallLimiterEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetClippingIndicatorEnabled(bool enabled) {
    try {
        if (g_mixer) g_mixer->setClippingIndicatorEnabled(enabled);
        std::cout << "[ShredEngine] Clipping indicator " << (enabled ? "enabled" : "disabled") << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetClippingIndicatorEnabled: " << e.what() << std::endl;
    }
}

SHRED_API void SetClippingThreshold(float threshold) {
    try {
        if (g_mixer) g_mixer->setClippingThreshold(threshold);
        std::cout << "[ShredEngine] Clipping threshold set to " << threshold << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetClippingThreshold: " << e.what() << std::endl;
    }
}

SHRED_API void SetCompressorRatio(float ratio) {
    try {
        if (g_mixer) g_mixer->setCompressorRatio(ratio);
        std::cout << "[ShredEngine] Compressor ratio set to " << ratio << ":1" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetCompressorRatio: " << e.what() << std::endl;
    }
}

SHRED_API void SetLimiterAttackTime(float attackMs) {
    try {
        if (g_mixer) g_mixer->setLimiterAttackTime(attackMs);
        std::cout << "[ShredEngine] Limiter attack time set to " << attackMs << "ms" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetLimiterAttackTime: " << e.what() << std::endl;
    }
}

SHRED_API void SetLimiterReleaseTime(float releaseMs) {
    try {
        if (g_mixer) g_mixer->setLimiterReleaseTime(releaseMs);
        std::cout << "[ShredEngine] Limiter release time set to " << releaseMs << "ms" << std::endl;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in SetLimiterReleaseTime: " << e.what() << std::endl;
    }
}

SHRED_API float GetCurrentPeakLevel() {
    try {
        return g_mixer ? g_mixer->getCurrentPeakLevel() : 0.0f;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in GetCurrentPeakLevel: " << e.what() << std::endl;
        return 0.0f;
    }
}

SHRED_API float GetCurrentRmsLevel() {
    try {
        return g_mixer ? g_mixer->getCurrentRmsLevel() : 0.0f;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in GetCurrentRmsLevel: " << e.what() << std::endl;
        return 0.0f;
    }
}

SHRED_API bool IsClipping() {
    try {
        return g_mixer ? g_mixer->isClipping() : false;
    } catch (std::exception& e) {
        std::cout << "[ShredEngine] Exception in IsClipping: " << e.what() << std::endl;
        return false;
    }
}

