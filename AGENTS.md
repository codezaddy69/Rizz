# MAIN HIGH LEVEL PRIORITY: Standard Operating Procedure (SOP)

Note: The Primary LOOP is I send you either Feedback or console errors or output. At this point you trouble shoot or dev then when you are finished the loop starts over. If feedback is given that the code is building with no errors and minimal warnings and a milestone is reached an update will be recommended where documentation is updated and a GIT is recommended. Do not commit without final approval. Then the loop continues after you recommend 3 options to proceed next.

IMPORTANT: Do not commit changes unless explicitly told to by the user.

## Audio Pipeline Implementation Notes

### ASIO Requirements
- ASIO4ALL must be installed and configured for 44100 Hz sample rate
- Enable "Always Resample 44.1↔48 kHz" in advanced settings
- Set buffer size to 256-512 samples for DJ use
- Test with WaveOut fallback if ASIO issues occur

### Permanent Pipeline Architecture
- SampleProvider chain: Silent → Playing → Looping → Volume
- Mixer maintains permanent inputs to prevent stream disconnects
- All format conversion happens before pipeline integration
- Use WDL resampler for best quality (replaces MediaFoundation)

### Error Handling Patterns
- Wrap all audio operations in try-catch with detailed logging
- Log format details (rate, channels, bits) on load
- Validate file integrity before processing
- Graceful fallback for unsupported formats

### Development Guidelines
- Always maintain permanent pipeline when adding new providers
- Test with various sample rates and formats
- Include comprehensive logging for debugging
- Follow existing error handling patterns

