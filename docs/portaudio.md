# PortAudio Setup for RizzAudioEngine

## Overview
PortAudio is a cross-platform audio I/O library that enables low-latency audio processing. For DJMixMaster, it provides ASIO support on Windows for professional audio output, ensuring ASIO4ALL loads in the system tray and delivers high-performance audio streaming.

## Installation on Windows
Use vcpkg (C++ package manager) for seamless installation:

1. **Clone vcpkg**:
   ```
   git clone https://github.com/Microsoft/vcpkg.git
   cd vcpkg
   ```

2. **Bootstrap vcpkg**:
   ```
   bootstrap-vcpkg.bat
   ```

3. **Install PortAudio**:
   ```
   vcpkg install portaudio
   ```

4. **Configure CMake**:
   Set the toolchain file in your CMake project:
   ```
   set CMAKE_TOOLCHAIN_FILE=path\to\vcpkg\scripts\buildsystems\vcpkg.cmake
   ```

Alternatively, run the provided script: `ref/vcpkg_setup.bat`

## Integration into RizzAudioEngine
- **Selekta.cpp**: Replace dummy implementation with real PortAudio device enumeration and stream opening.
- **ShredEngine.cpp**: Implement PortAudio stream with callback that processes audio from ScratchBuffers and mixes via ClubMixer.
- **CMakeLists.txt**: Link against PortAudio library.

## ASIO Configuration
- On Windows, PortAudio can be configured to use ASIO drivers.
- Set ASIO4ALL as the default device in PortAudio initialization.
- This ensures ASIO4ALL appears in the system tray when the app starts, providing low-latency audio output.

## Building the DLL
- Cross-compile from Linux using mingw-w64 with PortAudio linked.
- The resulting DLL will support ASIO on Windows.

## Testing
- Run the app; ASIO4ALL should load in systray.
- Use `--run-test` for headless validation of audio processing.