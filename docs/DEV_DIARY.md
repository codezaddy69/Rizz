# Development Diary

## November 22, 2025

### Events
- Implemented bus-based crossfader logic in ShredEngine.cpp callback, separating deck volumes from crossfader gains.
- Ensured full-range volume faders (0-1.0) with no artificial caps in ClubMixer.cpp.
- Added bus mixing in callback for LEFT/RIGHT orientation buses, matching Mixxx standards.
- Added getCrossfader() method in ClubMixer for crossfader value access.
- Disabled DSP temporarily for testing, then re-enabled for production use.
- Added callback logging for deck volumes and crossfader values.
- Optimized master gain slider to update engine only on DragCompleted, preventing spam calls.
- Followed SOP for logging: file-based detailed logs, console for critical events.
- Final testing: Volume faders still reported as not working audibly despite code functionality.

### Decisions
- Adopted Mixxx-inspired bus-based mixing to isolate volume faders from crossfader interference.
- Removed deck volume caps to provide professional full-range control.

### Direction
- Focus on testing volume fader independence and crossfader functionality.
- Next: Verify faders work with actual audio playback, add VU meters if needed.

### Theory
- Bus-based crossfader prevents additive modulation from overriding individual deck volumes.
- Full-range faders essential for DJ mixing precision.

### Critiques
- Previous additive crossfader was too simplistic; bus approach is more robust.

## November 20, 2025

### Events
- Focused on debugging DJMixMaster's audio playback system, identifying critical issues with silent output despite file validation.
- Analyzed RizzAudioEngine C++ components (ShredEngine, ClubMixer, Selekta, ScratchBuffer) for dual-deck support.
- Implemented WAV file loading in ScratchBuffer, added extensive logging to C++ engine boot and processes.
- Fixed class name inconsistencies (ClubMixer vs RizzEngineMixer), removed unused BeatSource files.
- Created RizzAudioEngineTestHarness.cs for auto-testing dual-deck simultaneous playback.
- Added --run-test command-line option for headless testing.
- Built C++ shared library (libShredEngine.so) on Linux; awaiting Windows DLL cross-compilation with mingw-w64.
- Removed old NAudio dependencies and references to avoid confusion.
- Implemented PortAudio integration for real audio output: Selekta with device enumeration/stream opening, ShredEngine with callback for deck processing and mixing.
- Created vcpkg setup script in ref/ and PortAudio documentation in docs/portaudio.md.
- Built Windows DLL with PortAudio, tested auto-test successfully: files load, dual-deck play/pause works, audio stream active.
- Implemented file logging framework (planned), removed per-frame console logs per SOP.
- Created docs/log_policy.md with full logging and error handling policy.
- Updated AGENTS.md to reference log_policy.md.
- Added try-catch to all ShredEngine functions for comprehensive error handling.
- Fixed audio playback: Forced device selection to Realtek Headphones, increased volume to 1.0, adjusted sine frequency for clear tone. Audio now plays successfully.

### Decisions
- Committed to pure C++ RizzAudioEngine (Mixxx-inspired) as sole audio backend, no fallbacks.
- Prioritized dual-deck simultaneous playback testing with simulated MIDI commands.
- Compartmentalized testing code for easy removal in stable builds.
- Used cross-compilation for Windows DLL to maintain compatibility.
- Integrated PortAudio for real-time audio output, enabling ASIO support on Windows.

### Direction
- Achieve functional C++ audio engine with reliable file loading, playback, and output.
- Establish robust auto-testing pipeline for regression prevention.
- Enable ASIO systray loading and low-latency audio for DJ use.
- Move toward MIDI controller integration once engine is stable.

### Theory
- C++ engine offers superior low-latency performance and control compared to C# NAudio for DJ applications.
- Extensive logging enables precise failure isolation in complex audio pipelines.
- Headless testing ensures consistent validation without GUI dependencies.
- PortAudio provides cross-platform audio I/O with ASIO backend for professional audio.

