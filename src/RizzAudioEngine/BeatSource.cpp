#include "RizzSoundSource.h"
#include <iostream>

BeatSource::BeatSource(const std::string& filePath) : m_filePath(filePath) {
    std::cout << "BeatSource created for " << filePath << std::endl;
}

BeatSource::~BeatSource() {
    std::cout << "BeatSource destroyed" << std::endl;
}