# DJ Mix Master - User Manual

## Getting Started

### Installation
1. Download the latest release from GitHub
2. Extract the ZIP file to your desired location
3. Run `DJMixMaster.exe`

### First Launch
- The application will open with the main DJ interface
- Configure your audio settings on first run
- Load your first tracks to begin mixing

## Interface Overview

### Main Window Layout
```
┌─────────────────────────────────────────────────┐
│                    DECK 1                       │
│  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Album Art   │  │       Waveform          │  │
│  │ Canvas      │  │       Display           │  │
│  └─────────────┘  └─────────────────────────┘  │
│  [REW] [PLAY] [FF] [LOAD] [TEST] [CUE1] [CUE2] │
└─────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────┐
│                 MIXER CONTROLS                  │
│  VOLUME ↑↓    CROSSFADER ←→    VOLUME ↑↓       │
└─────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────┐
│                    DECK 2                       │
│  (Same layout as Deck 1)                        │
└─────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────┐
│                 PLAYLIST AREA                   │
│  Folder Tree          Track List                │
└─────────────────────────────────────────────────┘
```

### Key Components
- **Decks**: Two independent audio players
- **Waveforms**: Visual representation of audio tracks
- **Transport Controls**: Play, pause, cue, load functions
- **Mixer**: Volume faders and crossfader
- **Playlist**: Track management and organization

## Basic Operations

### Loading Tracks
1. Click the **LOAD** button on the desired deck
2. Navigate to your audio file (MP3, WAV supported)
3. The track title and waveform will appear
4. Track information displays below the waveform

### Playback Control
- **PLAY**: Start playback from current position
- **PAUSE**: Pause playback (resume with PLAY)
- **REW/FF**: Rewind/Fast forward through track
- **STOP**: Stop and reset to beginning

### Volume and Mixing
- **Deck Volume**: Adjust individual deck levels
- **Crossfader**: Mix between decks (-1 = Deck 1, 0 = Center, 1 = Deck 2)
- **Master Volume**: Overall output level (future feature)

### Cue Points
- **CUE 1/2/3**: Set and jump to cue points
- **TEST**: Preview audio at cue point without starting playback

## Advanced Features

### Waveform Navigation
- **Click**: Jump to position in track
- **Zoom**: Mouse wheel to zoom in/out
- **Position Marker**: Shows current playback location

### Beat Detection
- Automatic BPM detection on track load
- Visual beat markers on waveform (future feature)
- Sync mode for automatic beat matching (planned)

### Effects and Processing
- Built-in effects rack (under development)
- VST plugin support (planned)
- Real-time audio processing

## Audio Setup

### ASIO Configuration (Recommended)
1. Open audio settings (⚙ button)
2. Select your ASIO driver
3. Set buffer size (lower = less latency, higher = more stable)
4. Test playback to ensure no dropouts

### WASAPI Configuration
1. Select WASAPI output in settings
2. Choose your audio device
3. Adjust buffer settings as needed

### Troubleshooting Audio
- **No Sound**: Check device selection and volume levels
- **Dropouts**: Increase buffer size or check CPU usage
- **Distortion**: Verify sample rate compatibility
- **High Latency**: Switch to ASIO mode

## Keyboard Shortcuts

### Transport
- `Space`: Play/Pause Deck 1
- `Shift + Space`: Play/Pause Deck 2
- `Ctrl + Space`: Stop both decks

### Cue Points
- `Q`: Set Cue 1 on Deck 1
- `W`: Set Cue 2 on Deck 1
- `E`: Set Cue 3 on Deck 1
- `R`: Jump to Cue 1 on Deck 1
- `T`: Jump to Cue 2 on Deck 1
- `Y`: Jump to Cue 3 on Deck 1

- `A`: Set Cue 1 on Deck 2
- `S`: Set Cue 2 on Deck 2
- `D`: Set Cue 3 on Deck 2
- `F`: Jump to Cue 1 on Deck 2
- `G`: Jump to Cue 2 on Deck 2
- `H`: Jump to Cue 3 on Deck 2

### Mixer
- `Z/X`: Decrease/Increase Deck 1 volume
- `C/V`: Decrease/Increase Deck 2 volume
- `B/N`: Move crossfader left/right

### Navigation
- `Left/Right Arrows`: Fine position adjustment
- `Up/Down Arrows`: Coarse position adjustment
- `Page Up/Down`: Jump sections

## File Management

### Supported Formats
- MP3 (MPEG-1 Audio Layer III)
- WAV (Waveform Audio File Format)
- Future: FLAC, OGG, AAC

### Track Information
- Title and artist metadata
- Duration and file size
- Sample rate and bit depth
- BPM (when detected)

### Playlist Features
- Folder browsing
- Drag-and-drop loading
- Track search and filtering
- Auto-playlist generation (planned)

## Recording

### Session Recording (Planned)
- Record your mix to file
- Multiple output formats
- Real-time encoding
- Metadata tagging

### Track Export
- Export individual tracks
- Apply effects during export
- Batch processing

## Customization

### Interface Themes
- Dark theme (default)
- Neon color schemes
- Custom color palettes (planned)

### Audio Preferences
- Default output device
- Buffer size settings
- Crossfader curve
- EQ preferences

### Keyboard Mapping
- Custom key assignments
- MIDI controller support (planned)
- Touchscreen gestures

## Performance Tips

### System Requirements
- Windows 10/11
- Quad-core CPU (i5 or better recommended)
- 8GB RAM minimum
- ASIO-compatible audio interface

### Optimization
- Close unnecessary applications
- Use SSD for audio files
- Keep buffer sizes reasonable
- Monitor CPU/GPU usage

### Best Practices
- Pre-load tracks before performance
- Use consistent sample rates
- Keep project folder organized
- Regular backup of settings

## Troubleshooting

### Common Issues

#### Application Won't Start
- Check .NET runtime installation
- Verify audio drivers
- Run as administrator
- Check Windows Event Viewer for errors

#### Audio Problems
- Reset audio settings to defaults
- Try different output modes (WASAPI → DirectSound)
- Update audio drivers
- Check sample rate compatibility

#### UI Issues
- Reset window layout (planned)
- Clear application cache
- Update graphics drivers
- Check DPI scaling settings

#### Performance Issues
- Lower visual quality settings
- Reduce waveform resolution
- Close background applications
- Check for overheating

### Getting Help
- Check the troubleshooting section in docs
- Search GitHub issues
- Join the community Discord
- Contact support

## Updates and Support

### Version Checking
- Automatic update notifications (planned)
- Manual version check in settings
- Release notes and changelogs

### Community Resources
- GitHub repository
- User forums
- Tutorial videos
- Live streams

### Professional Support
- Priority bug fixes
- Custom feature development
- Training and consultation

## Legal and Safety

### Important Notices
- This software is for entertainment purposes
- Use appropriate volume levels to protect hearing
- Comply with local copyright laws
- Backup your data regularly

### Warranty
- No warranty expressed or implied
- Use at your own risk
- Developer not responsible for data loss

### Acknowledgments
- Thanks to the open-source community
- Special thanks to beta testers
- Dedicated to DJs everywhere