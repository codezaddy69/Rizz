# AudioEngine

## Overview
The `AudioEngine` class is the main audio processing component of DJMixMaster, responsible for managing audio playback, volume control, crossfading, and waveform generation for two decks.

## Public API

### Properties
- `PlaybackPositionChanged`: Event raised when playback position changes
- `BeatGridUpdated`: Event raised when beat grid is updated

### Methods
- `LoadFile(int deckNumber, string filePath)`: Loads an audio file for the specified deck
- `Play(int deckNumber)`: Starts playback for the specified deck
- `Pause(int deckNumber)`: Pauses playback for the specified deck
- `Stop(int deckNumber)`: Stops playback for the specified deck
- `Seek(int deckNumber, double seconds)`: Seeks to a specific time in the track
- `GetPosition(int deckNumber)`: Gets the current playback position
- `GetLength(int deckNumber)`: Gets the total length of the loaded track
- `SetVolume(int deckNumber, float volume)`: Sets the volume for the specified deck
- `GetVolume(int deckNumber)`: Gets the current volume for the specified deck
- `IsPlaying(int deckNumber)`: Checks if the specified deck is playing
- `SetCrossfader(float position)`: Sets the crossfader position (-1 to 1)
- `GetCrossfader()`: Gets the current crossfader position
- `GetWaveformData(int deckNumber)`: Retrieves waveform data for visualization
- `AddCuePoint(int deckNumber)`: Adds a cue point at the current position
- `JumpToCuePoint(int deckNumber, int cueIndex)`: Jumps to a specific cue point
- `PlayTestTone(int deckNumber, double frequency, double durationSeconds)`: Plays a test tone

## Internal Details

### Dependencies
- `Controllers.PlaybackController`: Handles playback operations
- `Controllers.VolumeManager`: Manages volume and crossfader
- `Generators.WaveformGenerator`: Generates waveform data
- `Managers.CuePointManager`: Manages cue points
- NAudio libraries for audio processing

### Architecture
Uses composition pattern with specialized controllers for different concerns. Implements `IAudioEngine` interface for testability.</content>
<parameter name="filePath">src/Audio/AudioEngine.md