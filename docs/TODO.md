# TODO List

## Completed Tasks
- [x] Fix build errors and warnings (entry point, nullability, unused events)
- [x] Restructure project files into src/ directory
- [x] Update documentation with build status and project structure
- [x] Exclude reference folder from git tracking
- [x] Implement WPF Main method for proper application startup
- [x] Remove JUCE and NAudio code from src/
- [x] Implement CSCore-based AudioEngine with Deck, MixingProvider, VstHost
- [x] Update AudioEngineInitializer for CSCore engine
- [x] Update MainWindow to use new audio engine
- [x] Add CSCore and VST.NET documentation to ref/ folder
- [x] Fix AudioSettingsWindow XAML StaticResource error and add component-specific logging

## Priority Tasks
- [ ] Add CSCore and VST.NET NuGet packages (CSCore removed due to compatibility)
- [x] Implement crossfading with MixingProvider
- [x] Implement volume faders for decks
- [x] Implement play/pause/stop controls
- [x] Implement load/eject for tracks
- [ ] Test full DJ functionality (crossfader, decks, mixing)

## Future Features (Post-Mixing Stability)

### VST Plugin Support (VST.NET - No JUCE)
- [ ] Add VST.NET NuGet package for .NET-native VST2/VST3 hosting
- [ ] Implement VstHostManager class for plugin loading and audio processing
- [ ] Create per-deck VST effect chains (pre-C++ engine processing)
- [ ] Add master bus VST effects (post-mixing, pre-output)
- [ ] Build VST plugin browser UI with .dll scanning and drag-and-drop
- [ ] Implement VST parameter automation and MIDI learn functionality
- [ ] Add CPU monitoring and automatic plugin bypass on overload
- [ ] Create VST preset save/load system with session management
- [ ] Ensure compatibility with clipping protection (VST processing before clipping algorithms)
- [ ] Add VST rack visualization in main window with bypass controls
- [ ] Implement sidechain compression between decks via VST
- [ ] Add VST plugin validation and crash isolation

## General Tasks
- [ ] Implement beat detection using CSCore FFT
- [ ] Implement waveform generation from audio data
- [ ] Add position seeking and cue points
- [ ] Optimize for low latency audio processing
