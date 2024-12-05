using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using DJMixMaster.Controls;

namespace DJMixMaster.Audio
{
    public class DeckPlayer : IDisposable
    {
        private WaveStream? audioFile;
        private VolumeWaveProvider16? volumeProvider;
        private readonly WaveFormat waveFormat;
        private float[] waveformData = Array.Empty<float>();
        private readonly BeatDetector beatDetector;
        private readonly List<double> cuePoints;
        private System.Threading.Timer? positionTimer;
        private float volume = 1.0f;
        private float speed = 1.0f;
        private bool isPlaying;
        private bool isDisposed;
        private double trackLength;
        private FaderControl faderLeft;
        private FaderControl faderRight;
        private IWaveProvider? sourceProvider;
        private readonly object volumeLock = new object();
        private string? currentTrackPath;

        public event EventHandler<double>? PlaybackPositionChanged;
        public double CurrentPosition => audioFile?.CurrentTime.TotalSeconds ?? 0;
        public List<double> BeatPositions => beatDetector?.BeatPositions ?? new List<double>();
        public double BPM => beatDetector?.BPM ?? 0;
        public bool IsPlaying => isPlaying;

        public DeckPlayer(FaderControl faderLeft, FaderControl faderRight)
        {
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            beatDetector = new BeatDetector();
            cuePoints = new List<double>();
            this.faderLeft = faderLeft;
            this.faderRight = faderRight;
            InitializeVolume();
            Logger.Log("DeckPlayer initialized", LogLevel.Debug);
        }

        private void InitializeVolume()
        {
            // Set initial volume to 100%
            volume = 1.0f;
            
            // Link volume to L and R faders
            LinkVolumeToFaders();
        }

        private void LinkVolumeToFaders()
        {
            // Assuming faderLeft and faderRight are the fader controls
            faderLeft.ValueChanged += (s, e) => AdjustVolume();
            faderRight.ValueChanged += (s, e) => AdjustVolume();
        }

        private void AdjustVolume()
        {
            // Adjust volume based on fader positions
            float leftVolume = (float)(faderLeft.Value / faderLeft.Maximum);
            float rightVolume = (float)(faderRight.Value / faderRight.Maximum);
            Logger.LogDebug($"Adjusting volume: Left = {leftVolume}, Right = {rightVolume}");
            EnsureVolumeProviderInitialized();
            if (volumeProvider != null)
            {
                volumeProvider.Volume = (leftVolume + rightVolume) / 2;
                Logger.LogDebug($"Volume set to: {volumeProvider.Volume}");
            }
            else
            {
                Logger.LogWarning("VolumeProvider is null in AdjustVolume method, cannot set volume.");
            }
        }

        private void EnsureVolumeProviderInitialized()
        {
            if (volumeProvider == null)
            {
                Logger.LogDebug("VolumeProvider is null, recreating...");
                volumeProvider = audioFile != null ? new VolumeWaveProvider16(audioFile) { Volume = volume } : null;
                Logger.LogDebug($"VolumeProvider recreated: {volumeProvider != null}");
            }
        }

        public void LoadAudioFile(string filePath)
        {
            try
            {
                // Clean up existing resources
                CleanupAudio();

                Logger.Log($"Loading audio file: {filePath}", LogLevel.Debug);
                
                // Determine the file extension
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                // Use Mp3FileReader for MP3 files
                if (extension == ".mp3")
                {
                    audioFile = new Mp3FileReader(filePath);
                }
                else
                {
                    // Fallback to AudioFileReader for other formats
                    audioFile = new AudioFileReader(filePath);
                }
                
                // Store track length
                trackLength = audioFile.TotalTime.TotalSeconds;
                Logger.Log($"Track length: {trackLength}s", LogLevel.Debug);

                // Create the volume sample provider
                EnsureVolumeProviderInitialized();

                Logger.LogDebug($"VolumeProvider created: {volumeProvider != null}");
                Logger.LogDebug($"VolumeProvider is null: {volumeProvider == null}");
                Logger.LogDebug($"audioFile is null: {audioFile == null}");

                // Generate waveform data
                GenerateWaveformData();

                Logger.Log("Audio file loaded successfully", LogLevel.Debug);
                Logger.Log($"Sample rate: {audioFile?.WaveFormat.SampleRate}", LogLevel.Debug);
                Logger.Log($"Channels: {audioFile?.WaveFormat.Channels}", LogLevel.Debug);
                Logger.Log($"Encoding: {audioFile?.WaveFormat.Encoding}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load audio file: {ex.Message}", ex);
                throw new Exception($"Failed to load audio file: {ex.Message}", ex);
            }
        }

