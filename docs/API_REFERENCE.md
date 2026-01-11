# DJ Mix Master - API Reference

## Core Classes

### RizzApplication

The main application class that manages the lifecycle of the DJ mixing software.

#### Constructor
```csharp
RizzApplication(ILogger<RizzApplication> logger)
```

#### Methods
```csharp
void Initialize()
// Initializes the audio engine, logging, and UI components

void Shutdown()
// Cleans up resources and shuts down the audio pipeline
```

#### Properties
```csharp
ILogger Logger { get; }
// Application logger instance
```

### AudioEngine

High-level audio management class implementing the IAudioEngine interface.

#### Methods
```csharp
void LoadTrack(int deck, string filePath)
// Loads an audio file into the specified deck (0 or 1)

void Play(int deck)
// Starts playback on the specified deck

void Pause(int deck)
// Pauses playback on the specified deck

void Stop(int deck)
// Stops playback and resets position on the specified deck

void SetVolume(int deck, float volume)
// Sets the volume for the specified deck (0.0 to 1.0)

void SetCrossfader(float position)
// Sets the crossfader position (-1.0 left, 0.0 center, 1.0 right)

AudioFileProperties GetProperties(int deck)
// Returns metadata for the loaded track on the specified deck

float GetPosition(int deck)
// Returns the current playback position (0.0 to 1.0)

void SetPosition(int deck, float position)
// Sets the playback position for the specified deck
```

### Deck

Represents an individual audio deck with its own playback controls.

#### Constructor
```csharp
Deck(AudioEngine engine, int deckNumber)
```

#### Methods
```csharp
void Load(string filePath)
// Loads an audio file into this deck

void Play()
// Starts playback

void Pause()
// Pauses playback

void Stop()
// Stops playback and resets to beginning

void SetVolume(float volume)
// Sets deck volume (0.0 to 1.0)

void SetPosition(float position)
// Sets playback position (0.0 to 1.0)

float GetPosition()
// Returns current playback position
```

#### Properties
```csharp
AudioFileProperties Properties { get; }
// Metadata for the loaded track

bool IsPlaying { get; }
// Current playback state

float Volume { get; set; }
// Current volume level
```

### RizzAudioEngine

C# wrapper for the native C++ audio processing engine.

#### Constructor
```csharp
RizzAudioEngine()
```

#### Methods
```csharp
IntPtr CreateEngine()
// Creates a new native engine instance

void DestroyEngine(IntPtr handle)
// Destroys a native engine instance

bool LoadFile(IntPtr handle, string filePath)
// Loads an audio file into the native engine

bool Play(IntPtr handle)
// Starts audio playback

bool Pause(IntPtr handle)
// Pauses audio playback

bool SetVolume(IntPtr handle, float volume)
// Sets master volume

IntPtr GetBuffer(IntPtr handle, int samples)
// Retrieves audio samples for processing
```

## UI Components

### MainWindow

The primary WPF window containing the DJ interface.

#### XAML Structure
```xml
<Window x:Class="DJMixMaster.MainWindow"
        xmlns:local="clr-namespace:DJMixMaster"
        xmlns:visualization="clr-namespace:DJMixMaster.Visualization">
    <Grid>
        <!-- Deck areas, controls, waveforms -->
    </Grid>
</Window>
```

#### Key Controls
- `leftWaveform`: WaveformVisualizer for deck 1
- `rightWaveform`: WaveformVisualizer for deck 2
- `btnLeftPlay/btnRightPlay`: Transport controls
- `sliderLeftVolume/sliderRightVolume`: Volume controls
- `sliderCrossfader`: Crossfader control

### WaveformVisualizer

Custom control for real-time waveform rendering.

#### Properties
```csharp
public float[] WaveformData { get; set; }
// Audio samples for visualization

public double ZoomLevel { get; set; }
// Zoom factor for waveform display

public TimeSpan Position { get; set; }
// Current playback position marker
```

#### Methods
```csharp
void UpdateWaveform(float[] data)
// Updates the waveform with new audio data

void SetPositionMarker(double position)
// Sets the playback position indicator
```

## Data Structures

### AudioFileProperties

Contains metadata extracted from audio files.

```csharp
public class AudioFileProperties
{
    public string FilePath { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public TimeSpan Duration { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitDepth { get; set; }
    public long FileSize { get; set; }
    public string Format { get; set; }
}
```

### DeckEventArgs

Event arguments for deck-related events.

```csharp
public class DeckEventArgs : EventArgs
{
    public int DeckNumber { get; set; }
    public string TrackTitle { get; set; }
    public AudioFileProperties Properties { get; set; }
}
```

## Events

### AudioEngine Events
```csharp
public event EventHandler<DeckEventArgs> TrackLoaded;
public event EventHandler<DeckEventArgs> PlaybackStarted;
public event EventHandler<DeckEventArgs> PlaybackStopped;
public event EventHandler<PositionChangedEventArgs> PositionChanged;
```

### UI Events
```csharp
// Button click handlers
private void btnLeftPlay_Click(object sender, RoutedEventArgs e)
private void btnRightLoad_Click(object sender, RoutedEventArgs e)

// Slider value changed
private void sliderLeftVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
private void sliderCrossfader_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
```

## Configuration

### Audio Settings (settings/audio.json)
```json
{
  "outputDevice": "ASIO Device",
  "sampleRate": 44100,
  "bufferSize": 512,
  "crossfaderCurve": "linear",
  "masterVolume": 0.8
}
```

### Application Settings
```csharp
// Loaded from appsettings.json
public class AppSettings
{
    public LoggingSettings Logging { get; set; }
    public AudioSettings Audio { get; set; }
    public UiSettings Ui { get; set; }
}
```

## Error Handling

### Exception Types
```csharp
public class AudioEngineException : Exception
{
    public AudioEngineException(string message) : base(message) { }
}

public class FileLoadException : AudioEngineException
{
    public FileLoadException(string filePath)
        : base($"Failed to load audio file: {filePath}") { }
}
```

### Error Logging
```csharp
try
{
    audioEngine.LoadTrack(deck, filePath);
}
catch (FileLoadException ex)
{
    logger.LogError(ex, "Failed to load track {Path}", filePath);
    // Show user-friendly error message
}
```

## Performance Metrics

### Audio Latency
- ASIO: <10ms
- WASAPI: 20-50ms
- DirectSound: 100-200ms

### CPU Usage
- Idle: <5%
- Playback: 10-20%
- Effects processing: 20-40%

### Memory Usage
- Base application: 50MB
- Per loaded track: 10-50MB
- Waveform data: 1-5MB per track