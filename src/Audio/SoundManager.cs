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

            _logger.LogInformation("SoundManager initialization starting...");
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
            _logger.LogInformation("Querying audio devices...");

            // Enumerate ASIO devices first
            var asioDrivers = AsioOut.GetDriverNames();
            _logger.LogInformation("Found {Count} ASIO drivers", asioDrivers.Length);
            for (int i = 0; i < asioDrivers.Length; i++)
            {
                var driverName = asioDrivers[i];
                var device = new SoundDevice(i, driverName, _logger);
                _devices.Add(device);
                _logger.LogInformation("ASIO Device {Index}: {Name}", i, driverName);
            }

            // Add WaveOut as fallback (but prioritize ASIO)
            var waveOutDevice = new SoundDevice(-1, "WaveOut", _logger);
            _devices.Add(waveOutDevice);
            _logger.LogInformation("Added WaveOut fallback device");

            // Try to initialize first ASIO device to show systray icon
            if (_devices.Any(d => d.Index >= 0))
            {
                var firstAsio = _devices.First(d => d.Index >= 0);
                _logger.LogInformation("Attempting to initialize first ASIO device: {Name}", firstAsio.Name);
                try
                {
                    SetupDevice(firstAsio, 44100, 512);
                    _logger.LogInformation("ASIO device initialized - check systray for driver icon");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize ASIO device {Name}", firstAsio.Name);
                }
            }
        }

        public List<SoundDevice> GetDevices(bool outputDevices = true, bool inputDevices = false)
        {
            return _devices.Where(d => outputDevices).ToList(); // Simplified
        }

        public bool SetupDevice(SoundDevice device, int sampleRate, int bufferSize, ISampleProvider? input = null)
        {
            try
            {
                device.Open(sampleRate, bufferSize, input);
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

        public void Open(int sampleRate, int bufferSize, ISampleProvider? input = null)
        {
            if (Index >= 0)
            {
                // ASIO
                var asioOut = new AsioOut(Index);
                if (input != null)
                {
                    asioOut.Init(input);
                    asioOut.Play(); // Start playback
                }
                _player = asioOut;
            }
            else
            {
                // WaveOut
                var waveOut = new WaveOutEvent();
                if (input != null)
                {
                    waveOut.Init(input);
                    waveOut.Play(); // Start playback
                }
                _player = waveOut;
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