# Building ShredEngine Library for Linux

## Overview

This guide describes how to build the C++ ShredEngine library on Linux for the Rizz DJ software.

## Prerequisites

- GCC compiler (g++) with C++17 support
- Make
- wget (for downloading PortAudio)
- bash shell

## Build Steps

### 1. Download and Build PortAudio

The ShredEngine library depends on PortAudio for audio I/O. We'll build it from source.

```bash
cd /home/delta/.openclaw/workspace/src/rizz
mkdir -p external
cd external

# Download PortAudio
wget http://files.portaudio.com/archives/pa_stable_v190700_20210406.tgz
tar -xzf pa_stable_v190700_20210406.tgz
cd portaudio

# Configure and build
./configure --prefix=$(pwd)/build --disable-static --enable-shared
make -j4
make install
```

This installs PortAudio to `external/portaudio/build/` with:
- Headers: `build/include/portaudio.h`
- Library: `build/lib/libportaudio.so.2`

### 2. Build ShredEngine

The ShredEngine uses a Makefile that compiles all source files into a shared library.

```bash
cd /home/delta/.openclaw/workspace/src/rizz/src/RizzAudioEngine

# Clean any previous builds
make clean

# Build the library
make

# Install to bin directory
make install
```

This creates:
- `bin/libShredEngine.so` - The shared library
- `bin/libportaudio.so.2` - PortAudio dependency (copied for convenience)

The library is built with RPATH set to `$ORIGIN`, allowing it to find PortAudio in the same directory.

## Build Output

After successful build, you'll find:

```
bin/
├── libShredEngine.so          (160 KB) - Main audio engine library
└── libportaudio.so.2          (302 KB) - PortAudio dependency
```

## Testing

A test program is provided to verify the library works correctly:

```bash
cd /home/delta/.openclaw/workspace/src/rizz/bin
g++ -o test_shredengine ../src/RizzAudioEngine/test_shredengine.cpp -ldl
./test_shredengine
```

This tests:
- Engine initialization (test mode)
- File loading
- Playback controls (Play, Pause, Stop)
- Engine shutdown

## C# Interop

The C# P/Invoke interop layer is in `src/Audio/ShredEngineInterop.cs`. It automatically detects the platform and loads the appropriate library:

- **Linux**: `libShredEngine.so`
- **Windows**: `ShredEngine.dll`
- **macOS**: `libShredEngine.dylib`

The interop uses `dlopen` on Linux to load the library from the custom path.

## Library Dependencies

The built `libShredEngine.so` depends on:
- `libportaudio.so.2` (bundled)
- `libstdc++.so.6` (system)
- `libm.so.6` (system)
- `libgcc_s.so.1` (system)
- `libc.so.6` (system)
- `librt.so.6` (system)
- `libpthread.so.0` (system)

## Troubleshooting

### Library not found errors

If you get "library not found" errors:
1. Ensure `bin/libportaudio.so.2` is in the same directory as `libShredEngine.so`
2. Check file permissions: `ls -l bin/lib*.so`
3. Verify dependencies: `ldd bin/libShredEngine.so`

### PortAudio device issues

If audio devices aren't found:
1. Check PortAudio log output for device enumeration
2. Ensure ALSA/PulseAudio is running
3. Verify audio device permissions

### Build errors

If compilation fails:
1. Check GCC version: `g++ --version` (should support C++17)
2. Verify PortAudio headers are accessible: `ls external/portaudio/build/include/portaudio.h`
3. Clean and rebuild: `make clean && make`

## Architecture

The ShredEngine library consists of:

- **ShredEngine.cpp/h** - Main C API and audio callback
- **ScratchBuffer.cpp/h** - Audio buffering and playback
- **ClubMixer.cpp/h** - Mixing and effects processing
- **Selekta.cpp/h** - Audio device management
- **dr_mp3.h** - MP3 decoding (single-header library)

All components are compiled into a single shared library exposing a C API for P/Invoke interoperability.

## Performance

- **Latency**: Configurable (default 512 frames @ 44.1kHz = ~11.6ms)
- **Sample Rate**: 44.1 kHz
- **Channels**: Stereo (2 channels)
- **Format**: 32-bit float

## Next Steps

1. Build the C# WPF application (note: currently targets `net9.0-windows`)
2. Test with actual MP3/WAV files
3. Configure audio devices in the application
4. Implement cross-platform support in the WPF layer

## Version Information

- ShredEngine: 1.0.0
- PortAudio: 19.7.0
- C++ Standard: C++17
- Built: 2026-02-28
