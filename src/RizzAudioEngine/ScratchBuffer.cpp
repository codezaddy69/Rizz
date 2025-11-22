#include "ScratchBuffer.h"
#include <iostream>
#include <fstream>
#include <cstring>
#include <cmath>
#include <cstdint>
#define DR_MP3_IMPLEMENTATION
#include "dr_mp3.h"

static std::ofstream debugLog("scratchbuffer_debug.log", std::ios::app);

void resampleAudio(std::vector<float>& data, int channels, int srcRate, int dstRate) {
    if (srcRate == dstRate) return;
    debugLog << "[resampleAudio] Resampling from " << srcRate << " to " << dstRate << std::endl;
    size_t srcSamples = data.size() / channels;
    size_t dstSamples = (size_t)(srcSamples * (double)dstRate / srcRate);
    std::vector<float> newData(dstSamples * channels);
    for (size_t i = 0; i < dstSamples; ++i) {
        double srcPos = (double)i * srcRate / dstRate;
        size_t srcIdx = (size_t)srcPos;
        double frac = srcPos - srcIdx;
        if (srcIdx + 1 >= srcSamples) {
            srcIdx = srcSamples - 1;
            frac = 0.0;
        }
        for (int ch = 0; ch < channels; ++ch) {
            float val1 = data[srcIdx * channels + ch];
            float val2 = data[(srcIdx + 1) * channels + ch];
            newData[i * channels + ch] = val1 * (1.0f - frac) + val2 * frac;
        }
    }
    data = std::move(newData);
}

bool ScratchBuffer::getFileInfo(const std::string& filePath, FileInfo& info) {
    std::ifstream file(filePath, std::ios::binary);
    if (!file) {
        return false;
    }

    // Check extension
    size_t dotPos = filePath.find_last_of('.');
    std::string ext = (dotPos != std::string::npos) ? filePath.substr(dotPos) : "";

    if (ext == ".wav" || ext == ".WAV") {
        // Parse WAV header
        char riff[4];
        file.read(riff, 4);
        if (std::memcmp(riff, "RIFF", 4) != 0) return false;
        file.seekg(8, std::ios::beg);
        char wave[4];
        file.read(wave, 4);
        if (std::memcmp(wave, "WAVE", 4) != 0) return false;

        // Find fmt
        while (!file.eof()) {
            char chunkId[4];
            file.read(chunkId, 4);
            if (file.eof()) break;
            uint32_t chunkSize;
            file.read(reinterpret_cast<char*>(&chunkSize), 4);
            if (std::memcmp(chunkId, "fmt ", 4) == 0) {
                uint16_t audioFormat, numChannels, bits;
                uint32_t sampleRate;
                file.read(reinterpret_cast<char*>(&audioFormat), 2);
                file.read(reinterpret_cast<char*>(&numChannels), 2);
                file.read(reinterpret_cast<char*>(&sampleRate), 4);
                file.seekg(6, std::ios::cur);
                file.read(reinterpret_cast<char*>(&bits), 2);
                info.channels = numChannels;
                info.sampleRate = sampleRate;
                info.bitsPerSample = bits;
                info.audioFormat = audioFormat;
                file.seekg(chunkSize - 16, std::ios::cur);
                break;
            } else {
                file.seekg(chunkSize, std::ios::cur);
            }
        }

        // Find data
        while (!file.eof()) {
            char chunkId[4];
            file.read(chunkId, 4);
            if (file.eof()) break;
            uint32_t chunkSize;
            file.read(reinterpret_cast<char*>(&chunkSize), 4);
            if (std::memcmp(chunkId, "data", 4) == 0) {
                info.lengthSamples = chunkSize / (info.bitsPerSample / 8) / info.channels;
                info.duration = (double)info.lengthSamples / info.sampleRate;
                info.format = "WAV";
                return true;
            } else {
                file.seekg(chunkSize, std::ios::cur);
            }
        }
    } else if (ext == ".mp3" || ext == ".MP3") {
        // Placeholder for MP3
        info.format = "MP3";
        info.sampleRate = 44100; // assume
        info.channels = 2;
        info.bitsPerSample = 16;
        info.lengthSamples = 0; // unknown
        info.duration = 0;
        return true;
    }

    return false;
}

