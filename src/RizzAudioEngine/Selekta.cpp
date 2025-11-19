#include "Selekta.h"
#include <iostream>

Selekta::Selekta() : m_stream(nullptr), m_bufferSize(512) {
    // Improvement 3: PortAudio version check
    const PaVersionInfo* version = Pa_GetVersionInfo();
    std::cout << "PortAudio version: " << version->versionText << std::endl;

    PaError err = Pa_Initialize();
    if (err != paNoError) {
        std::cout << "PortAudio init failed: " << Pa_GetErrorText(err) << std::endl;
    } else {
        std::cout << "Selekta created" << std::endl;
    }
}

Selekta::~Selekta() {
    if (m_stream) {
        Pa_StopStream(m_stream);
        Pa_CloseStream(m_stream);
    }
    Pa_Terminate();
    std::cout << "Selekta destroyed" << std::endl;
}

std::vector<DeviceInfo> Selekta::enumerateDevices() {
    std::vector<DeviceInfo> devices;
    int numDevices = Pa_GetDeviceCount();
    for (int i = 0; i < numDevices; ++i) {
        const PaDeviceInfo* deviceInfo = Pa_GetDeviceInfo(i);
        if (deviceInfo && deviceInfo->maxOutputChannels > 0) {
            devices.push_back({deviceInfo->name, deviceInfo->maxOutputChannels, (int)deviceInfo->defaultSampleRate});
        }
    }
    std::cout << "Amp enumerated " << devices.size() << " devices" << std::endl;
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
        std::cout << "Device not found: " << name << std::endl;
        return false;
    }

    PaStreamParameters outputParameters;
    outputParameters.device = deviceIndex;
    outputParameters.channelCount = 2;
    outputParameters.sampleFormat = paFloat32;
    outputParameters.suggestedLatency = Pa_GetDeviceInfo(deviceIndex)->defaultLowOutputLatency;
    outputParameters.hostApiSpecificStreamInfo = nullptr;

    PaError err = Pa_OpenStream(&m_stream, nullptr, &outputParameters, 44100, bufferSize, paClipOff, nullptr, nullptr);
    if (err != paNoError) {
        std::cout << "Failed to open stream: " << Pa_GetErrorText(err) << std::endl;
        return false;
    }

    m_bufferSize = bufferSize;
    std::cout << "Amp opening device " << name << " with buffer size " << bufferSize << std::endl;
    return true;
}

void Selekta::startStream() {
    if (m_stream) {
        PaError err = Pa_StartStream(m_stream);
        if (err != paNoError) {
            std::cout << "Failed to start stream: " << Pa_GetErrorText(err) << std::endl;
        } else {
            std::cout << "Amp starting audio stream" << std::endl;
        }
    }
}

void Selekta::stopStream() {
    if (m_stream) {
        Pa_StopStream(m_stream);
        std::cout << "Amp stopping audio stream" << std::endl;
    }
}

void Selekta::setBufferSize(int size) {
    m_bufferSize = size;
    std::cout << "Amp setting buffer size to " << size << std::endl;
}

PaStream* Selekta::getStream() {
    return m_stream;
}