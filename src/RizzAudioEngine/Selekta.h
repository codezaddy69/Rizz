#pragma once

#include <string>
#include <vector>
#include <portaudio.h>

struct DeviceInfo {
    std::string name;
    int channels;
    int sampleRate;
};

class Selekta {
public:
    Selekta();
    ~Selekta();

    std::vector<DeviceInfo> enumerateDevices();
    bool openDevice(const std::string& name, int bufferSize);
    void startStream();
    void stopStream();
    void setBufferSize(int size);
    PaStream* getStream();

private:
    PaStream* m_stream;
    int m_bufferSize;
};