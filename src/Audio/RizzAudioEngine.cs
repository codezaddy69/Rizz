using System;
using Microsoft.Extensions.Logging;
using DJMixMaster.Audio;

namespace DJMixMaster.Audio
{
    public class RizzAudioEngine : IAudioEngine
    {
        private readonly ILogger<RizzAudioEngine> _logger;
        private readonly Deck[] _decks;

#pragma warning disable CS0067
        public event Action<object?, (int, double)>? PlaybackPositionChanged;
        public event Action<object?, (int, double[], double)>? BeatGridUpdated;
#pragma warning restore CS0067

        public RizzAudioEngine(ILogger<RizzAudioEngine> logger, bool isTestMode = false)
        {
            _logger = logger;
            try
            {
                _logger.LogInformation("Starting RizzAudioEngine boot");
                ShredEngineInterop.InitializeEngine(isTestMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ShredEngine DLL");
                throw;
            }

            // Create decks (minimal, since C++ handles playback)
            _logger.LogInformation("Starting deck initialization");
            var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            _decks = new Deck[2];
            for (int i = 0; i < _decks.Length; i++)
            {
                _logger.LogInformation("Starting deck {Index} boot", i);
                _decks[i] = new Deck(i, loggerFactory.CreateLogger<Deck>());
                _logger.LogInformation("Deck {Index} created", i);
            }
        }

        public void LoadFile(int deckNumber, string filePath)
        {
            try
            {
                int result = ShredEngineInterop.LoadFile(deckNumber, filePath);
                if (result == 0)
                {
                    _logger.LogInformation("File loaded successfully via ShredEngine: {File} on deck {Deck}", filePath, deckNumber);
                }
                else
                {
                    _logger.LogError("ShredEngine LoadFile failed with code {Code}", result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in LoadFile for deck {Deck}", deckNumber);
            }
        }

        public void Play(int deckNumber)
        {
            try
            {
                ShredEngineInterop.Play(deckNumber);
                _logger.LogInformation("Play requested on deck {Deck} via ShredEngine", deckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Play for deck {Deck}", deckNumber);
            }
        }

        public void Pause(int deckNumber)
        {
            try
            {
                ShredEngineInterop.Pause(deckNumber);
                _logger.LogInformation("Pause requested on deck {Deck} via ShredEngine", deckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Pause for deck {Deck}", deckNumber);
            }
        }

        public void Stop(int deckNumber)
        {
            try
            {
                ShredEngineInterop.Stop(deckNumber);
                _logger.LogInformation("Stop requested on deck {Deck} via ShredEngine", deckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Stop for deck {Deck}", deckNumber);
            }
        }

        public void Seek(int deckNumber, double seconds)
        {
            try
            {
                ShredEngineInterop.Seek(deckNumber, seconds);
                _logger.LogInformation("Seek requested on deck {Deck} to {Seconds}s via ShredEngine", deckNumber, seconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Seek for deck {Deck}", deckNumber);
            }
        }

        public double GetPosition(int deckNumber)
        {
            try
            {
                return ShredEngineInterop.GetPosition(deckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetPosition for deck {Deck}", deckNumber);
                return 0.0;
            }
        }

        public double GetLength(int deckNumber)
        {
            try
            {
                return ShredEngineInterop.GetLength(deckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetLength for deck {Deck}", deckNumber);
                return 0.0;
            }
        }

        public void SetVolume(int deckNumber, float volume)
        {
            try
            {
                ShredEngineInterop.SetVolume(deckNumber, volume);
                _logger.LogInformation("Volume set on deck {Deck} to {Volume} via ShredEngine", deckNumber, volume);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetVolume for deck {Deck}", deckNumber);
            }
        }

        public void SetCrossfader(float value)
        {
            try
            {
                ShredEngineInterop.SetCrossfader(value);
                _logger.LogInformation("Crossfader set to {Value} via ShredEngine", value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetCrossfader");
            }
        }

        public void SetMasterVolume(float volume)
        {
            try
            {
                ShredEngineInterop.SetMasterVolume(volume);
                _logger.LogInformation("Master volume set to {Volume} via ShredEngine", volume);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetMasterVolume");
            }
        }

        public void SetCrossfaderCurve(int curveType)
        {
            try
            {
                ShredEngineInterop.SetCrossfaderCurve(curveType);
                _logger.LogInformation("Crossfader curve set to {Curve} via ShredEngine", curveType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetCrossfaderCurve");
            }
        }

        // Clipping Protection Methods
        public void SetClippingProtectionEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetClippingProtectionEnabled(enabled);
                _logger.LogInformation("Clipping protection {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetClippingProtectionEnabled");
            }
        }

        public void SetDeckVolumeCapEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetDeckVolumeCapEnabled(enabled);
                _logger.LogInformation("Deck volume cap {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetDeckVolumeCapEnabled");
            }
        }

        public void SetPeakDetectionEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetPeakDetectionEnabled(enabled);
                _logger.LogInformation("Peak detection {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetPeakDetectionEnabled");
            }
        }

        public void SetSoftKneeCompressorEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetSoftKneeCompressorEnabled(enabled);
                _logger.LogInformation("Soft knee compressor {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetSoftKneeCompressorEnabled");
            }
        }

        public void SetLookAheadLimiterEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetLookAheadLimiterEnabled(enabled);
                _logger.LogInformation("Look-ahead limiter {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetLookAheadLimiterEnabled");
            }
        }

        public void SetRmsMonitoringEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetRmsMonitoringEnabled(enabled);
                _logger.LogInformation("RMS monitoring {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetRmsMonitoringEnabled");
            }
        }

