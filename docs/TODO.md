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

## Priority Tasks
- [ ] Add CSCore and VST.NET NuGet packages
- [ ] Implement crossfading with MixingProvider
- [ ] Implement volume faders for decks
- [ ] Implement play/pause/stop controls
- [ ] Implement load/eject for tracks
- [ ] Test full DJ functionality (crossfader, decks, mixing)

## General Tasks
- [ ] Implement beat detection using CSCore FFT
- [ ] Implement waveform generation from audio data
- [ ] Integrate VST.NET for plugin hosting
- [ ] Add position seeking and cue points
- [ ] Optimize for low latency audio processing
