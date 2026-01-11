# DJ Mix Master

A professional DJ mixing software built with C# WPF and NAudio, featuring low-latency ASIO audio output, real-time waveform visualization, beat detection, and a hybrid C++/C# audio engine.

## Features

### Audio Engine
- **NAudio Integration**: High-performance audio playback with ASIO support for sub-10ms latency
- **RizzAudioEngine**: Hybrid C++/C# engine for advanced audio processing
- **Permanent Audio Pipeline**: Continuous streaming architecture for uninterrupted playback
- **Beat Detection**: Real-time BPM analysis and cue point detection
- **Resampling**: High-quality audio resampling for format compatibility

### User Interface
- **Two-Deck Layout**: Professional DJ interface with dual decks
- **Waveform Visualization**: Real-time waveform rendering with zoom and navigation
- **Neon Styling**: Custom WPF controls with glow effects and dark theme
- **Transport Controls**: Play, pause, cue, load buttons with hotkeys
- **Mixer Controls**: Volume faders, crossfader, and EQ controls
- **Track Management**: File loading, track info display, playlist integration

### Audio Processing
- **Deck Management**: Independent control of two audio decks
- **Mixing**: Crossfading, volume control, and audio routing
- **Effects**: Placeholder for VST plugin integration
- **Recording**: Future support for session recording

## Architecture

### Project Structure
```
DJMixMaster/
├── src/                          # Source code
│   ├── Audio/                    # Audio engine components
│   │   ├── Deck.cs              # Audio deck management
│   │   ├── AudioEngine.cs       # Main audio engine
│   │   ├── RizzAudioEngine.cs   # C# wrapper for C++ engine
│   │   └── ...
│   ├── Controls/                # Custom WPF controls
│   ├── Converters/              # Value converters
│   ├── UI/                      # UI handlers and utilities
│   ├── Visualization/           # Waveform rendering
│   ├── MainWindow.xaml          # Main DJ interface
│   ├── App.xaml                 # Application entry point
│   └── Program.cs               # Console entry point (alternative)
├── RizzAudioEngine/             # C++ audio processing
│   ├── ClubMixer.cpp/h         # Main mixing engine
│   ├── ShredEngine.cpp/h       # Audio shredding effects
│   ├── ScratchBuffer.cpp/h     # Audio buffering
│   └── ...
├── docs/                        # Documentation
├── assets/                      # Audio samples and graphics
└── settings/                    # Configuration files
```

### Audio Pipeline
1. **File Loading**: AudioFileReader loads WAV/MP3 files
2. **Decoding**: dr_mp3 for MP3, native for WAV
3. **Resampling**: WDL resampling to 44100Hz
4. **Buffering**: ScratchBuffer for real-time processing
5. **Mixing**: ClubMixer combines deck outputs
6. **Output**: ASIO/WASAPI to audio device

## Setup and Installation

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- ASIO drivers (recommended for low latency)
- Windows 10/11

### Building
```bash
# Clone repository
git clone <repository-url>
cd DJMixMaster

# Restore packages
dotnet restore

# Build project
dotnet build DJMixMaster.csproj

# Run application
dotnet run --project DJMixMaster.csproj
```

### Audio Setup
1. Install ASIO drivers for your audio interface
2. Configure audio settings in the app
3. Select ASIO output device for best performance

## Usage

### Basic Operation
1. Launch the application
2. Load tracks using the LOAD buttons
3. Use PLAY buttons to start playback
4. Adjust volume with sliders
5. Use crossfader for mixing between decks

### Keyboard Shortcuts
- Space: Play/Pause deck 1
- Shift+Space: Play/Pause deck 2
- Q/W: Cue points deck 1
- E/R: Cue points deck 2
- Arrow keys: Navigate waveforms

### Configuration
- Audio settings in `settings/audio.json`
- UI customization in XAML files
- Engine parameters in RizzAudioEngine

## Development

### Adding Features
- Audio features: Extend RizzAudioEngine or AudioEngine.cs
- UI features: Modify MainWindow.xaml and code-behind
- Effects: Implement in ShredEngine.cpp

### Testing
- Unit tests in `tests/` directory
- Integration tests for audio pipeline
- Manual testing with various audio formats

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make changes with proper documentation
4. Submit a pull request

## API Reference

### RizzApplication
Main application class managing audio lifecycle.

```csharp
var app = new RizzApplication(logger);
app.Initialize();
// Audio processing active
app.Shutdown();
```

### AudioEngine
Core audio processing engine.

```csharp
var engine = new AudioEngine();
engine.LoadTrack(deck, filePath);
engine.Play(deck);
engine.SetVolume(deck, level);
```

### Deck
Individual audio deck management.

```csharp
var deck = new Deck();
deck.Load(filePath);
deck.Play();
deck.SetPosition(position);
```

## Troubleshooting

### Audio Issues
- **No sound**: Check audio device selection and drivers
- **Latency**: Switch to ASIO output mode
- **Distortion**: Verify sample rate compatibility

### Build Issues
- **Missing dependencies**: Run `dotnet restore`
- **Platform issues**: Ensure Windows target framework
- **C++ compilation**: Check CMake configuration

### Performance
- Use ASIO for lowest latency
- Close other audio applications
- Monitor CPU usage in task manager

## Roadmap

### Short Term
- [ ] Complete VST plugin integration
- [ ] Add recording functionality
- [ ] Implement advanced effects

### Long Term
- [ ] Multi-platform support (macOS, Linux)
- [ ] Cloud sync for tracks and settings
- [ ] Mobile companion app

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- NAudio library for .NET audio processing
- dr_mp3 for MP3 decoding
- WDL for resampling algorithms
- WPF for UI framework

## Contact

For questions or contributions, please open an issue on GitHub.