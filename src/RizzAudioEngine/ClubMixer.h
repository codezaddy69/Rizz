#pragma once

class ClubMixer {
public:
    RizzEngineMixer();
    ~RizzEngineMixer();

    void mix(float* left, float* right, int frames);
    void setCrossfader(float position);
    void setVolume(int deck, float gain);
    void setMasterVolume(float gain);

private:
    float m_crossfader;
    float m_volumes[2];
    float m_masterVolume;
};