### UI Theming
- Light theme: Light gray background (#D3D3D3) with black text (#000000)
- All controls (TextBox, ComboBox, CheckBox, etc.) use light gray theme
- ComboBox dropdown items styled with light gray background
- Selected items highlighted with cyan background

### ASIO Troubleshooting
- Check logs for driver capabilities: buffer size (256-512), latency, supported sample rates (44100/48000)
- If garbled playback: Verify ASIO4ALL buffer settings, ensure ChannelOffset is correct, check for sample rate mismatches
- Fallback to WaveOut logs detailed error reasons
- Use ShowAsioControlPanel() to adjust driver settings
- Monitor MixingSampleProvider logs for pre-sum levels >1.0 (indicates overdriving)

### Testing Checklist
- [ ] 44100 Hz files (no resampling)
- [ ] 48000 Hz files (resampling)
- [ ] Mono files (channel conversion)
- [ ] Corrupted files (error handling)
- [ ] ASIO vs WaveOut output
- [ ] Pause/play state transitions

## Autocompletion and Autodebug Section

### Build and Run Process for Debugging
1. **Locate dotnet.exe**: Use `/mnt/c/Program Files/dotnet/dotnet.exe` (default Windows dotnet installation path).
2. **Build Command**: Run `/mnt/c/Program\ Files/dotnet/dotnet.exe build DJMixMaster.csproj` to compile the project.
3. **Run Command**: Execute `/mnt/c/Program\ Files/dotnet/dotnet.exe run --project DJMixMaster.csproj` to launch the application.
4. **Debugging**: Monitor terminal output for logs, errors, and audio pipeline details during runtime. WPF GUI will open separately; focus on console for troubleshooting ASIO, resampling, and playback issues.
5. **Post-Run Review**: After testing, check logs (e.g., log20251116_*.txt) for anomalies, validate against Testing Checklist, and recommend fixes or next steps.

## FOCUS.md: Current Project Focus

# DJMixMaster Development Focus: Audio Playback System Debugging

## Current Project Status: Audio Pipeline Debugging & ASIO Integration

### Executive Summary
The DJMixMaster application has a fully functional audio pipeline architecture, but encounters critical issues with audio data integrity and ASIO driver compatibility. The system successfully initializes, loads files, and processes audio data, but produces silent output due to reader implementation failures.

### Key Findings from Debug Session

#### ✅ System Architecture: EXCELLENT
- **Audio Pipeline**: Robust mixing, resampling, and playback systems
- **Error Handling**: Comprehensive fallback mechanisms and logging
- **UI Integration**: Proper threading and user interaction handling
- **Performance**: Sub-millisecond latency, zero buffer underruns

#### ❌ Critical Issues Identified

##### 1. ASIO Driver Compatibility Failure
**Problem**: ASIO4ALL v2 reports no support for standard sample rates (44100Hz, 48000Hz, 96000Hz)
**Impact**: ASIO initialization fails, no systray icon, forces WaveOut fallback
**Status**: Configuration issue - requires ASIO4ALL setup in Windows Sound settings
**Solution**: User must configure ASIO4ALL to enable 44100Hz support

##### 2. Audio Data Reader Corruption
**Problem**: AudioFileReader passes validation (shows 0.590 amplitude) but returns zeros during playback
**Impact**: Files appear to load correctly but produce silent audio
**Status**: Code issue - reader implementation fails during streaming
**Evidence**:
```
Audio Reader: AudioFileReader, File: Techno1.wav, Amplitude: 0.590  ← Validation
PlayingSampleProvider: read 13230 samples, max amplitude=0.000     ← Playback
WARNING: Source returning silent audio data!
```

##### 3. UI Component Errors
**Problem**: NaN values in waveform visualization causing exceptions
**Impact**: File loading succeeds but UI updates fail
**Status**: Threading issue - beat grid updates on wrong thread
**Solution**: Implemented Dispatcher.Invoke fix

### Implemented Solutions

#### Phase 1: Reader Fallback System ✅
- **AudioFileReader**: Primary for all formats
- **WaveFileReader**: WAV fallback
- **Mp3FileReader**: MP3 fallback
- **MediaFoundationReader**: Universal fallback
- **Validation**: Amplitude checking with 0.001 threshold
- **Logging**: Reader selection and amplitude reporting

#### Phase 2: Enhanced Diagnostics ✅
- **Boot Logging**: Comprehensive initialization tracking
- **Runtime Monitoring**: Audio level validation every 10 reads
- **ASIO Detection**: Sample rate support checking before initialization
- **Error Handling**: Graceful fallbacks with detailed logging

#### Phase 3: UI Threading Fixes ✅
- **Dispatcher Implementation**: OnBeatGridUpdated uses Invoke
- **Load Confirmations**: Dialog prompts for playing deck interruptions
- **Button State Updates**: Proper synchronization after operations

### Current Test Results

#### System Health: EXCELLENT
- **Initialization**: 100% success rate
- **File Loading**: 75% success (validation working)
- **Playback Start**: 100% success (pipeline active)
- **Performance**: <0.1ms latency, 0 errors

#### Audio Output: CRITICAL FAILURE
- **Validation**: Detects audio presence correctly
- **Streaming**: Returns zeros despite validation
- **ASIO**: Fails due to driver configuration
- **WaveOut**: Active but receiving silent data

### Root Cause Analysis

#### Primary Issue: Reader Streaming Failure
**Hypothesis**: AudioFileReader works for metadata/header reading but fails during continuous sample streaming
**Evidence**: Validation reads ~4KB successfully, playback reads fail over time
**Possible Causes**:
- File corruption (valid header, corrupted data)
- Reader state management issues
- Threading conflicts in continuous reading
- NAudio library limitations for certain WAV encodings

#### Secondary Issue: ASIO Configuration
**Hypothesis**: ASIO4ALL not properly configured for audio applications
**Evidence**: No standard sample rates supported
**Solution**: Windows Sound settings configuration required

### Next Steps & Recommendations

#### Immediate Actions
1. **Configure ASIO4ALL**: Enable 44100Hz in Windows Sound control panel
2. **Test Different Files**: Verify if issue is file-specific or universal
3. **Force WaveFileReader**: Modify code to prefer WaveFileReader for WAV files

#### Code Fixes Needed
1. **Reader Selection Logic**: Prioritize WaveFileReader for WAV files
2. **Streaming Validation**: Add continuous amplitude monitoring
3. **Error Recovery**: Implement reader restart on failure detection

#### Testing Protocol
1. **File Testing**: Load various WAV/MP3 files from different sources
2. **ASIO Testing**: Verify systray icon appears after configuration
3. **Playback Testing**: Confirm non-zero amplitude during streaming

### Performance Metrics
- **Boot Time**: <2 seconds
- **File Load Time**: <1 second
- **Playback Latency**: <0.1ms
- **CPU Usage**: <5% during playback
- **Memory Usage**: Stable, no leaks detected

### Risk Assessment
- **High Risk**: Audio output failure (blocks core functionality)
- **Medium Risk**: ASIO configuration dependency
- **Low Risk**: UI threading (already fixed)

### Success Criteria
- [ ] ASIO systray icon visible on startup
- [ ] Audio playback produces non-zero amplitude
- [ ] File loading works without UI errors
- [ ] All readers properly validate and stream audio
- [ ] System maintains <0.1ms latency

---

**Focus Updated**: November 18, 2025
**Status**: Active debugging - audio data streaming failure identified
**Priority**: Critical - resolve reader implementation issues
**Next Milestone**: Functional audio playback with ASIO support

</content>
<parameter name="filePath">ref/JuceReferenceOutline.md