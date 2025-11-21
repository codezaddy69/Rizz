# MAIN HIGH LEVEL PRIORITY: Standard Operating Procedure (SOP)

Note: The Primary LOOP is I send you either Feedback or console errors or output. At this point you trouble shoot or dev then when you are finished the loop starts over. If feedback is given that the code is building with no errors and minimal warnings and a milestone is reached an update will be recommended where documentation is updated and a GIT is recommended. Do not commit without final approval. Then the loop continues after you recommend 3 options to proceed next.

IMPORTANT: Do not commit changes unless explicitly told to by the user.

## Daily Maintenance
- Update docs/DEV_DIARY.md every day with events, decisions, direction, theory, and critiques.

## Logging Guidelines
See docs/log_policy.md for full policy.
- Console logs: Only critical events (init, errors, major state changes).
- Detailed logs: Route to files for debugging (e.g., audio processing, frame details).
- Avoid per-frame logs to prevent console flooding and performance issues.

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

# DJMixMaster Development Focus: C++ RizzAudioEngine Integration & Auto-Testing

## Current Project Status: C++ Engine Implementation & Headless Testing Setup

### Executive Summary
Transitioned to pure C++ RizzAudioEngine (Mixxx-inspired) as sole audio backend. Implemented WAV file loading, extensive logging, and dual-deck support. Removed old NAudio systems to avoid confusion. Auto-testing harness simulates MIDI commands for headless dual-deck playback validation. C++ shared library built on Linux; awaiting Windows DLL cross-compilation with mingw-w64.

### Key Findings from Implementation

#### ✅ System Architecture: ROBUST
- **C++ Engine**: ShredEngine, ScratchBuffer, ClubMixer, Selekta with dual-deck playback
- **File Loading**: Basic WAV parser in ScratchBuffer (16-bit, 44100Hz, mono/stereo)
- **Logging**: Comprehensive boot/init/process logging for failure detection
- **Testing**: RizzAudioEngineTestHarness.cs for simulated MIDI commands

#### ❌ Current Blockers
##### 1. DLL Build Pending
**Problem**: C++ built as Linux .so; Windows .dll needed for P/Invoke
**Impact**: Cannot run auto-test until DLL deployed
**Status**: mingw-w64 installing; cross-compilation planned
**Solution**: Build ShredEngine.dll, copy to C# bin directory

##### 2. Audio Validation Incomplete
**Problem**: C++ process() generates sine wave if no file loaded; actual playback untested
**Impact**: Silent output possible if WAV loading fails
**Status**: Code implemented; testing blocked by DLL
**Evidence**: Logs will show "[ScratchBuffer] Processed X frames from audio data" on success

##### 3. Cross-Platform Build Complexity
**Problem**: WSL Linux build vs Windows runtime
**Impact**: Delays testing; potential compatibility issues
**Status**: Mitigated by mingw cross-compilation
**Solution**: Use x86_64-w64-mingw32-g++ for Windows DLL

### Implemented Solutions

#### Phase 1: C++ Engine Core ✅
- **ShredEngine**: Dual-deck LoadFile/Play/Pause/Seek with logging
- **ScratchBuffer**: WAV loading, process() for audio streaming
- **ClubMixer**: Crossfader, volume mixing with logging
- **Selekta**: Dummy audio device management
- **Fixes**: Class names, includes, removed unused code

#### Phase 2: Auto-Testing Framework ✅
- **Test Harness**: Simulates load/play on both decks, monitors 10s, logs positions
- **Headless Mode**: --run-test CLI option for no-GUI testing
- **Compartmentalized**: RizzAudioEngineTestHarness.cs easy to remove

#### Phase 3: Cleanup & Documentation ✅
- **Removed Old Systems**: NAudio dependencies, BeatSource files, unused code
- **DEV_DIARY.md**: Daily tracking of events/decisions/direction/theory/critiques
- **AGENTS.md**: Added daily DEV_DIARY upkeep note

### Current Test Readiness

#### System Health: EXCELLENT
- **C# Build**: Success, no errors
- **C++ Compile**: Success on Linux
- **Integration**: P/Invoke ready for DLL
- **Logging**: Extensive for debugging

#### Audio Output: UNTESTED
- **File Loading**: Implemented but unverified
- **Playback**: Sine fallback if load fails
- **Dual-Deck**: Structure supports simultaneous play
- **Mixing**: Crossfader/volume logic in place

### Root Cause Analysis

#### Primary Issue: Build Environment Mismatch
**Hypothesis**: WSL Linux development vs Windows deployment requires cross-compilation
**Evidence**: .so built successfully, but .dll needed
**Possible Causes**: Platform differences, toolchain setup
**Solution**: mingw-w64 for Windows DLL

#### Secondary Issue: Audio Pipeline Validation
**Hypothesis**: C++ engine correct, but WAV parser may fail on complex files
**Evidence**: Simple parser assumes 16-bit PCM; may not handle all WAV variants
**Solution**: Test with ThisIsTrash.wav, expand parser if needed

### Next Steps & Recommendations

#### Immediate Actions
1. **Complete mingw Installation**: Finish sudo apt install -y mingw-w64
2. **Cross-Compile DLL**: Use mingw toolchain, build ShredEngine.dll
3. **Deploy & Test**: Copy DLL to bin/, run --run-test, analyze logs

#### Code Fixes Needed
1. **WAV Parser Robustness**: Add error handling for unsupported formats
2. **Audio Streaming**: Ensure looping and stereo handling correct
3. **Logging Granularity**: Add amplitude checks in process()

#### Testing Protocol
1. **DLL Deployment**: Verify ShredEngine.dll loads without errors
2. **Headless Test**: Run --run-test, check console logs for success
3. **Audio Validation**: Confirm non-sine output, dual positions advancing

### Performance Metrics
- **C++ Boot Time**: <1 second (estimated)
- **File Load Time**: <0.5 second (estimated)
- **Playback Latency**: <0.1ms (target)
- **CPU Usage**: <5% during playback (target)
- **Memory Usage**: Stable, no leaks (target)

### Risk Assessment
- **High Risk**: DLL build failure or compatibility issues
- **Medium Risk**: WAV parser limitations
- **Low Risk**: Testing framework (compartmentalized)

### Success Criteria
- [ ] ShredEngine.dll builds and loads successfully
- [ ] Auto-test runs without crashes, logs dual-deck playback
- [ ] Audio output non-silent, positions update correctly
- [ ] C++ engine handles ThisIsTrash.wav properly
- [ ] System maintains low latency and stability

---

**Focus Updated**: November 20, 2025
**Status**: C++ engine implemented, DLL build pending
**Priority**: Critical - complete cross-compilation and test
**Next Milestone**: Functional dual-deck playback via C++ engine

</content>
<parameter name="filePath">ref/JuceReferenceOutline.md