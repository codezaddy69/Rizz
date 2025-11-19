# DJMixMaster

A professional DJ mixing application built with C#/.NET WPF, inspired by Mixxx. Features dual-deck audio playback, ASIO low-latency support, crossfader controls, and extensible architecture for future VST integration.

## Architecture
### Core Components
- **RizzAudioEngine**: Main audio engine coordinating playback
- **ScratchBuffer**: Per-deck buffer management (inspired by Mixxx EngineBuffer)
- **BeatSource**: Audio file decoding (inspired by Mixxx SoundSource)
- **ClubMixer**: Output mixing with crossfader (inspired by Mixxx EngineMixer)
- **Selekta**: ASIO device management (inspired by Mixxx SoundManager)
- **ShredEngine**: C++ DLL interface (future implementation)

### UI Components
- **MainWindow**: Primary interface with deck controls
- **DeckEventHandler**: Handles user interactions
- **AudioSettingsWindow**: Configuration dialog (planned)

## Features Implemented
### Audio Engine
- CSCore/NAudio integration for playback
- ASIO device enumeration and selection
- Custom MixingSampleProvider with logging
- SineWaveProvider for test tones
- Command-line options: `--test-tone`, `--file`, `--verbose`, `--asio-device`, `--sample-rate`

### UI Features
- Dual-deck interface with play/pause/load buttons
- Crossfader and volume sliders
- Position sliders with timer updates
- Settings button (placeholder)

### Debugging & Testing
- Comprehensive logging to console and `debug.log`
- File-based logging for live debugging
- Command-line auto-load and playback testing
- ASIO systray icon verification

## Code Structure
```
src/
├── Audio/
│   ├── RizzAudioEngine.cs (main engine)
│   ├── ScratchBuffer.cs (deck buffer)
│   ├── BeatSource.cs (decoder)
│   ├── ClubMixer.cs (mixer)
│   ├── Selekta.cs (device manager)
│   ├── SineWaveProvider.cs (test tone)
│   ├── ShredEngineInterop.cs (C++ interface)
│   └── MixingSampleProvider.cs (custom mixer)
├── UI/Handlers/
│   └── DeckEventHandler.cs
├── MainWindow.xaml/cs
├── App.xaml/cs
└── AudioSettingsWindow.xaml/cs (planned)

libtard/
├── portaudio/ (Git cloned)
└── README_PortAudio.md

docs/
├── SESSION_SUMMARY.md
├── PORTABLE_OUTLINE.md
└── README_PortAudio.md
```

## Build & Run
### Prerequisites
- .NET 9.0 SDK
- Windows with ASIO drivers (e.g., ASIO4ALL)

### Build Commands
```bash
# Build C# project
dotnet build DJMixMaster.csproj

# Run with options
dotnet run --project DJMixMaster.csproj --test-tone
dotnet run --project DJMixMaster.csproj --file "path/to/song.wav"
```

### Command-Line Options
- `--test-tone`: Play 440Hz sine wave for 5 seconds
- `--file <path>`: Load and auto-play specified file
- `--verbose`: Enable detailed logging
- `--asio-device <name>`: Specify ASIO device
- `--sample-rate <rate>`: Set sample rate

## Current Status
### Working
- Application launches and initializes
- ASIO device detection and systray icon
- UI controls and event handling
- Logging system with file output
- Command-line parsing and auto-load

### In Progress
- Audio playback (ASIO initialized but no sound output)
- C++ ShredEngine DLL (planned)
- Portable build (outlined but not implemented)

### Known Issues
- Test tone generates but no audible output
- Playback pipeline may need buffer synchronization
- ASIO device selection not fully tested

## Development Roadmap
### Phase 1: Playback Fix (Current)
- Debug why ASIO outputs silence despite initialization
- Verify buffer sizes and sample rates
- Test with different ASIO drivers

### Phase 2: C++ Integration
- Build ShredEngine.dll with PortAudio
- Implement P/Invoke interface
- Migrate playback to C++ for performance

### Phase 3: Full Features
- VST support
- Recording functionality
- Advanced UI (waveforms, effects)

### Phase 4: Polish & Release
- Portable packaging
- Comprehensive testing
- Documentation completion

## Technical Details
### Audio Pipeline
1. File loaded via BeatSource (NAudio decoders)
2. Buffered in ScratchBuffer
3. Mixed in ClubMixer
4. Output via Selekta (ASIO/WaveOut)

### Logging System
- Microsoft.Extensions.Logging for structured logs
- File logging to `debug.log` for persistence
- Console output for immediate feedback

### Dependencies
- NAudio: Audio I/O and processing
- CSCore: Alternative audio library (planned)
- PortAudio: C++ audio backend (planned)

## Credits & Inspiration
- **Mixxx**: Open-source DJ software providing architectural inspiration
- **PortAudio**: Cross-platform audio library
- **NAudio**: .NET audio processing library

## License
See LICENSE file. PortAudio licensed under custom permissive terms with attribution.</content>
<parameter name="filePath">README.md