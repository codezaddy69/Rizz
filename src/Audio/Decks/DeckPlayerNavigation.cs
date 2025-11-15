using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace DJMixMaster.Audio
{
    public class DeckPlayerNavigation
    {
        private readonly ILogger logger;
        private readonly DeckPlayerAudioProcessing audioProcessing;
        private readonly CancellationTokenSource positionTrackingCts;
        private float speed = 1.0f;
        private bool isPlaying;

        public event EventHandler<double>? PlaybackPositionChanged;
        public double CurrentPosition => audioProcessing.AudioFile?.CurrentTime.TotalSeconds ?? 0;
        public bool IsPlaying => isPlaying;
        public float Speed
        {
            get => speed;
            set
            {
                speed = Math.Clamp(value, 0.5f, 2f);
                logger.LogDebug($"Speed set to {speed}");
            }
        }

        public DeckPlayerNavigation(DeckPlayerAudioProcessing audioProcessing, ILogger logger)
        {
            this.audioProcessing = audioProcessing;
            this.logger = logger;
            positionTrackingCts = new CancellationTokenSource();
        }

        public void Seek(TimeSpan position)
        {
            if (audioProcessing.AudioFile == null)
            {
                logger.LogWarning("Cannot seek: No audio file loaded");
                return;
            }

            try
            {
                logger.LogDebug($"Seeking to position {position}");
                audioProcessing.AudioFile.CurrentTime = position;
                
                // Notify position change
                PlaybackPositionChanged?.Invoke(this, position.TotalSeconds);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to seek to position {position}", ex);
                throw;
            }
        }

        public void FastForward(double seconds)
        {
            if (audioProcessing.AudioFile == null) return;

            try
            {
                var currentPosition = audioProcessing.AudioFile.CurrentTime;
                var newPosition = currentPosition.Add(TimeSpan.FromSeconds(seconds));
                
                if (newPosition > audioProcessing.AudioFile.TotalTime)
                {
                    newPosition = audioProcessing.AudioFile.TotalTime;
                }
                
                logger.LogDebug($"Fast forwarding {seconds}s to {newPosition}");
                Seek(newPosition);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to fast forward {seconds} seconds", ex);
                throw;
            }
        }

        public void Rewind(double seconds)
        {
            if (audioProcessing.AudioFile == null) return;

            try
            {
                var currentPosition = audioProcessing.AudioFile.CurrentTime;
                var newPosition = currentPosition.Subtract(TimeSpan.FromSeconds(seconds));
                
                if (newPosition < TimeSpan.Zero)
                {
                    newPosition = TimeSpan.Zero;
                }
                
                logger.LogDebug($"Rewinding {seconds}s to {newPosition}");
                Seek(newPosition);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to rewind {seconds} seconds", ex);
                throw;
            }
        }

        public void StartPositionTracking()
        {
            if (audioProcessing.AudioFile == null) return;

            Task.Run(async () =>
            {
                while (!positionTrackingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (isPlaying && audioProcessing.AudioFile != null)
                        {
                            var position = audioProcessing.AudioFile.CurrentTime.TotalSeconds;
                            var bytesRead = audioProcessing.AudioFile.Position;
                            logger.LogDebug($"Position: {position:F2}s, Bytes read: {bytesRead}, Total length: {audioProcessing.AudioFile.Length}");
                            PlaybackPositionChanged?.Invoke(this, position);

                            // Check if we've reached the end of the track
                            if (position >= audioProcessing.TrackLength)
                            {
                                isPlaying = false;
                                logger.LogDebug("Reached end of track");
                                break;
                            }
                        }
                        await Task.Delay(50, positionTrackingCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error in position tracking: {ex.Message}");
                        break;
                    }
                }
            }, positionTrackingCts.Token);
        }

        public void StopPositionTracking()
        {
            positionTrackingCts.Cancel();
        }

        public void SetPlaybackState(bool playing)
        {
            isPlaying = playing;
            if (playing)
            {
                StartPositionTracking();
            }
        }
    }
}
