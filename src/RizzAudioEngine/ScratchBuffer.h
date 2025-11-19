#pragma once

#include <portaudio.h>

class ScratchBuffer {
public:
    ScratchBuffer();
    ~ScratchBuffer();

    bool initialize(PaStream* stream);
    void process(int frames, float* output);
    void play();
    void pause();
    void seek(long frame);
    void setSpeed(double ratio);
    double getPosition();
    double getLength();

private:
    PaStream* m_stream;
    bool m_isPlaying;
    long m_currentFrame;
    double m_speed;
};