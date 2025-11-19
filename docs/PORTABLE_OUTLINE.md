# Portable DJMixMaster Implementation Outline

## Executive Summary
This outline details the steps to make DJMixMaster fully portable, runnable from any folder without installation. Focus on bundling dependencies, self-contained .NET publishing, and relative path handling.

## Phase 1: Dependency Setup and Bundling
**Objective**: Collect all required libraries in libtard folder for portability.

**Steps**:
1. Clone PortAudio to `libtard/portaudio/`
2. Build PortAudio DLL for Windows
3. Build ShredEngine.dll linking to PortAudio
4. Copy all DLLs to `libtard/bin/`
5. Update CMakeLists.txt to reference local paths

**3 Improvements**:
1. **Version Pinning**: Use specific Git commit for PortAudio to ensure consistency
2. **Compression**: Compress DLLs with UPX to reduce size
3. **Dependency Scanner**: Auto-detect and bundle required VCRuntime DLLs

## Phase 2: .NET Self-Contained Publishing
**Objective**: Create standalone executable with embedded runtime.

**Steps**:
1. Modify DJMixMaster.csproj for self-contained publishing
2. Set runtime to `win-x64`
3. Configure output directory to `bin/Portable/`
4. Copy ShredEngine.dll and PortAudio DLLs to output
5. Test on clean Windows environment

**3 Improvements**:
1. **Trimming**: Enable aggressive trimming to remove unused assemblies
2. **Single-File**: Publish as single EXE for maximum portability
3. **ReadyToRun**: Use R2R compilation for faster startup

## Phase 3: Configuration and Path Handling
**Objective**: Ensure all paths are relative and configs are portable.

**Steps**:
1. Update appsettings.json to use relative paths (e.g., `./assets/`)
2. Modify ShredEngineInterop.cs to load DLLs from exe directory
3. Implement audio file loading from relative paths
4. Add portable mode detection and warnings

**3 Improvements**:
1. **Auto-Discovery**: Scan exe directory for audio files on startup
2. **Config Migration**: Import settings from user documents if available
3. **Sandbox Mode**: Detect limited permissions and adjust features

## Phase 4: Testing and Distribution
**Objective**: Validate portability and prepare release package.

**Steps**:
1. Test on multiple Windows versions without admin rights
2. Create ZIP archive of portable folder
3. Write setup instructions in README
4. Monitor for missing dependencies in logs

**3 Improvements**:
1. **Health Check**: Built-in diagnostic on startup to verify components
2. **Update Mechanism**: Self-updating with rollback capability
3. **Telemetry Opt-In**: Anonymous usage stats for compatibility tracking

## Component-Specific Improvements

### ShredEngine DLL
1. **Lazy Loading**: Load DLL only when audio features activated
2. **Error Isolation**: Sandbox DLL failures to prevent app crashes
3. **Memory Mapping**: Use memory-mapped files for large audio buffers

### PortAudio Integration
1. **Device Caching**: Cache device enumeration to avoid repeated queries
2. **Buffer Optimization**: Auto-tune buffer sizes based on system performance
3. **Fallback Chain**: Implement WaveOut fallback despite no-fallback policy for testing

### .NET Runtime
1. **GC Tuning**: Optimize garbage collection for real-time audio
2. **Thread Affinity**: Pin audio threads to specific CPU cores
3. **Native AOT**: Explore ahead-of-time compilation for reduced load times

## Folder Structure (Final)
```
DJMixMaster/
├── DJMixMaster.exe (self-contained)
├── ShredEngine.dll
├── portaudio.dll
├── appsettings.json
├── assets/
│   ├── audio/
│   └── gfx/
├── libtard/ (build-time only)
│   ├── portaudio/
│   └── bin/
└── docs/
```

## Build Commands
```bash
# Build PortAudio
cd libtard/portaudio
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DPA_BUILD_SHARED=ON
make

# Build ShredEngine
cd ../../../src/RizzAudioEngine
mkdir build && cd build
cmake .. -DPORTAUDIO_DIR=../../../libtard/portaudio
make

# Publish .NET
cd ../../..
dotnet publish -c Release --self-contained --runtime win-x64 --output bin/Portable
cp libtard/bin/*.dll bin/Portable/
```

## Success Criteria
- [ ] Runs on clean Windows machine without installation
- [ ] All DLLs load from relative paths
- [ ] Audio playback functional with ASIO or fallback
- [ ] Settings persist in exe directory
- [ ] ZIP size under 50MB

This outline provides a complete roadmap for portable DJMixMaster implementation.</content>
<parameter name="filePath">docs/PORTABLE_OUTLINE.md