        public void SetAutoGainReductionEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetAutoGainReductionEnabled(enabled);
                _logger.LogInformation("Auto gain reduction {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetAutoGainReductionEnabled");
            }
        }

        public void SetBrickwallLimiterEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetBrickwallLimiterEnabled(enabled);
                _logger.LogInformation("Brickwall limiter {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetBrickwallLimiterEnabled");
            }
        }

        public void SetClippingIndicatorEnabled(bool enabled)
        {
            try
            {
                ShredEngineInterop.SetClippingIndicatorEnabled(enabled);
                _logger.LogInformation("Clipping indicator {State} via ShredEngine", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetClippingIndicatorEnabled");
            }
        }

        public void SetClippingThreshold(float threshold)
        {
            try
            {
                ShredEngineInterop.SetClippingThreshold(threshold);
                _logger.LogInformation("Clipping threshold set to {Threshold} via ShredEngine", threshold);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetClippingThreshold");
            }
        }

        public void SetCompressorRatio(float ratio)
        {
            try
            {
                ShredEngineInterop.SetCompressorRatio(ratio);
                _logger.LogInformation("Compressor ratio set to {Ratio}:1 via ShredEngine", ratio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetCompressorRatio");
            }
        }

        public void SetLimiterAttackTime(float attackMs)
        {
            try
            {
                ShredEngineInterop.SetLimiterAttackTime(attackMs);
                _logger.LogInformation("Limiter attack time set to {Attack}ms via ShredEngine", attackMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetLimiterAttackTime");
            }
        }

        public void SetLimiterReleaseTime(float releaseMs)
        {
            try
            {
                ShredEngineInterop.SetLimiterReleaseTime(releaseMs);
                _logger.LogInformation("Limiter release time set to {Release}ms via ShredEngine", releaseMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SetLimiterReleaseTime");
            }
        }

        // Monitoring getters
        public float GetCurrentPeakLevel()
        {
            try
            {
                return ShredEngineInterop.GetCurrentPeakLevel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetCurrentPeakLevel");
                return 0.0f;
            }
        }

        public float GetCurrentRmsLevel()
        {
            try
            {
                return ShredEngineInterop.GetCurrentRmsLevel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetCurrentRmsLevel");
                return 0.0f;
            }
        }

        public bool IsClipping()
        {
            try
            {
                return ShredEngineInterop.IsClipping();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in IsClipping");
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                ShredEngineInterop.ShutdownEngine();
                _logger.LogInformation("ShredEngine shutdown via Dispose");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to shutdown ShredEngine");
            }
        }

        // IAudioEngine interface implementations
        public void PlayTestTone(int deckNumber, double frequency, double duration)
        {
            LoadFile(deckNumber, @"C:\Users\rogue\Code\DJMixMaster\bin\Debug\net9.0-windows\..\..\..\assets\audio\ThisIsTrash.wav");
            Play(deckNumber);
        }
        public void UpdateAudioSettings(AudioSettings settings)
        {
            try
            {
                _logger.LogInformation("Updating audio settings in ShredEngine");

                // Apply clipping protection settings
                SetClippingProtectionEnabled(settings.EnableDeckVolumeCap || settings.EnablePeakDetection ||
                                           settings.EnableSoftKneeCompressor || settings.EnableLookAheadLimiter ||
                                           settings.EnableRmsMonitoring || settings.EnableAutoGainReduction ||
                                           settings.EnableBrickwallLimiter || settings.EnableClippingIndicator);

                SetDeckVolumeCapEnabled(settings.EnableDeckVolumeCap);
                SetPeakDetectionEnabled(settings.EnablePeakDetection);
                SetSoftKneeCompressorEnabled(settings.EnableSoftKneeCompressor);
                SetLookAheadLimiterEnabled(settings.EnableLookAheadLimiter);
                SetRmsMonitoringEnabled(settings.EnableRmsMonitoring);
                SetAutoGainReductionEnabled(settings.EnableAutoGainReduction);
                SetBrickwallLimiterEnabled(settings.EnableBrickwallLimiter);
                SetClippingIndicatorEnabled(settings.EnableClippingIndicator);

                // Apply thresholds if user configurable
                if (settings.EnableUserConfigurableThresholds)
                {
                    SetClippingThreshold(settings.ClippingThreshold);
                    SetCompressorRatio(settings.CompressorRatio);
                    SetLimiterAttackTime(settings.LimiterAttackTime * 1000); // Convert to ms
                    SetLimiterReleaseTime(settings.LimiterReleaseTime * 1000); // Convert to ms
                }

                _logger.LogInformation("Audio settings updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update audio settings");
            }
        }
        public AudioSettings GetCurrentSettings() => new AudioSettings();
        public void ShowAsioControlPanel() { }
        public object EnumerateDevices() => new List<object>();
        public void UpdateOutputDevice(string deviceName) { }
        public string GetSoundOutState() => "Rizz Engine Active";
        public int GetSampleRate(int deckNumber) => 44100;
        public float GetVolume(int deckNumber) => 1.0f;
        public bool IsPlaying(int deckNumber) => false;
        public object GetDeckProperties(int deckNumber) => null!;
        public float GetCrossfader() => 0.0f;
        public (float[] WaveformData, double TrackLength) GetWaveformData(int deckNumber) => (null!, 0);
        public void AddCuePoint(int deckNumber) { }
        public void JumpToCuePoint(int deckNumber, int cueIndex) { }
    }
}