### Critique
- Path is synergistic: C++ expertise builds on existing audio knowledge, PortAudio integration adds professional audio capabilities.
- Truthful assessment: Silent output resolved with PortAudio; ASIO loading now possible on Windows.
- Balanced view: Gains in performance and ASIO support outweigh integration complexity; vcpkg simplifies dependency management.
- Potential pitfall: PortAudio configuration may require user setup; ensure fallback if ASIO fails.

### Next Steps
- Install PortAudio on Windows via vcpkg.
- Cross-compile DLL with PortAudio linked.
- Execute auto-test, verify ASIO systray loading and audio output.

## November 21, 2025

### Events
- Added resampleAudio function to ScratchBuffer for resampling non-44100 Hz files using linear interpolation.
- Improved WAV loading to handle 32-bit PCM and float formats based on audioFormat.
- Added audioFormat field to FileInfo struct for proper format detection.
- Modified CMakeLists and ShredEngine.h for cross-platform DLL export (Windows __declspec, Linux __attribute__).
- Attempted cross-compilation with mingw-w64, DLL built successfully.
- Ran auto-test, WAV loads but resampling log not appearing (DLL update issue suspected).

### Decisions
- Implemented linear resampling to normalize all audio to 44100 Hz/stereo for PortAudio compatibility.
- Added format-specific handling for WAV (PCM vs float) to prevent data corruption.

### Direction
- Debug DLL update issue, ensure resampleAudio is called and working.
- Investigate WAV static: may be due to test file corruption or resampling artifacts.
- Proceed to ASIO integration for professional audio output.

### Theory
- Resampling ensures compatibility with fixed 44100 Hz PortAudio stream.
- Linear interpolation provides basic quality for testing; can upgrade to libsamplerate later.
- Cross-platform defines allow building on Linux for Windows deployment.

### Critique
- DLL build process complex due to cross-compilation; need reliable toolchain setup.
- Test file "ThisIsTrash.wav" may be corrupted (huge float values), affecting validation.
- Gains in resampling and format handling outweigh added complexity.

### Next Steps
- Confirm DLL update and resampleAudio execution in logs.
- Install ASIO4ALL v2 on Windows, configure for 44100 Hz.
- Modify ShredEngine device selection to prioritize ASIO over Realtek.
- Re-run auto-test with ASIO, verify low-latency (<10ms) and systray icon.
- Add ID3 metadata parsing for MP3 title/artist display.
- Update diary daily with progress, decisions, and critiques.

## November 21, 2025

### Events
- Fixed WAV parser to properly read fmt chunk, support 16/32-bit PCM/float WAV files.
- Implemented file type detection with getFileInfo() for WAV/MP3, extracting sample rate, channels, bits, length, duration.
- Added MP3 support using dr_mp3 header-only decoder for full MP3 playback.
- Cross-compiled DLL with MP3/WAV loading; MP3 plays successfully, WAV loads but has static (needs resampling or format fix).
- Ran app, confirmed MP3 loads and plays with volume control; WAV loads but outputs static.
- Committed all changes to Rizz branch.

### Decisions
- Used dr_mp3 for MP3 decoding to avoid complex library dependencies.
- Prioritized MP3 working first; WAV static issue likely due to float format or device mismatch.
- Committed despite WAV issue, as MP3 success is major milestone.

### Direction
- Debug WAV static: check if float values are correct, perhaps normalize or convert.
- Add resampling for non-44100 files.
- Integrate ASIO for low-latency; install ASIO4ALL and test.

### Theory
- MP3 decoding works because dr_mp3 handles MPEG correctly; WAV static suggests data corruption in parsing or playback.
- Float WAV may need different handling than PCM.

### Critique
- MP3 implementation efficient and working; WAV needs polish.
- Progress solid: file detection and MP3 support added in one session.

### Next Steps
- Fix WAV static by verifying data integrity.
- Add libsamplerate for resampling.
- Install ASIO4ALL, re-run with ASIO device selection.