ScratchBuffer::ScratchBuffer() : m_stream(nullptr), m_isPlaying(false), m_currentFrame(0), m_speed(1.0), m_length(0), m_channels(0) {
    debugLog << "Starting ScratchBuffer boot" << std::endl;
    debugLog.flush();
    std::cout << "[ScratchBuffer] Created" << std::endl;
}

ScratchBuffer::~ScratchBuffer() {
    std::cout << "[ScratchBuffer] Destroyed" << std::endl;
}

bool ScratchBuffer::initialize(void* stream) {
    m_stream = stream;
    std::cout << "[ScratchBuffer] Initialized with stream (dummy)" << std::endl;
    return true;
}

bool ScratchBuffer::loadFile(const std::string& filePath) {
    FileInfo info;
    if (!getFileInfo(filePath, info)) {
        std::cout << "[ScratchBuffer] Unsupported file format: " << filePath << std::endl;
        return false;
    }

    // Log file info
    debugLog << "[ScratchBuffer] File info: " << filePath << " - Format: " << info.format
             << ", SampleRate: " << info.sampleRate << ", Channels: " << info.channels
             << ", Bits: " << info.bitsPerSample << ", Length: " << info.lengthSamples
             << " samples (" << info.duration << "s)" << std::endl;
    debugLog.flush();

    // Validate
    if (info.sampleRate != 44100 && info.sampleRate != 48000) {
        debugLog << "[ScratchBuffer] Warning: Unusual sample rate " << info.sampleRate << " Hz" << std::endl;
    }
    if (info.channels < 1 || info.channels > 2) {
        debugLog << "[ScratchBuffer] Warning: Unsupported channel count " << info.channels << std::endl;
        return false;
    }
    if (info.bitsPerSample != 16) {
        debugLog << "[ScratchBuffer] Warning: Non-16-bit files may not load correctly" << std::endl;
    }

    if (info.format == "WAV") {
        return loadWAV(filePath, info);
    } else if (info.format == "MP3") {
        return loadMP3(filePath, info);
    }

    return false;
}

bool ScratchBuffer::loadWAV(const std::string& filePath, const FileInfo& info) {
    debugLog << "Starting file load boot for " << filePath << std::endl;
    debugLog.flush();
    debugLog << "[ScratchBuffer] loadWAV start" << std::endl;
    debugLog << "[ScratchBuffer] loadWAV called for " << filePath << std::endl;
    m_channels = info.channels;
    m_sampleRate = info.sampleRate;
    m_bitsPerSample = info.bitsPerSample;
    m_length = info.lengthSamples;

    std::ifstream file(filePath, std::ios::binary);
    if (!file) {
        std::cout << "[ScratchBuffer] Failed to open file: " << filePath << std::endl;
        return false;
    }

    // Skip to data
    file.seekg(12, std::ios::beg); // RIFF + size + WAVE
    while (!file.eof()) {
        char chunkId[4];
        file.read(chunkId, 4);
        if (file.eof()) break;
        uint32_t chunkSize;
        file.read(reinterpret_cast<char*>(&chunkSize), 4);
        if (std::memcmp(chunkId, "data", 4) == 0) {
            m_audioData.resize(m_length * m_channels);
            if (m_bitsPerSample == 16) {
                std::vector<int16_t> rawData(m_length * m_channels);
                file.read(reinterpret_cast<char*>(rawData.data()), chunkSize);
                for (size_t i = 0; i < rawData.size(); ++i) {
                    m_audioData[i] = rawData[i] / 32768.0f;
                }
            } else if (m_bitsPerSample == 32) {
                if (info.audioFormat == 3) {
                    // float
                    file.read(reinterpret_cast<char*>(m_audioData.data()), chunkSize);
                } else {
                    // int32 PCM
                    std::vector<int32_t> rawData(m_length * m_channels);
                    file.read(reinterpret_cast<char*>(rawData.data()), chunkSize);
                    for (size_t i = 0; i < rawData.size(); ++i) {
                        m_audioData[i] = rawData[i] / 2147483648.0f;
                    }
                }
            }
            // Resample to 44100 Hz
            debugLog << "[ScratchBuffer] Before resample, rate=" << m_sampleRate << std::endl;
            resampleAudio(m_audioData, m_channels, m_sampleRate, 44100);
            m_sampleRate = 44100;
            m_length = m_audioData.size() / m_channels;
            debugLog << "[ScratchBuffer] Loaded and resampled WAV data, length=" << m_length << ", channels=" << m_channels << ", rate=" << m_sampleRate << ", bits=" << m_bitsPerSample << std::endl;
            return true;
        } else {
            file.seekg(chunkSize, std::ios::cur);
        }
    }

    std::cout << "[ScratchBuffer] No data chunk found" << std::endl;
    return false;
}

