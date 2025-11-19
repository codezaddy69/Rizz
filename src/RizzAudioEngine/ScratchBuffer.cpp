#include "ScratchBuffer.h"
#include <iostream>

ScratchBuffer::ScratchBuffer() : m_stream(nullptr), m_isPlaying(false), m_currentFrame(0), m_speed(1.0) {
    std::cout << "ScratchBuffer created" << std::endl;
}

ScratchBuffer::~ScratchBuffer() {
    std::cout << "ScratchBuffer destroyed" << std::endl;
}

bool ScratchBuffer::initialize(PaStream* stream) {
    m_stream = stream;
    std::cout << "ScratchBuffer initialized with stream" << std::endl;
    return true;
}

void ScratchBuffer::process(int frames, float* output) {
    if (!m_isPlaying) {
        // Fill with silence
        for (int i = 0; i < frames * 2; ++i) {
            output[i] = 0.0f;
        }
        return;
    }

    // Placeholder: generate sine wave for testing
    static double phase = 0.0;
    for (int i = 0; i < frames; ++i) {
        float sample = static_cast<float>(sin(phase) * 0.1);
        output[i * 2] = sample;     // Left
        output[i * 2 + 1] = sample; // Right
        phase += 0.1 * m_speed;
    }

    m_currentFrame += frames;
}

void ScratchBuffer::play() {
    m_isPlaying = true;
    std::cout << "EngineBuffer play" << std::endl;
}

void ScratchBuffer::pause() {
    m_isPlaying = false;
    std::cout << "EngineBuffer pause" << std::endl;
}

void ScratchBuffer::seek(long frame) {
    m_currentFrame = frame;
    std::cout << "ScratchBuffer seek to " << frame << std::endl;
}

void ScratchBuffer::setSpeed(double ratio) {
    m_speed = ratio;
    std::cout << "ScratchBuffer speed set to " << ratio << std::endl;
}

double ScratchBuffer::getPosition() {
    return m_currentFrame / 44100.0; // Assume 44.1kHz
}

double ScratchBuffer::getLength() {
    return 0.0; // Placeholder
}