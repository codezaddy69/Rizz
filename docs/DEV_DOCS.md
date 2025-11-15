# DJMixMaster Development Documentation

## Table of Contents
1. [Tech Stack](#tech-stack)
2. [Development Environment Setup](#development-environment-setup)
3. [Build and Run Instructions](#build-and-run-instructions)
4. [Project Structure](#project-structure)
5. [Feature List](#feature-list)
6. [API Reference](#api-reference)
7. [Development Guidelines](#development-guidelines)
8. [Testing](#testing)
9. [Deployment](#deployment)

## Tech Stack

### Core Technologies
- **Language**: C# 12.0 (.NET 9.0)
- **UI Framework**: Windows Presentation Foundation (WPF)
- **Audio Engine**: NAudio (.NET managed library)
  - Features: Playback, mixing, crossfading, volume control
  - MIT License
- **Build System**: .NET SDK (dotnet CLI)

### Dependencies
```xml
<!-- Core .NET -->
<TargetFramework>net9.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>

<!-- Audio Libraries -->
<PackageReference Include="NAudio" Version="2.2.1" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
```

### Development Tools
- **IDE**: Visual Studio 2022 (recommended) or VS Code with C# extensions
- **Version Control**: Git
- **Build Tool**: .NET CLI (dotnet)
- **Package Manager**: NuGet

### Target Platform
- **OS**: Windows 10/11 (64-bit)
- **Architecture**: x64
- **Audio**: Windows Audio Session API (WASAPI)

## Development Environment Setup

### Prerequisites
1. **Windows 10/11** (64-bit)
2. **.NET 9.0 SDK** - Download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0)
3. **Visual Studio 2022** (Community edition is free)
   - Workloads: .NET desktop development, Desktop development with C++
4. **Git** for version control

### Environment Setup Steps

#### 1. Clone Repository
```bash
git clone <repository-url>
cd djmixmaster
```

#### 2. Restore Dependencies
```bash
dotnet restore
```

#### 3. Build Solution
```bash
dotnet build
```

#### 4. Run Application
```bash
dotnet run
```

**Build Status**: ✅ Application builds successfully with no warnings on .NET 9.0 Windows (NAudio implementation).

### Optional: JUCE Development Setup
For VST plugin development, you'll need:
1. **JUCE Framework** - Download from [JUCE.com](https://juce.com/)
2. **Visual Studio Build Tools** for C++ compilation
3. **CMake** for building native components

## Build and Run Instructions

### Standard Build
```bash
# Clean and build
dotnet clean
dotnet build

# Run in debug mode
dotnet run --configuration Debug

# Run in release mode
dotnet run --configuration Release
```

### Build Configurations
- **Debug**: Full debugging symbols, no optimizations
- **Release**: Optimized, no debugging symbols

### Build Artifacts
- **Output Directory**: `bin/Debug/net9.0-windows/` or `bin/Release/net9.0-windows/`
- **Main Executable**: `DJMixMaster.exe`
- **Dependencies**: Copied to output directory automatically

### Troubleshooting Build Issues

#### Common Issues
1. **Missing .NET SDK**
   ```
   Error: .NET SDK not found
   Solution: Install .NET 9.0 SDK from Microsoft
   ```

2. **Native Library Missing**
   ```
   Error: juce_dll.dll not found
   Solution: Build JUCE native library (see JUCE setup)
   ```

3. **Platform Target Mismatch**
   ```
   Error: Platform not supported
   Solution: Ensure x64 platform target
   ```

## Project Structure

```
DJMixMaster/
├── Audio/                          # Audio processing components
│   ├── DeckPlayer.cs              # NAudio-based deck implementation
│   ├── DeckPlayerAudioProcessing.cs # Audio file loading & processing
│   ├── DeckPlayerNavigation.cs    # Playback navigation & speed control
│   ├── DeckPlayerCuePoints.cs     # Cue point management
│   ├── AudioEngine.cs             # JUCE-based audio engine interface
│   ├── BeatDetector.cs            # BPM detection algorithm
│   └── Providers/                 # Audio effect providers
│       └── DeckVolumeProvider.cs
├── Controls/                      # Custom WPF controls
│   ├── Fader.cs                   # Volume fader logic
│   └── FaderControl.cs            # Fader UI control
├── Converters/                    # WPF value converters
│   └── BrushOpacityConverter.cs
├── Native/                        # Native interop layer
│   └── JUCE/
│       ├── JuceAudioEngine.cs     # JUCE audio engine wrapper
│       └── JuceNative.cs          # P/Invoke declarations
├── Visualization/                 # UI visualization components
│   └── WaveformVisualizer.cs      # Real-time waveform display
├── MainWindow.xaml/.cs            # Main application window
├── App.xaml/.cs                   # Application entry point
├── AGENTS.md                      # Agent development guidelines
├── SYSTEM_ANALYSIS.md             # Technical architecture docs
├── ANALYSIS_SUMMARY.md            # Executive summary
├── Documentation.md               # General documentation
├── TODO.md                        # Development tasks
└── DJMixMaster.csproj             # Project configuration
```

## Feature List

### ✅ Implemented Features

#### Core Audio Playback
- [x] **File Loading**: MP3, WAV, AIFF support
- [x] **Transport Controls**: Play, pause, stop, seek
- [x] **Navigation**: Fast-forward, rewind, speed control (0.5x - 2.0x)
- [x] **Position Tracking**: Real-time position updates (50ms precision)
- [x] **Volume Control**: Per-deck volume adjustment

#### User Interface
- [x] **Two-Deck Layout**: Professional DJ interface
- [x] **Neon Styling**: Custom WPF controls with glow effects
- [x] **Waveform Visualization**: Real-time scrolling waveform
- [x] **Zoom & Navigation**: Mouse wheel zoom, auto-scroll
- [x] **Mixer Controls**: Volume faders, crossfader UI
- [x] **Transport Buttons**: Play/pause/stop/load controls
- [x] **Hot Cue System**: 3 cue points per deck with visual indicators

#### Audio Analysis
- [x] **Beat Detection**: Energy-based algorithm
- [x] **BPM Calculation**: Automatic tempo detection (60-200 BPM)
- [x] **Beat Grid Visualization**: Yellow markers on waveform
- [x] **Waveform Generation**: 1000-point amplitude data

#### Architecture
- [x] **Dependency Injection**: Microsoft.Extensions.Logging
- [x] **Interface-Based Design**: IAudioEngine abstraction
- [x] **Event-Driven Communication**: Loose coupling between components
- [x] **Resource Management**: IDisposable pattern implementation

### 🚧 Partially Implemented Features

#### VST Plugin System
- [x] **Framework Architecture**: P/Invoke declarations for VST loading
- [x] **Plugin Interface**: Parameter control methods defined
- [ ] **Plugin Loading**: Implementation incomplete
- [ ] **Parameter Mapping**: GUI controls missing
- [ ] **Plugin Browser**: UI not implemented

#### Crossfader
- [x] **UI Component**: Horizontal slider with neon styling
- [x] **Position Tracking**: Value change events implemented
- [ ] **Audio Mixing**: Backend crossfader logic missing
- [ ] **Blend Algorithms**: Linear/crossfader curves not implemented

#### Effects System
- [x] **UI Framework**: FX buttons in mixer section
- [x] **Architecture**: Extensible effects chain design
- [ ] **Effect Processors**: No concrete effect implementations
- [ ] **Parameter Controls**: No effect parameter UI

### ❌ Planned/Missing Features

#### Advanced DJ Features
- [ ] **Beat Matching**: Automatic tempo sync between decks
- [ ] **Loop Regions**: Loop points and regions
- [ ] **Sampler**: Sample playback decks
- [ ] **Recording**: Mix recording to file
- [ ] **Key Detection**: Musical key analysis
- [ ] **Harmonic Mixing**: Key compatibility visualization

#### User Experience
- [ ] **Playlist Management**: Load/save playlists
- [ ] **File Browser**: Directory navigation
- [ ] **Keyboard Shortcuts**: Hotkey support
- [ ] **Settings Persistence**: Save user preferences
- [ ] **Themes**: Additional UI themes
- [ ] **Help System**: User documentation

#### Audio Processing
- [ ] **EQ Controls**: 3-band EQ per deck
- [ ] **Filters**: High-pass/low-pass filters
- [ ] **Reverb/Delay**: Time-based effects
- [ ] **Distortion**: Wave shaping effects
- [ ] **Master Output**: Final mix processing

#### Integration
- [ ] **MIDI Controller Support**: Hardware integration
- [ ] **OSC Protocol**: External control support
- [ ] **Streaming**: Icecast/Shoutcast output
- [ ] **Database**: Track metadata storage

## API Reference

### IAudioEngine Interface
```csharp
public interface IAudioEngine : IDisposable
{
    // Transport Control
    void LoadFile(int deckNumber, string filePath);
    void Play(int deckNumber);
    void Pause(int deckNumber);
    void Stop(int deckNumber);
    void Seek(int deckNumber, double seconds);

    // State Queries
    double GetPosition(int deckNumber);
    double GetLength(int deckNumber);
    void SetVolume(int deckNumber, float volume);
    float GetVolume(int deckNumber);
    bool IsPlaying(int deckNumber);

    // Mixing
    void SetCrossfader(float position);
    float GetCrossfader();

    // Events
    event EventHandler<(int DeckNumber, double Position)> PlaybackPositionChanged;
    event EventHandler<(int DeckNumber, double[] BeatPositions, double BPM)> BeatGridUpdated;
}
```

### DeckPlayer Public API
```csharp
public class DeckPlayer : IDisposable
{
    // Constructor
    public DeckPlayer(ILogger logger, Fader faderLeft, Fader faderRight);

    // Playback Control
    public void LoadAudioFile(string filePath);
    public void Play();
    public void Pause();
    public void Stop();
    public void Seek(TimeSpan position);
    public void SetVolume(float volume);

    // Navigation
    public void FastForward(double seconds);
    public void Rewind(double seconds);
    public void SetSpeed(float speed);

    // Cue Points
    public void AddCuePoint(double position);
    public void JumpToCuePoint(int index);
    public double[] GetCuePoints();

    // Properties
    public double CurrentPosition { get; }
    public bool IsPlaying { get; }
    public float[] WaveformData { get; }
    public double GetTrackLength();

    // Events
    public event EventHandler<double> PlaybackPositionChanged;
}
```

### WaveformVisualizer API
```csharp
public class WaveformVisualizer : Canvas
{
    // Data Loading
    public void UpdateWaveform(float[] data, double trackLength);

    // Playback Tracking
    public void UpdatePlaybackPosition(double timeSeconds);

    // Beat Grid
    public void UpdateBeatGrid(double[] beatPositions);

    // Cue Points
    public void AddCuePoint(double timeSeconds);
}
```

### BeatDetector API
```csharp
public class BeatDetector
{
    // Analysis
    public void AnalyzeFile(string filePath);

    // Results
    public List<double> BeatPositions { get; }
    public double BPM { get; }

    // Queries
    public List<double> GetBeatPositionsInRange(double startTime, double endTime);
}
```

## Development Guidelines

### Code Style
- **Language**: C# 12.0 with nullable reference types enabled
- **Naming**: PascalCase for classes/methods/properties, camelCase for locals/parameters
- **Formatting**: 4 spaces indentation, one class per file
- **Imports**: System first, then third-party, then project namespaces

### Architecture Patterns
- **Dependency Injection**: Use Microsoft.Extensions.Logging
- **Interface Segregation**: Define clear component boundaries
- **Event-Driven**: Loose coupling between UI and audio components
- **Resource Management**: Implement IDisposable for unmanaged resources

### Error Handling
- **Try-Catch**: Wrap all public methods
- **Logging**: Use structured logging with context
- **User Feedback**: Show user-friendly error messages
- **Graceful Degradation**: Continue operation when possible

### Audio Processing Guidelines
- **Threading**: Keep audio operations off UI thread
- **Synchronization**: Use Dispatcher for UI updates
- **Resource Cleanup**: Dispose audio resources properly
- **Format Consistency**: Convert to 44.1kHz stereo internally

### UI Development
- **MVVM Pattern**: Separate UI logic from presentation
- **Data Binding**: Use WPF binding for reactive updates
- **Performance**: Virtualize large lists, minimize redraws
- **Accessibility**: Support keyboard navigation

## Testing

### Current State
- **Test Framework**: None configured
- **Coverage**: 0% (manual testing only)

### Recommended Testing Strategy

#### Unit Tests
```csharp
# Add to project file
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.6.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="Moq" Version="4.20.69" />
```

#### Test Categories
- **Audio Processing**: Beat detection, waveform generation
- **Navigation**: Position tracking, speed control
- **UI Logic**: Control interactions, data binding
- **Integration**: End-to-end audio playback

#### Test Structure
```
tests/
├── UnitTests/
│   ├── Audio/
│   │   ├── BeatDetectorTests.cs
│   │   └── WaveformTests.cs
│   └── UI/
│       └── ControlTests.cs
└── IntegrationTests/
    └── PlaybackTests.cs
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Deployment

### Build Configuration
```xml
<!-- Release configuration -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <Optimize>true</Optimize>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

### Packaging
```bash
# Publish as self-contained application
dotnet publish -c Release -r win-x64 --self-contained true

# Create installer (requires WiX Toolset)
# MSI installer with dependencies included
```

### Distribution
- **Target**: Windows 10/11 x64
- **Dependencies**: .NET runtime included in self-contained build
- **Size**: ~50MB (estimated with JUCE native library)
- **Installation**: ClickOnce or MSI installer

### Runtime Requirements
- **OS**: Windows 10 version 1903 or later
- **Memory**: 4GB RAM minimum, 8GB recommended
- **Storage**: 100MB free space
- **Audio**: Windows-compatible audio device

## Contributing

### Development Workflow
1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/new-feature`)
3. **Implement** changes with tests
4. **Test** thoroughly (manual + automated)
5. **Commit** with descriptive messages
6. **Push** to your fork
7. **Create** pull request

### Commit Message Format
```
type(scope): description

[optional body]

[optional footer]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

### Code Review Process
- **Automated**: Build checks, linting
- **Manual**: Architecture review, performance impact
- **Testing**: Unit tests pass, integration verified

## Support

### Documentation
- `AGENTS.md`: Agent development guidelines
- `SYSTEM_ANALYSIS.md`: Technical architecture
- `ANALYSIS_SUMMARY.md`: Executive summary

### Issue Tracking
- **Bug Reports**: Include steps to reproduce, system info
- **Feature Requests**: Describe use case, expected behavior
- **Performance Issues**: Include profiling data

### Community
- **Discussions**: GitHub Discussions for questions
- **Issues**: GitHub Issues for bugs/features
- **Wiki**: Project documentation and guides

---

**Last Updated**: November 10, 2025
**Version**: 0.1.0-alpha
**Maintainer**: DJMixMaster Development Team</content>
<parameter name="filePath">/mnt/c/users/rogue/code/djmixmaster/DEV_DOCS.md