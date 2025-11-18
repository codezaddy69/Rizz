using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class SoundManager
    {
        private readonly ILogger<SoundManager> _logger;
        private readonly List<SoundDevice> _devices = new();
        private SoundDevice? _currentDevice;
        private bool _isInitialized;

        public SoundManager(ILogger<SoundManager> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                QueryDevices();
                _isInitialized = true;
                _logger.LogInformation("SoundManager initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SoundManager");
                throw;
            }
        }

        public void QueryDevices()
        {
            _devices.Clear();

            // Enumerate ASIO devices
            for (int i = 0; i < AsioOut.GetDriverNames().Length; i++)
            {
                var driverName = AsioOut.GetDriverNames()[i];
                var device = new SoundDevice(i, driverName, _logger);
                _devices.Add(device);
                _logger.LogInformation("ASIO Device {Index}: {Name}", i, driverName);
            }

            // Add WaveOut as fallback
            var waveOutDevice = new SoundDevice(-1, "WaveOut", _logger);
            _devices.Add(waveOutDevice);
            _logger.LogInformation("Added WaveOut fallback device");
        }

        public List<SoundDevice> GetDevices(bool outputDevices = true, bool inputDevices = false)
        {
            return _devices.Where(d => outputDevices).ToList(); // Simplified
        }

        public bool SetupDevice(SoundDevice device, int sampleRate, int bufferSize)
        {
            try
            {
                device.Open(sampleRate, bufferSize);
                _currentDevice = device;
                _logger.LogInformation("Device {Name} opened successfully", device.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup device {Name}", device.Name);
                return false;
            }
        }

        public void Shutdown()
        {
            if (_currentDevice != null)
            {
                _currentDevice.Close();
                _currentDevice = null;
            }
            _logger.LogInformation("SoundManager shutdown");
        }
    }

    public class SoundDevice
    {
        private readonly ILogger _logger;
        private IWavePlayer? _player;

        public int Index { get; }
        public string Name { get; }

        public SoundDevice(int index, string name, ILogger logger)
        {
            Index = index;
            Name = name;
            _logger = logger;
        }

        public void Open(int sampleRate, int bufferSize)
        {
            if (Index >= 0)
            {
                // ASIO
                _player = new AsioOut(Index);
            }
            else
            {
                // WaveOut
                _player = new WaveOutEvent();
            }
        }

        public void Close()
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
        }

        public IWavePlayer? Player => _player;
    }
}