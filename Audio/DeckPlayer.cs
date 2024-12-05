using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace DJMixMaster.Audio
{
    public class DeckPlayer : IDisposable
    {
        private AudioFileReader? audioFile;
        private VolumeSampleProvider? volumeProvider;
        private readonly WaveFormat waveFormat;
        private float[] waveformData = Array.Empty<float>();
        private float volume = 1.0f;
        private float speed = 1.0f;
        private readonly BeatDetector beatDetector;
        private readonly List<double> cuePoints;
        private double trackLength;
        private System.Threading.Timer? positionTimer;
        private bool isDisposed;

        public event EventHandler<double>? PlaybackPositionChanged;
        public double CurrentPosition => audioFile?.CurrentTime.TotalSeconds ?? 0;
        public List<double> BeatPositions => beatDetector?.BeatPositions ?? new List<double>();
        public double BPM => beatDetector?.BPM ?? 0;

        public DeckPlayer(int sampleRate, int channels)
        {
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            beatDetector = new BeatDetector();
            cuePoints = new List<double>();
        }

        public void LoadTrack(string filePath)
        {
            try
            {
                DisposeCurrentTrack();

                audioFile = new AudioFileReader(filePath);
                volumeProvider = new VolumeSampleProvider(audioFile.ToSampleProvider());
                volumeProvider.Volume = volume;
                trackLength = audioFile.TotalTime.TotalSeconds;

                // Generate waveform data
                GenerateWaveformData();

                // Analyze beats
                beatDetector.AnalyzeFile(filePath);
            }
            catch (Exception ex)
            {
                DisposeCurrentTrack();
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
                float[] buffer = new float[samplesPerPixel];

                for (int i = 0; i < waveformData.Length; i++)
                {
                    readCount = audioFile.Read(buffer, 0, buffer.Length);
                    if (readCount == 0) break;

                    float sum = 0;
                    for (int j = 0; j < readCount; j++)
                    {
                        float abs = Math.Abs(buffer[j]);
                        sum += abs;
                        if (abs > maxValue) maxValue = abs;
                    }

                    waveformData[i] = sum / readCount;
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
            }
            catch (Exception ex)
            {
                waveformData = Array.Empty<float>();
                throw new Exception($"Failed to generate waveform data: {ex.Message}", ex);
            }
        }

        public void Play()
        {
            if (audioFile == null) return;
            StartPositionTracking();
        }

        public void Pause()
        {
            StopPositionTracking();
        }

        public void SetVolume(float newVolume)
        {
            volume = Math.Clamp(newVolume, 0f, 1f);
            if (volumeProvider != null)
            {
                volumeProvider.Volume = volume;
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Math.Clamp(newSpeed, 0.5f, 2f);
            // Implement pitch/speed control
        }

        public void Seek(TimeSpan position)
        {
            if (audioFile == null) return;
            try
            {
                var newPosition = audioFile.CurrentTime + position;
                if (newPosition < TimeSpan.Zero)
                    newPosition = TimeSpan.Zero;
                if (newPosition > audioFile.TotalTime)
                    newPosition = audioFile.TotalTime;
                
                audioFile.CurrentTime = newPosition;
                PlaybackPositionChanged?.Invoke(this, CurrentPosition);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to seek: {ex.Message}", ex);
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
                throw new Exception($"Failed to jump to cue point: {ex.Message}", ex);
            }
        }

        private void StartPositionTracking()
        {
            try
            {
                StopPositionTracking();
                positionTimer = new System.Threading.Timer(
                    _ => 
                    {
                        try 
                        {
                            if (!isDisposed && audioFile != null)
                            {
                                PlaybackPositionChanged?.Invoke(this, CurrentPosition);
                            }
                        }
                        catch 
                        {
                            // Ignore timer callback errors
                        }
                    },
                    null,
                    0,
                    33); // ~30fps update rate
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start position tracking: {ex.Message}", ex);
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

        private void DisposeCurrentTrack()
        {
            StopPositionTracking();
            if (audioFile != null)
            {
                audioFile.Dispose();
                audioFile = null;
            }
            volumeProvider = null;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                DisposeCurrentTrack();
            }
        }

        public ISampleProvider GetSampleProvider()
        {
            return volumeProvider ?? new SilenceProvider(waveFormat).ToSampleProvider();
        }

        public float[] GetWaveformData()
        {
            return waveformData;
        }

        public double GetTrackLength()
        {
            return trackLength;
        }
    }
}