bool ScratchBuffer::loadMP3(const std::string& filePath, const FileInfo& info) {
    debugLog << "Starting file load boot for " << filePath << std::endl;
    debugLog.flush();
    drmp3 mp3;
    if (!drmp3_init_file(&mp3, filePath.c_str(), NULL)) {
        std::cout << "[ScratchBuffer] Failed to open MP3 file: " << filePath << std::endl;
        return false;
    }

    m_channels = mp3.channels;
    m_sampleRate = mp3.sampleRate;
    m_bitsPerSample = 32; // float
    drmp3_uint64 totalFrames = drmp3_get_pcm_frame_count(&mp3);
    m_length = totalFrames;
    m_audioData.resize(totalFrames * m_channels);

    size_t framesRead = drmp3_read_pcm_frames_f32(&mp3, totalFrames, m_audioData.data());
    if (framesRead != totalFrames) {
        std::cout << "[ScratchBuffer] Failed to read all MP3 frames" << std::endl;
        drmp3_uninit(&mp3);
        return false;
    }

    drmp3_uninit(&mp3);
    // Resample to 44100 Hz if needed
    if (m_sampleRate != 44100) {
        resampleAudio(m_audioData, m_channels, m_sampleRate, 44100);
        m_sampleRate = 44100;
        m_length = m_audioData.size() / m_channels;
    }
    debugLog << "[ScratchBuffer] Loaded and resampled MP3 data, length=" << m_length << ", channels=" << m_channels << ", rate=" << m_sampleRate << std::endl;
    return true;
}

void ScratchBuffer::getAudio(float* left, float* right, int frames) {
    if (!m_isPlaying || m_audioData.empty()) {
        // Fill with silence
        for (int i = 0; i < frames; ++i) {
            left[i] = 0.0f;
            right[i] = 0.0f;
        }
        return;
    }

    // Play from audio data
    for (int i = 0; i < frames; ++i) {
        long pos = m_currentFrame + i;
        if (pos >= m_length) pos %= m_length; // Loop
        if (m_channels == 1) {
            float sample = m_audioData[pos];
            left[i] = sample;
            right[i] = sample;
        } else if (m_channels == 2) {
            left[i] = m_audioData[pos * 2];
            right[i] = m_audioData[pos * 2 + 1];
        } else {
            left[i] = 0.0f;
            right[i] = 0.0f;
        }
    }

    m_currentFrame += frames;
}

void ScratchBuffer::play() {
    m_isPlaying = true;
    std::cout << "[ScratchBuffer] Play started" << std::endl;
}

void ScratchBuffer::pause() {
    m_isPlaying = false;
    std::cout << "[ScratchBuffer] Paused" << std::endl;
}

void ScratchBuffer::seek(long frame) {
    m_currentFrame = frame;
    std::cout << "[ScratchBuffer] Seek to frame " << frame << std::endl;
}

void ScratchBuffer::setSpeed(double ratio) {
    m_speed = ratio;
    std::cout << "[ScratchBuffer] Speed set to " << ratio << std::endl;
}

double ScratchBuffer::getPosition() {
    return m_currentFrame / 44100.0; // Assume 44.1kHz
}

double ScratchBuffer::getLength() {
    return m_length / 44100.0;
}