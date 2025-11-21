#include "Selekta.h"
#include <iostream>

Selekta::Selekta() : m_stream(nullptr), m_bufferSize(512) {
    PaError err = Pa_Initialize();
    if (err != paNoError) {
        std::cout << "[Selekta] PortAudio init failed: " << Pa_GetErrorText(err) << std::endl;
    } else {
        std::cout << "[Selekta] PortAudio initialized" << std::endl;
    }
}

Selekta::~Selekta() {
    if (m_stream) {
        Pa_StopStream(m_stream);
        Pa_CloseStream(m_stream);
        m_stream = nullptr;
    }
    Pa_Terminate();
    std::cout << "[Selekta] PortAudio terminated" << std::endl;
}

std::vector<DeviceInfo> Selekta::enumerateDevices() {
    std::vector<DeviceInfo> devices;
    int numDevices = Pa_GetDeviceCount();
    if (numDevices < 0) {
        std::cout << "[Selekta] Pa_GetDeviceCount failed: " << Pa_GetErrorText(numDevices) << std::endl;
        return devices;
    }
    for (int i = 0; i < numDevices; ++i) {
        const PaDeviceInfo* deviceInfo = Pa_GetDeviceInfo(i);
        if (deviceInfo) {
            devices.push_back({deviceInfo->name, deviceInfo->maxOutputChannels, (int)deviceInfo->defaultSampleRate});
        }
    }
    std::cout << "[Selekta] Enumerated " << devices.size() << " devices" << std::endl;
    return devices;
}

bool Selekta::openDevice(const std::string& name, int bufferSize) {
    // Find device by name
    int numDevices = Pa_GetDeviceCount();
    int deviceIndex = -1;
    for (int i = 0; i < numDevices; ++i) {
        const PaDeviceInfo* deviceInfo = Pa_GetDeviceInfo(i);
        if (deviceInfo && std::string(deviceInfo->name) == name) {
            deviceIndex = i;
            break;
        }
    }
    if (deviceIndex == -1) {
        std::cout << "[Selekta] Device '" << name << "' not found" << std::endl;
        return false;
    }

    PaStreamParameters outputParameters;
    outputParameters.device = deviceIndex;
    outputParameters.channelCount = 2; // Stereo
    outputParameters.sampleFormat = paFloat32;
    outputParameters.suggestedLatency = Pa_GetDeviceInfo(deviceIndex)->defaultLowOutputLatency;
    outputParameters.hostApiSpecificStreamInfo = nullptr;

    PaError err = Pa_OpenStream(&m_stream, nullptr, &outputParameters, 44100, bufferSize, paClipOff, nullptr, nullptr);
    if (err != paNoError) {
        std::cout << "[Selekta] Failed to open stream: " << Pa_GetErrorText(err) << std::endl;
        return false;
    }

    m_bufferSize = bufferSize;
    std::cout << "[Selekta] Opened device '" << name << "' with buffer size " << bufferSize << std::endl;
    return true;
}

void Selekta::startStream() {
    if (m_stream) {
        PaError err = Pa_StartStream(m_stream);
        if (err != paNoError) {
            std::cout << "[Selekta] Failed to start stream: " << Pa_GetErrorText(err) << std::endl;
        } else {
            std::cout << "[Selekta] Stream started" << std::endl;
        }
    }
}

void Selekta::stopStream() {
    if (m_stream) {
        PaError err = Pa_StopStream(m_stream);
        if (err != paNoError) {
            std::cout << "[Selekta] Failed to stop stream: " << Pa_GetErrorText(err) << std::endl;
        } else {
            std::cout << "[Selekta] Stream stopped" << std::endl;
        }
    }
}

void Selekta::setBufferSize(int size) {
    m_bufferSize = size;
    std::cout << "[Selekta] Buffer size set to " << size << std::endl;
}

PaStream* Selekta::getStream() {
    return m_stream;
}