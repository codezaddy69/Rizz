# DJ Mix Master - Developer Documentation

## Overview

DJ Mix Master is a professional-grade DJ mixing application built with a hybrid C++/C# architecture. The application provides real-time audio processing, waveform visualization, and a comprehensive DJ interface for live performance and production.

## Architecture

### Core Components

#### RizzApplication (C#)
- Main application entry point
- Manages audio engine lifecycle
- Handles logging and configuration
- Coordinates between UI and audio layers

#### AudioEngine (C#)
- High-level audio management
- Deck coordination and mixing
- ASIO/WASAPI output handling
- Integration with RizzAudioEngine

#### RizzAudioEngine (C++/C#)
- Low-level audio processing in C++
- Real-time mixing and effects
- File I/O and decoding
- High-performance algorithms

#### MainWindow (WPF)
- Primary user interface
- Two-deck DJ layout
- Waveform visualization
- Control binding and event handling

### Audio Pipeline

```
File Input → Decoding → Resampling → Buffering → Processing → Mixing → Output
    ↓           ↓           ↓           ↓           ↓           ↓           ↓
   MP3/WAV   dr_mp3/WAV   WDL Resample ScratchBuf  ShredEngine ClubMixer  ASIO
```

## Development Environment

### Prerequisites
- .NET 9.0 SDK
- CMake 3.28+
- Visual Studio 2022 (with C++ workload)
- ASIO SDK (optional, for development)

### Building the Project

#### Full Build
```bash
# Build C# components
dotnet build DJMixMaster.csproj

# Build C++ engine
cd RizzAudioEngine
cmake -B build -S .
cmake --build build --config Release
```

#### Development Build
```bash
# Quick rebuild
dotnet build --configuration Debug

# With native library copy
dotnet build && copy RizzAudioEngine\build\libShredEngine.dll bin\Debug\net9.0\
```

### Running Tests
```bash
# Unit tests
dotnet test

# Integration tests
dotnet run --project DJMixMaster.csproj -- test-mode
```

## Code Organization

### Source Structure
```
src/
├── Audio/                    # Audio processing
│   ├── CoreDjEngine.cs      # Engine interface
│   ├── RizzAudioEngine.cs   # C++ wrapper
│   ├── AudioEngine.cs       # Main engine
│   ├── Deck.cs              # Deck management
│   └── ...
├── UI/                      # User interface
│   ├── Handlers/            # Event handlers
│   └── ...
├── Visualization/           # Graphics rendering
│   ├── Waveforms/           # Waveform display
│   └── ...
├── Controls/                # Custom controls
├── Converters/              # Data converters
└── MainWindow.xaml          # Main UI
```

### Key Classes

#### RizzApplication
```csharp
public class RizzApplication
{
    public void Initialize() { /* Setup audio pipeline */ }
    public void Shutdown() { /* Cleanup resources */ }
    public ILogger Logger { get; }
}
```

#### AudioEngine
```csharp
public class AudioEngine : IAudioEngine
{
    public void LoadTrack(int deck, string path) { }
    public void Play(int deck) { }
    public void SetVolume(int deck, float volume) { }
    public void SetCrossfader(float position) { }
}
```

#### Deck
```csharp
public class Deck
{
    public void Load(string path) { }
    public void Play() { }
    public void Pause() { }
    public float Position { get; set; }
    public AudioFileProperties Properties { get; }
}
```

## Audio Processing Details

### File Loading
- Supports MP3 (via dr_mp3) and WAV
- Automatic format detection
- Metadata extraction (title, artist, duration)

### Real-time Processing
- 44100Hz sample rate
- 32-bit float precision
- Low-latency buffering
- Thread-safe operations

### Effects Pipeline
- Beat detection algorithms
- Time-stretching (future)
- Pitch shifting (future)
- VST plugin hosting (planned)

## UI Development

### WPF Architecture
- MVVM pattern (partial implementation)
- Custom controls for DJ interface
- Real-time data binding
- Neon styling with effects

### Key UI Components
- **WaveformVisualizer**: Real-time waveform rendering
- **DeckCanvas**: Album art and visual feedback
- **NeonButton**: Styled transport controls
- **NeonVerticalSlider**: Volume controls
- **NeonCrossfader**: Mixing control

### Event Handling
```csharp
// Transport controls
btnLeftPlay.Click += (s, e) => audioEngine.Play(0);
btnRightLoad.Click += (s, e) => LoadTrackDialog(1);

// Slider binding
sliderLeftVolume.ValueChanged += (s, e) =>
    audioEngine.SetVolume(0, (float)sliderLeftVolume.Value);
```

## C++ Engine Integration

### RizzAudioEngine Structure
```
RizzAudioEngine/
├── ClubMixer.h/cpp        # Main mixing logic
├── ShredEngine.h/cpp      # Effects processing
├── ScratchBuffer.h/cpp    # Audio buffering
├── Selekta.h/cpp          # Track selection
└── CMakeLists.txt         # Build configuration
```

### C# Interop
```csharp
[DllImport("libShredEngine.dll")]
private static extern IntPtr CreateShredEngine();

public class RizzAudioEngine
{
    private IntPtr engineHandle;

    public RizzAudioEngine()
    {
        engineHandle = CreateShredEngine();
    }
}
```

### Build Process
```cmake
# CMakeLists.txt
cmake_minimum_required(VERSION 3.28)
project(ShredEngine)

add_library(ShredEngine SHARED
    ClubMixer.cpp
    ShredEngine.cpp
    ScratchBuffer.cpp
    Selekta.cpp
)

target_include_directories(ShredEngine PUBLIC include)
```

## Testing Strategy

### Unit Tests
- Audio processing algorithms
- UI component behavior
- File I/O operations

### Integration Tests
- Full audio pipeline
- UI interaction with engine
- Performance benchmarks

### Manual Testing
- Audio quality assessment
- Latency measurements
- UI responsiveness

## Performance Optimization

### Audio Threading
- Dedicated audio processing thread
- Lock-free data structures
- Minimal GC pressure

### Memory Management
- Pooled audio buffers
- Efficient file streaming
- Resource cleanup

### Profiling
- Use Visual Studio profiler
- Monitor audio callback timing
- Memory usage analysis

## Deployment

### Packaging
```xml
<!-- DJMixMaster.csproj -->
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

### Distribution
- Single executable with embedded runtime
- Separate C++ DLL for audio processing
- Configuration files for user settings

## Troubleshooting

### Common Issues

#### Build Failures
- Ensure .NET 9.0 SDK is installed
- Check CMake version compatibility
- Verify Visual Studio workloads

#### Runtime Errors
- ASIO driver installation
- Audio device permissions
- File format support

#### Performance Issues
- Disable debug logging in release
- Use release builds for C++
- Monitor thread priorities

## Contributing

### Code Standards
- C# naming conventions
- C++ Google style guide
- Comprehensive documentation
- Unit test coverage

### Pull Request Process
1. Create feature branch
2. Implement with tests
3. Update documentation
4. Submit PR with description

### Issue Reporting
- Use GitHub issues
- Include system specs
- Attach log files
- Describe reproduction steps

## Future Development

### Planned Features
- VST3 plugin support
- Multi-track recording
- Advanced effects rack
- Cloud synchronization
- Mobile remote control

### Architecture Improvements
- Complete MVVM implementation
- Plugin architecture
- Modular effects system
- Cross-platform support