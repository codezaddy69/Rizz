#include "ClubMixer.h"
#include <iostream>
#include <algorithm>

ClubMixer::ClubMixer() : m_crossfader(0.0f), m_masterVolume(1.0f) {
    m_volumes[0] = 1.0f;
    m_volumes[1] = 1.0f;
    std::cout << "[ClubMixer] Initialized with crossfader=0.0, masterVolume=1.0, volumes[0]=1.0, volumes[1]=1.0" << std::endl;
}

ClubMixer::~ClubMixer() {
    std::cout << "[ClubMixer] Destroyed" << std::endl;
}

void ClubMixer::mix(float* left, float* right, int frames) {
    // Placeholder: simple crossfader logic
    for (int i = 0; i < frames; ++i) {
        float l = left[i] * m_volumes[0] * (1.0f - std::max(0.0f, m_crossfader));
        float r = right[i] * m_volumes[1] * (1.0f + std::min(0.0f, m_crossfader));
        left[i] = l * m_masterVolume;
        right[i] = r * m_masterVolume;
    }
}

void ClubMixer::setCrossfader(float position) {
    m_crossfader = position;
    std::cout << "[ClubMixer] Crossfader set to " << position << std::endl;
}

void ClubMixer::setVolume(int deck, float gain) {
    if (deck >= 0 && deck < 2) {
        m_volumes[deck] = gain;
        std::cout << "[ClubMixer] Volume for deck " << deck << " set to " << gain << std::endl;
    } else {
        std::cout << "[ClubMixer] Invalid deck " << deck << " for setVolume" << std::endl;
    }
}

void ClubMixer::setMasterVolume(float gain) {
    m_masterVolume = gain;
    std::cout << "[ClubMixer] Master volume set to " << gain << std::endl;
}