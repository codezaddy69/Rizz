# PortAudio Integration Documentation

## Download and Setup
- **Source**: Cloned from https://github.com/PortAudio/portaudio.git
- **Location**: `libtard/portaudio/`
- **Purpose**: Provide ASIO-enabled audio I/O for ShredEngine DLL
- **License**: Custom permissive license (attributed in README.md)

## Build Requirements
- **Platform**: Windows (MSVC compiler)
- **Dependencies**: Windows SDK, ASIO SDK (optional for full ASIO)
- **Output**: `libtard/portaudio/build/Release/portaudio.dll` and headers

## Integration Steps
1. Build PortAudio with CMake: `cmake .. -DCMAKE_BUILD_TYPE=Release -DPA_BUILD_SHARED=ON`
2. Copy `portaudio.dll` and `portaudio.lib` to `libtard/`
3. Include headers in ShredEngine CMakeLists.txt
4. Link library in ShredEngine build

## Accreditation
PortAudio is an open-source cross-platform audio API. Used under its license terms with full attribution.</content>
<parameter name="filePath">libtard/README_PortAudio.md