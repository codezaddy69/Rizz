#pragma once

#include <string>

class RizzSoundSource {
public:
    RizzSoundSource(const std::string& filePath);
    virtual ~RizzSoundSource();

    virtual bool open() = 0;
    virtual int read(float* buffer, int samples) = 0;
    virtual void seek(long frame) = 0;
    virtual long length() = 0;
    virtual void close() = 0;

protected:
    std::string m_filePath;
};