        private void GenerateWaveformData()
        {
            if (audioFile == null) return;

            try
            {
                // Reset position to start
                audioFile.Position = 0;

                // Calculate number of samples for visualization
                int samplesPerPixel = (int)(audioFile.Length / (audioFile.WaveFormat.BlockAlign * 1000));
                waveformData = new float[1000]; // Store 1000 points for visualization

                float maxValue = 0;
                int readCount = 0;
                byte[] buffer = new byte[samplesPerPixel * audioFile.WaveFormat.BlockAlign];

                for (int i = 0; i < waveformData.Length; i++)
                {
                    readCount = audioFile.Read(buffer, 0, buffer.Length);
                    if (readCount == 0) break;

                    float sum = 0;
                    for (int j = 0; j < readCount / audioFile.WaveFormat.BlockAlign; j++)
                    {
                        float abs = Math.Abs(BitConverter.ToSingle(buffer, j * audioFile.WaveFormat.BlockAlign));
                        sum += abs;
                        if (abs > maxValue) maxValue = abs;
                    }

                    waveformData[i] = sum / (readCount / audioFile.WaveFormat.BlockAlign);
                }

                // Normalize waveform data
                if (maxValue > 0)
                {
                    for (int i = 0; i < waveformData.Length; i++)
                    {
                        waveformData[i] /= maxValue;
                    }
                }

                // Reset position to start
                audioFile.Position = 0;
                Logger.Log("Waveform data generated successfully", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to generate waveform data: {ex.Message}", ex);
                waveformData = Array.Empty<float>();
            }
        }

        private void CleanupAudio()
        {
            try
            {
                isPlaying = false;
                StopPositionTracking();

                if (audioFile != null)
                {
                    audioFile.Dispose();
                    audioFile = null;
                }

                EnsureVolumeProviderInitialized();
                Logger.LogDebug("VolumeProvider disposed");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during cleanup: {ex.Message}", ex);
            }
        }

        public ISampleProvider GetSampleProvider()
        {
            if (volumeProvider == null || !isPlaying)
            {
                Logger.LogDebug($"Returning silence. volumeProvider null: {volumeProvider == null}, isPlaying: {isPlaying}");
                return new SilenceProvider(waveFormat).ToSampleProvider();
            }

            // Convert VolumeWaveProvider16 to ISampleProvider using NAudio's conversion
            return volumeProvider.ToSampleProvider();
        }

        public void Play()
        {
            if (audioFile == null)
            {
                Logger.LogWarning("Cannot play: No audio file loaded");
                return;
            }

            try
            {
                Logger.LogDebug("Attempting to start playback");
                // Reset position if at end
                if (audioFile.Position >= audioFile.Length)
                {
                    Logger.LogDebug("Resetting position to start");
                    audioFile.Position = 0;
                }

                Logger.LogDebug($"Starting playback at position: {audioFile.Position}, Format: {audioFile.WaveFormat}, Volume: {volumeProvider?.Volume}");
                Logger.LogDebug($"VolumeProvider state: {volumeProvider != null}");
                
                isPlaying = true;
                Logger.LogDebug($"Playback started: isPlaying = {isPlaying}");
                Logger.LogDebug($"Playback state: isPlaying = {isPlaying}, Position = {audioFile?.CurrentTime}, Bytes read = {audioFile?.Position}");
                StartPositionTracking();
            }
            catch (Exception ex)
            {
                isPlaying = false;
                Logger.LogError("Failed to start playback", ex);
                throw;
            }
        }

        public void Pause()
        {
            Logger.LogDebug("Pausing playback");
            isPlaying = false;
            StopPositionTracking();
        }

        public void SetVolume(float newVolume)
        {
            if (volumeProvider == null)
            {
                Logger.LogDebug("VolumeProvider is null. Reinitializing...");
                EnsureVolumeProviderInitialized();
            }

            lock (volumeLock)
            {
                volume = Math.Clamp(newVolume, 0f, 1f);
                Logger.LogDebug($"Setting volume to: {volume}");
                if (volumeProvider != null)
                {
                    volumeProvider.Volume = volume;
                    Logger.LogDebug($"VolumeProvider is active. Volume set to: {volumeProvider.Volume}");
                }
                else
                {
                    Logger.LogWarning("VolumeProvider is null in SetVolume method.");
                }
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Math.Clamp(newSpeed, 0.5f, 2f);
            // Implement pitch/speed control
        }

        public void Seek(TimeSpan position)
        {
            if (position < TimeSpan.Zero)
            {
                position = TimeSpan.Zero;
            }
            if (audioFile == null)
            {
                Logger.LogWarning("Cannot seek: No audio file loaded");
                return;
            }

            try
            {
                Logger.LogDebug($"Seeking to position: {position}");
                audioFile.CurrentTime = position;
                
                // Create new volume provider after seeking
                EnsureVolumeProviderInitialized();
                Logger.LogDebug($"VolumeProvider recreated after seeking: {volumeProvider != null}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to seek to position {position}", ex);
                throw;
            }
        }

        public void FastForward(int seconds)
        {
            if (audioFile == null)
            {
                Logger.LogWarning("Cannot fast forward: No audio file loaded");
                return;
            }

            try
            {
                var newPosition = audioFile.CurrentTime.Add(TimeSpan.FromSeconds(seconds));
                if (newPosition > audioFile.TotalTime)
                {
                    newPosition = audioFile.TotalTime;
                }
                Logger.LogDebug($"Fast forwarding {seconds}s to {newPosition}");
                Seek(newPosition);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to fast forward {seconds} seconds", ex);
                throw;
            }
        }

        public void Rewind(int seconds)
        {
            if (audioFile == null)
            {
                Logger.LogWarning("Cannot rewind: No audio file loaded");
                return;
            }

            try
            {
                var newPosition = audioFile.CurrentTime.Subtract(TimeSpan.FromSeconds(seconds));
                if (newPosition < TimeSpan.Zero)
                {
                    newPosition = TimeSpan.Zero;
                }
                Logger.LogDebug($"Rewinding {seconds}s to {newPosition}");
                Seek(newPosition);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to rewind {seconds} seconds", ex);
                throw;
            }
        }

        public void AddCuePoint()
        {
            if (audioFile == null) return;

            try
            {
                var currentPosition = audioFile.CurrentTime.TotalSeconds;
                if (!cuePoints.Contains(currentPosition))
                {
                    cuePoints.Add(currentPosition);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to add cue point: {ex.Message}", ex);
                throw new Exception($"Failed to add cue point: {ex.Message}", ex);
            }
        }

        public void JumpToCuePoint(int index)
        {
            if (audioFile == null) return;
            if (index < 0 || index >= cuePoints.Count) return;

            try
            {
                audioFile.CurrentTime = TimeSpan.FromSeconds(cuePoints[index]);
                PlaybackPositionChanged?.Invoke(this, CurrentPosition);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to jump to cue point: {ex.Message}", ex);
                throw new Exception($"Failed to jump to cue point: {ex.Message}", ex);
            }
        }

        private void StartPositionTracking()
        {
            try
            {
                StopPositionTracking();
                Logger.LogDebug("Starting position tracking");
                
                positionTimer = new System.Threading.Timer(
                    _ => 
                    {
                        try 
                        {
                            if (audioFile != null && isPlaying)
                            {
                                var position = audioFile.CurrentTime.TotalSeconds;
                                var bytesRead = audioFile.Position;
                                Logger.LogDebug($"Position: {position:F2}s, Bytes read: {bytesRead}, Total length: {audioFile.Length}");
                                PlaybackPositionChanged?.Invoke(this, position);

                                // Move the audio position forward manually
                                var read = audioFile.Read(new byte[4096], 0, 4096);

                                // Check if we've reached the end of the track
                                if (read == 0 || position >= audioFile.TotalTime.TotalSeconds)
                                {
                                    Logger.LogDebug("Reached end of track");
                                    isPlaying = false;
                                    StopPositionTracking();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Position tracking error", ex);
                        }
                    },
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(50)
                );
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to start position tracking", ex);
                throw;
            }
        }

        private void StopPositionTracking()
        {
            try
            {
                if (positionTimer != null)
                {
                    positionTimer.Dispose();
                    positionTimer = null;
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public float[] GetWaveformData()
        {
            return waveformData;
        }

        public double GetTrackLength()
        {
            return trackLength;
        }

        private void DisposeCurrentTrack()
        {
            try
            {
                StopPositionTracking();
                audioFile?.Dispose();
                audioFile = null;
                EnsureVolumeProviderInitialized();
                waveformData = Array.Empty<float>();
                Logger.LogDebug("VolumeProvider disposed");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                DisposeCurrentTrack();
            }
        }

        public void InitializeAudio(string filePath)
        {
            currentTrackPath = filePath;
            var reader = new AudioFileReader(filePath);
            sourceProvider = reader;

            // Initialize the VolumeProvider
            volumeProvider = new VolumeWaveProvider16(sourceProvider);
            volumeProvider.Volume = 1.0f; // Default volume

            Logger.LogDebug("Creating VolumeProvider");
        }

        public void SetTrack(string filePath)
        {
            // Dispose of old provider
            if (volumeProvider != null)
            {
                // No Dispose method for VolumeWaveProvider16, just set to null
                volumeProvider = null;
            }

            // Reinitialize
            InitializeAudio(filePath);
        }

        public void SetVolumeThreadSafe(float volume)
        {
            lock (volumeLock)
            {
                if (volumeProvider != null)
                {
                    volumeProvider.Volume = volume;
                }
            }
        }
    }
}
