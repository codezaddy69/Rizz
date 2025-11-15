# DJMixMaster Audio Pipeline Documentation

## Overview

DJMixMaster uses a sophisticated permanent audio pipeline architecture designed for low-latency, professional DJ mixing. The system maintains continuous audio streams to ASIO output devices, eliminating timing issues and ensuring stable playback.

### Architecture Principles
- **Permanent Pipeline**: Audio streams never disconnect, preventing ASIO timing errors
- **Sample Provider Chain**: Modular processing components for flexibility
- **ASIO-First Design**: Optimized for low-latency professional audio interfaces
- **Error Resilience**: Comprehensive error handling with graceful degradation

## Core Components

### SilentSampleProvider
**Location**: `src/Audio/SilentSampleProvider.cs`

**Purpose**: Provides continuous silence (zero samples) for unloaded decks or paused states.

**Key Features**:
- Generates infinite zero samples at specified format
- Maintains pipeline continuity when no audio is playing
- Thread-safe for multi-deck operation

**Usage**:
```csharp
var silent = new SilentSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
```

### PlayingSampleProvider
**Location**: `src/Audio/PlayingSampleProvider.cs`

**Purpose**: Wraps audio sources to provide silence when playback is paused.

**Key Features**:
- Checks `IsPlaying` state on each read
- Returns source audio or silence based on state
- Enables proper pause behavior without pipeline disruption

**Usage**:
```csharp
var playing = new PlayingSampleProvider(sourceProvider, () => deck.IsPlaying);
```

### LoopingSampleProvider
**Location**: `src/Audio/Deck.cs` (integrated class)

**Purpose**: Handles audio looping and source switching for continuous playback.

**Key Features**:
- Infinite looping with position reset
- Dynamic source switching without pipeline breakage
- Silent source support for unloaded decks
- Error handling for corrupted streams

**Usage**:
```csharp
var looping = new LoopingSampleProvider(audioSource, audioFileReader);
// Later: looping.SetSource(newSource, newReader);
```

### AudioEngine
**Location**: `src/Audio/AudioEngine.cs`

**Purpose**: Master audio coordinator managing ASIO output and deck mixing.

**Key Features**:
- ASIO4ALL integration with fallback to WaveOut
- Permanent mixer inputs for continuous streaming
- Crossfader and volume management
- Comprehensive error handling and logging

**Configuration**:
- Sample Rate: 44100 Hz
- Buffer Size: 128-512 samples (ASIO-dependent)
- Output Format: 32-bit float stereo

### Deck
**Location**: `src/Audio/Deck.cs`

**Purpose**: Individual deck management with format conversion and state tracking.

**Key Features**:
- File loading with metadata extraction (planned)
- Format conversion (mono→stereo, resampling)
- Volume and crossfader integration
- Position tracking and seeking

## Data Flow

```
Audio File → AudioFileReader → Format Conversion → Permanent Pipeline → Mixing → ASIO Output
     ↓              ↓              ↓              ↓              ↓              ↓
  MP3/WAV       TagLib#       WDL Resampling   Silent/Playing/   Volume/Cross   Low-Latency
  Loading       Artist/Title  (44.1kHz)       Looping Providers Fader Effects  Playback
```

### Processing Order
1. **File Loading**: AudioFileReader loads file with basic validation
2. **Metadata Extraction**: TagLib reads artist/title (planned)
3. **Format Conversion**:
   - Mono → Stereo (if needed)
   - Resampling to 44100 Hz (if needed)
4. **Pipeline Integration**: Source switched into permanent provider chain
5. **Mixing**: Combined with other decks via MixingSampleProvider
6. **Output**: ASIO streaming with low latency

## Configuration

### ASIO4ALL Settings
- **Buffer Size**: 256-512 samples for DJ use
- **Sample Rate**: Must be 44100 Hz (app requirement)
- **Always Resample**: Enable for rate mismatch handling
- **Hardware Buffer**: Enable if WavePCI device available

### Audio Formats
- **Supported**: MP3, WAV, AIFF, FLAC
- **Processing**: Automatic format detection and conversion
- **Quality**: 32-bit float internal processing

## Troubleshooting

### Common Issues

#### Distortion/Speed Problems
- **Cause**: Sample rate mismatch between app and ASIO
- **Solution**: Set ASIO4ALL to 44100 Hz, enable "Always Resample"
- **Prevention**: Verify ASIO control panel settings

#### Playback Stopping
- **Cause**: Pipeline disconnect (old issue, now resolved)
- **Solution**: Check for exceptions in logs
- **Prevention**: Permanent pipeline prevents this

#### High CPU Usage
- **Cause**: Small buffer sizes or inefficient resampling
- **Solution**: Increase ASIO buffer size, verify WDL resampler
- **Prevention**: Monitor performance with larger buffers

### Diagnostic Tools
- **Logging**: Comprehensive format and error logging
- **ASIO4ALL Control Panel**: Monitor device status and buffer usage
- **Performance Monitoring**: CPU usage and buffer underrun detection

## Development Notes

### Adding New SampleProviders
1. Implement `ISampleProvider` interface
2. Follow permanent pipeline pattern
3. Add comprehensive error handling
4. Include detailed logging

### Testing Procedures
1. Test with various audio formats
2. Verify ASIO vs WaveOut compatibility
3. Check buffer size impact on performance
4. Validate pause/play state transitions

### Future Enhancements
- VST plugin integration
- Advanced effects processing
- Multi-channel output support
- Hardware acceleration optimization

This audio pipeline provides the foundation for professional DJ software with reliable, low-latency performance.</content>
<parameter name="filePath">docs/AUDIO_PIPELINE.md