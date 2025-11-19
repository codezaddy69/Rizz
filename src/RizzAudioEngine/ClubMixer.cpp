#include "RizzEngineMixer.h"
#include <iostream>
#include <algorithm>

RizzEngineMixer::RizzEngineMixer() : m_crossfader(0.0f), m_masterVolume(1.0f) {
    m_volumes[0] = 1.0f;
    m_volumes[1] = 1.0f;
    std::cout << "RizzEngineMixer created" << std::endl;
}

RizzEngineMixer::~RizzEngineMixer() {
    std::cout << "RizzEngineMixer destroyed" << std::endl;
}

void RizzEngineMixer::mix(float* left, float* right, int frames) {
    // Placeholder: simple crossfader logic
    for (int i = 0; i < frames; ++i) {
        float l = left[i] * m_volumes[0] * (1.0f - std::max(0.0f, m_crossfader));
        float r = right[i] * m_volumes[1] * (1.0f + std::min(0.0f, m_crossfader));
        left[i] = l * m_masterVolume;
        right[i] = r * m_masterVolume;
    }
    std::cout << "Mixing " << frames << " frames" << std::endl;
}

void RizzEngineMixer::setCrossfader(float position) {
    m_crossfader = position;
    std::cout << "Crossfader set to " << position << std::endl;
}

void RizzEngineMixer::setVolume(int deck, float gain) {
    if (deck >= 0 && deck < 2) {
        m_volumes[deck] = gain;
        std::cout << "Volume for deck " << deck << " set to " << gain << std::endl;
    }
}

void RizzEngineMixer::setMasterVolume(float gain) {
    m_masterVolume = gain;
    std::cout << "Master volume set to " << gain << std::endl;
}