using System;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Native.JUCE
{
    public class JuceAudioEngine : IDisposable
    {
        private readonly ILogger<JuceAudioEngine> _logger;
        private IntPtr _deviceManager;
        private IntPtr _formatManager;
        private IntPtr _audioGraph;
        private IntPtr _currentTrack;
        private bool _isPlaying;
        private bool _disposed;

        public double CurrentPosition => JuceNative.GetPosition();
        public double Length => JuceNative.GetLength();
        public bool IsPlaying => JuceNative.IsPlaying();

        public JuceAudioEngine(ILogger<JuceAudioEngine> logger)
        {
            _logger = logger;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _logger.LogInformation("Initializing JUCE Audio Engine");
                
                // Create device and format managers
                _deviceManager = JuceNative.JUCE_CreateAudioDeviceManager();
                _formatManager = JuceNative.JUCE_CreateAudioFormatManager();
                _audioGraph = JuceNative.JUCE_CreateAudioProcessorGraph();

                if (_deviceManager == IntPtr.Zero || _formatManager == IntPtr.Zero || _audioGraph == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to initialize JUCE Audio Engine components");
                }

                // Initialize audio device with default settings
                const int numInputChannels = 2;  // Stereo input
                const int numOutputChannels = 2; // Stereo output
                const double sampleRate = 44100.0;
                const int bufferSize = 512;

                bool initialized = JuceNative.JUCE_InitializeAudioDevice(
                    _deviceManager,
                    "", // Empty string for default device
                    numInputChannels,
                    numOutputChannels,
                    sampleRate,
                    bufferSize
                );

                if (!initialized)
                {
                    throw new InvalidOperationException("Failed to initialize audio device");
                }

                _logger.LogInformation("JUCE Audio Engine initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing JUCE Audio Engine");
                throw;
            }
        }

        public bool LoadAudioFile(string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading audio file: {filePath}");

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError($"File not found: {filePath}");
                    return false;
                }

                // Create a new audio node for the track
                var newTrack = JuceNative.JUCE_CreateAudioFormatReader(_formatManager, filePath);
                if (newTrack == IntPtr.Zero)
                {
                    _logger.LogError("Failed to create audio format reader");
                    return false;
                }

                // If we had a previous track, clean it up
                if (_currentTrack != IntPtr.Zero)
                {
                    // TODO: Add cleanup function for audio format reader
                    _currentTrack = IntPtr.Zero;
                }

                _currentTrack = newTrack;

                // Connect the track to the audio graph
                var trackNode = JuceNative.JUCE_CreateAudioGraphNode(_audioGraph);
                if (trackNode == IntPtr.Zero)
                {
                    _logger.LogError("Failed to create audio graph node");
                    return false;
                }

                // Connect the track node to the output
                if (!JuceNative.JUCE_ConnectAudioNodes(_audioGraph, trackNode, 0, IntPtr.Zero, 0))
                {
                    _logger.LogError("Failed to connect audio nodes");
                    return false;
                }

                _logger.LogInformation("Audio file loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading audio file: {filePath}");
                return false;
            }
        }

        public void Play()
        {
            try
            {
                if (!_isPlaying && _currentTrack != IntPtr.Zero)
                {
                    _logger.LogInformation("Starting playback");
                    JuceNative.JUCE_StartPlayback(_deviceManager);
                    _isPlaying = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting playback");
                throw;
            }
        }

        public void Pause()
        {
            try
            {
                if (_isPlaying)
                {
                    _logger.LogInformation("Pausing playback");
                    JuceNative.JUCE_StopPlayback(_deviceManager);
                    _isPlaying = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing playback");
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                if (_isPlaying)
                {
                    _logger.LogInformation("Stopping playback");
                    JuceNative.JUCE_StopPlayback(_deviceManager);
                    JuceNative.JUCE_SetPlaybackPosition(_deviceManager, 0.0);
                    _isPlaying = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping playback");
                throw;
            }
        }

        public void SetPosition(double positionInSeconds)
        {
            try
            {
                _logger.LogInformation($"Setting position to {positionInSeconds} seconds");
                JuceNative.JUCE_SetPlaybackPosition(_deviceManager, positionInSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting position to {positionInSeconds}");
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_currentTrack != IntPtr.Zero)
                {
                    // TODO: Add cleanup function for audio format reader
                    _currentTrack = IntPtr.Zero;
                }

                if (_audioGraph != IntPtr.Zero)
                {
                    JuceNative.JUCE_DeleteAudioProcessorGraph(_audioGraph);
                    _audioGraph = IntPtr.Zero;
                }

                if (_formatManager != IntPtr.Zero)
                {
                    JuceNative.JUCE_DeleteAudioFormatManager(_formatManager);
                    _formatManager = IntPtr.Zero;
                }

                if (_deviceManager != IntPtr.Zero)
                {
                    JuceNative.JUCE_DeleteAudioDeviceManager(_deviceManager);
                    _deviceManager = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        ~JuceAudioEngine()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
