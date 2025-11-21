#pragma once

#include <vector>
#include <string>

struct FileInfo {
    std::string format;
    int sampleRate;
    int channels;
    int bitsPerSample;
    long lengthSamples;
    double duration;
    std::string title;
    std::string artist;
};

class ScratchBuffer {
public:
    ScratchBuffer();
    ~ScratchBuffer();

    static bool getFileInfo(const std::string& filePath, FileInfo& info);
    bool initialize(void* stream);
    bool loadFile(const std::string& filePath);
    bool loadWAV(const std::string& filePath, const FileInfo& info);
    bool loadMP3(const std::string& filePath, const FileInfo& info);
    void getAudio(float* left, float* right, int frames);
    void play();
    void pause();
    void seek(long frame);
    void setSpeed(double ratio);
    double getPosition();
    double getLength();

private:
    void* m_stream;
    bool m_isPlaying;
    long m_currentFrame;
    double m_speed;
    std::vector<float> m_audioData;
    long m_length;
    int m_channels;
    int m_sampleRate;
    int m_bitsPerSample;
};