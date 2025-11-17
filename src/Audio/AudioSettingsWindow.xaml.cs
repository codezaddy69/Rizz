using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Interaction logic for AudioSettingsWindow.xaml
    /// </summary>
    public partial class AudioSettingsWindow : Window
    {
        private readonly ILogger<AudioSettingsWindow> _logger;
        private readonly IAudioEngine _audioEngine;
        private AudioSettings _currentSettings = new();
        private AudioSettings _originalSettings = new();
        private readonly string _configPath = "settings/audio.json";

        public AudioSettingsWindow(ILogger<AudioSettingsWindow> logger, IAudioEngine audioEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));

            try
            {
                _logger.LogInformation("Initializing AudioSettingsWindow...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing AudioSettingsWindow...\n");

                _logger.LogInformation("Loading XAML components...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Loading XAML components...\n");
                InitializeComponent();
                _logger.LogInformation("XAML components loaded successfully");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: XAML components loaded successfully\n");

                _logger.LogInformation("Loading settings...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Loading settings...\n");
                LoadSettings();

                _logger.LogInformation("Populating device list...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Populating device list...\n");
                PopulateDeviceList();

                _logger.LogInformation("Initializing controls...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing controls...\n");
                InitializeControls();

                _logger.LogInformation("AudioSettingsWindow initialization complete");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: AudioSettingsWindow initialization complete\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AudioSettingsWindow at stage: {Message}", ex.Message);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Failed to initialize AudioSettingsWindow at stage: {ex.Message}\n{ex.StackTrace}\n");
                _logger.LogError(ex, "Full exception details: {StackTrace}", ex.StackTrace);

                // Try to create a minimal fallback window
                try
                {
                    _logger.LogInformation("Attempting to create fallback window...");
                    File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Attempting to create fallback window...\n");
                    CreateFallbackWindow();
                    _logger.LogInformation("Fallback window created successfully");
                    File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Fallback window created successfully\n");
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback window creation also failed");
                    File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Fallback window creation also failed: {fallbackEx.Message}\n{fallbackEx.StackTrace}\n");
                    throw new InvalidOperationException("Settings window initialization failed", ex);
                }
            }
        }

        private void CreateFallbackWindow()
        {
            // Create a minimal window with just basic controls
            this.Title = "Audio Settings (Limited Mode)";
            this.Width = 400;
            this.Height = 200;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Audio Settings (Limited Mode)",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10),
                Foreground = Brushes.White
            };
            Grid.SetRow(titleText, 0);

            var messageText = new TextBlock
            {
                Text = "Full settings interface failed to load.\nCheck application logs for details.\n\nAvailable features are limited.",
                Margin = new Thickness(10),
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(messageText, 1);

            var closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeButton.Click += (s, e) => this.Close();
            Grid.SetRow(closeButton, 2);

            grid.Children.Add(titleText);
            grid.Children.Add(messageText);
            grid.Children.Add(closeButton);

            this.Content = grid;
            this.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
        }

        private void LoadSettings()
        {
            try
            {
                _logger.LogInformation("Loading settings from {Path}", _configPath);

                if (File.Exists(_configPath))
                {
                    _logger.LogInformation("Settings file exists, reading content...");
                    string json = File.ReadAllText(_configPath);
                    _logger.LogDebug("JSON content length: {Length}", json.Length);

                    _logger.LogInformation("Deserializing settings...");
                    try
                    {
                        _currentSettings = JsonSerializer.Deserialize<AudioSettings>(json)!;
                        if (_currentSettings == null)
                        {
                            _logger.LogWarning("JSON deserialization returned null, using defaults");
                            _currentSettings = new AudioSettings();
                        }
                        else
                        {
                            _logger.LogInformation("Settings loaded successfully");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "JSON deserialization failed, content: {Content}", json);
                        throw;
                    }
                }
                else
                {
                    _logger.LogInformation("Settings file does not exist, using defaults");
                    _currentSettings = new AudioSettings();
                }

                // Create a deep copy for revert functionality
                _logger.LogInformation("Creating settings backup for revert functionality...");
                _originalSettings = JsonSerializer.Deserialize<AudioSettings>(
                    JsonSerializer.Serialize(_currentSettings)) ?? new AudioSettings();

                _logger.LogInformation("Settings initialization complete");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization failed - corrupted settings file");
                _currentSettings = new AudioSettings();
                _originalSettings = new AudioSettings();
                UpdateStatus("Settings file corrupted, using defaults");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "File I/O error loading settings");
                _currentSettings = new AudioSettings();
                _originalSettings = new AudioSettings();
                UpdateStatus("Cannot access settings file, using defaults");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading audio settings");
                _currentSettings = new AudioSettings();
                _originalSettings = new AudioSettings();
                UpdateStatus("Failed to load settings, using defaults");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configPath, json);
                _logger.LogInformation("Audio settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save audio settings");
                UpdateStatus("Failed to save settings");
            }
        }

        private void PopulateDeviceList()
        {
            try
            {
                _logger.LogInformation("Enumerating ASIO devices...");

                // Get available ASIO drivers
                _logger.LogInformation("Calling AsioOut.GetDriverNames()...");
                string[] driverNames;
                try
                {
                    driverNames = AsioOut.GetDriverNames();
                    _logger.LogInformation("AsioOut.GetDriverNames() succeeded, found {Count} drivers", driverNames.Length);
                }
                catch (Exception driverEx)
                {
                    _logger.LogError(driverEx, "AsioOut.GetDriverNames() failed");
                    throw new InvalidOperationException("ASIO driver enumeration failed", driverEx);
                }

                _currentSettings.AvailableAsioDevices.Clear();
                foreach (string driverName in driverNames)
                {
                    _logger.LogDebug("Adding ASIO device: {Name}", driverName);
                    var deviceInfo = new AsioDeviceInfo
                    {
                        Id = driverName,
                        Name = driverName,
                        DriverName = driverName,
                        Status = "Available"
                    };
                    _currentSettings.AvailableAsioDevices.Add(deviceInfo);
                }

                // Enumerate WaveOut devices
                _logger.LogInformation("Enumerating WaveOut devices...");
                Console.WriteLine($"WaveOut device count: {WaveOut.DeviceCount}");
                _currentSettings.AvailableWaveOutDevices.Clear();
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var caps = WaveOut.GetCapabilities(i);
                    bool isDefault = (i == 0); // Assume device 0 is default, or use Windows API
                    var waveOutInfo = new WaveOutDeviceInfo
                    {
                        DeviceNumber = i,
                        Name = caps.ProductName,
                        Channels = caps.Channels,
                        IsDefault = isDefault,
                        Status = "Available"
                    };
                    _currentSettings.AvailableWaveOutDevices.Add(waveOutInfo);
                    _logger.LogDebug("Adding WaveOut device: {Name}", caps.ProductName);
                    Console.WriteLine($"WaveOut device {i}: {caps.ProductName}, channels: {caps.Channels}");
                }

                // Add fallback WaveOut device if none found
                if (_currentSettings.AvailableWaveOutDevices.Count == 0)
                {
                    _logger.LogWarning("No WaveOut devices found, adding fallback");
                    _currentSettings.AvailableWaveOutDevices.Add(new WaveOutDeviceInfo
                    {
                        DeviceNumber = -1,
                        Name = "Default Audio (WaveOut)",
                        Channels = 2,
                        IsDefault = true,
                        Status = "Fallback"
                    });
                }

                // Set ASIO device list
                DeviceComboBox.ItemsSource = _currentSettings.AvailableAsioDevices;
                DeviceComboBox.DisplayMemberPath = "Name";

                // Select current ASIO device
                var currentAsioDevice = _currentSettings.AvailableAsioDevices
                    .FirstOrDefault(d => d.Id == _currentSettings.SelectedAsioDevice);
                if (currentAsioDevice != null)
                {
                    _logger.LogInformation("Selecting current ASIO device: {Name}", currentAsioDevice.Name);
                    DeviceComboBox.SelectedItem = currentAsioDevice;
                }
                else if (_currentSettings.AvailableAsioDevices.Any())
                {
                    _logger.LogInformation("No current ASIO device found, selecting first available");
                    DeviceComboBox.SelectedIndex = 0;
                }

                // Create combined output device list
                var allDevices = new List<CombinedDeviceInfo>();
                allDevices.AddRange(_currentSettings.AvailableAsioDevices.Select(d => new CombinedDeviceInfo { Type = "ASIO", Device = d, DisplayName = $"ASIO: {d.Name}" }));
                allDevices.AddRange(_currentSettings.AvailableWaveOutDevices.Select(d => new CombinedDeviceInfo { Type = "WaveOut", Device = d, DisplayName = $"WaveOut: {d.Name}{(d.IsDefault ? " [Default]" : "")}" }));

                Console.WriteLine($"Total devices in combo: {allDevices.Count}");
                foreach (var dev in allDevices)
                {
                    Console.WriteLine($"Device: {dev.DisplayName}");
                }

                OutputDeviceComboBox.ItemsSource = allDevices;
                OutputDeviceComboBox.DisplayMemberPath = "DisplayName";

                // Select current output device
                CombinedDeviceInfo? selectedOutput = null;
                if (_currentSettings.SelectedOutputDevice.StartsWith("ASIO:"))
                {
                    var asioId = _currentSettings.SelectedOutputDevice.Substring(5);
                    selectedOutput = allDevices.FirstOrDefault(d => d.Type == "ASIO" && ((AsioDeviceInfo)d.Device).Id == asioId);
                }
                else if (_currentSettings.SelectedOutputDevice.StartsWith("WaveOut:"))
                {
                    var waveOutNum = int.Parse(_currentSettings.SelectedOutputDevice.Substring(8));
                    selectedOutput = allDevices.FirstOrDefault(d => d.Type == "WaveOut" && ((WaveOutDeviceInfo)d.Device).DeviceNumber == waveOutNum);
                }
                else
                {
                    // Default to first WaveOut device
                    selectedOutput = allDevices.FirstOrDefault(d => d.Type == "WaveOut" && ((WaveOutDeviceInfo)d.Device).IsDefault);
                }
                if (selectedOutput != null)
                {
                    OutputDeviceComboBox.SelectedItem = selectedOutput;
                }
                else
                {
                    _logger.LogWarning("No ASIO devices available");
                    UpdateStatus("No ASIO devices found");
                }

                _logger.LogInformation("Device list population complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate ASIO devices");
                UpdateStatus("Failed to load ASIO devices");

                // Add a fallback empty device for UI
                _currentSettings.AvailableAsioDevices.Add(new AsioDeviceInfo
                {
                    Id = "none",
                    Name = "No devices available",
                    Status = "Unavailable"
                });
                DeviceComboBox.ItemsSource = _currentSettings.AvailableAsioDevices;
            }
        }

        private void InitializeControls()
        {
            try
            {
                _logger.LogInformation("Initializing control values...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing control values...\n");

                // Device Selection
                _logger.LogInformation("Initializing Device Selection controls...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing Device Selection controls...\n");
                _logger.LogInformation("DeviceComboBox exists: {Exists}", DeviceComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: DeviceComboBox exists: {DeviceComboBox != null}\n");
                _logger.LogInformation("BufferSizeSlider exists: {Exists}", BufferSizeSlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: BufferSizeSlider exists: {BufferSizeSlider != null}\n");
                _logger.LogInformation("BufferSizeTextBox exists: {Exists}", BufferSizeTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: BufferSizeTextBox exists: {BufferSizeTextBox != null}\n");
                _logger.LogInformation("SampleRateComboBox exists: {Exists}", SampleRateComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: SampleRateComboBox exists: {SampleRateComboBox != null}\n");
                if (BufferSizeSlider != null) BufferSizeSlider.Value = _currentSettings.BufferSize;
                if (BufferSizeTextBox != null) BufferSizeTextBox.Text = _currentSettings.BufferSize.ToString();
                if (SampleRateComboBox != null) SampleRateComboBox.Text = _currentSettings.SampleRate.ToString();
                _logger.LogInformation("Device Selection controls initialized");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Device Selection controls initialized\n");

                // Crossfader
                _logger.LogInformation("Initializing Crossfader controls...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing Crossfader controls...\n");
                _logger.LogInformation("CrossfaderCurveComboBox exists: {Exists}", CrossfaderCurveComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CrossfaderCurveComboBox exists: {CrossfaderCurveComboBox != null}\n");
                _logger.LogInformation("CrossfaderRangeSlider exists: {Exists}", CrossfaderRangeSlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CrossfaderRangeSlider exists: {CrossfaderRangeSlider != null}\n");
                _logger.LogInformation("CrossfaderRangeTextBox exists: {Exists}", CrossfaderRangeTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CrossfaderRangeTextBox exists: {CrossfaderRangeTextBox != null}\n");
                _logger.LogInformation("CrossfaderSensitivitySlider exists: {Exists}", CrossfaderSensitivitySlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CrossfaderSensitivitySlider exists: {CrossfaderSensitivitySlider != null}\n");
                _logger.LogInformation("CrossfaderSensitivityTextBox exists: {Exists}", CrossfaderSensitivityTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CrossfaderSensitivityTextBox exists: {CrossfaderSensitivityTextBox != null}\n");
                _logger.LogInformation("CrossfaderModeComboBox exists: {Exists}", CrossfaderModeComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CrossfaderModeComboBox exists: {CrossfaderModeComboBox != null}\n");
                _logger.LogInformation("CenterDetentCheckBox exists: {Exists}", CenterDetentCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CenterDetentCheckBox exists: {CenterDetentCheckBox != null}\n");
                if (CrossfaderCurveComboBox != null) CrossfaderCurveComboBox.Text = _currentSettings.CrossfaderCurve;
                if (CrossfaderRangeSlider != null) CrossfaderRangeSlider.Value = _currentSettings.CrossfaderRange;
                if (CrossfaderRangeTextBox != null) CrossfaderRangeTextBox.Text = (_currentSettings.CrossfaderRange * 100).ToString("F0");
                if (CrossfaderSensitivitySlider != null) CrossfaderSensitivitySlider.Value = _currentSettings.CrossfaderSensitivity;
                if (CrossfaderSensitivityTextBox != null) CrossfaderSensitivityTextBox.Text = (_currentSettings.CrossfaderSensitivity * 100).ToString("F0");
                if (CrossfaderModeComboBox != null) CrossfaderModeComboBox.Text = _currentSettings.CrossfaderMode;
                if (CenterDetentCheckBox != null) CenterDetentCheckBox.IsChecked = _currentSettings.CenterDetent;
                _logger.LogInformation("Crossfader controls initialized");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Crossfader controls initialized\n");

                // Output Routing
                _logger.LogInformation("Initializing Output Routing controls...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing Output Routing controls...\n");
                _logger.LogInformation("MasterOutputComboBox exists: {Exists}", MasterOutputComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: MasterOutputComboBox exists: {MasterOutputComboBox != null}\n");
                _logger.LogInformation("CueOutputComboBox exists: {Exists}", CueOutputComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: CueOutputComboBox exists: {CueOutputComboBox != null}\n");
                _logger.LogInformation("BoothOutputComboBox exists: {Exists}", BoothOutputComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: BoothOutputComboBox exists: {BoothOutputComboBox != null}\n");
                _logger.LogInformation("MasterLevelSlider exists: {Exists}", MasterLevelSlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: MasterLevelSlider exists: {MasterLevelSlider != null}\n");
                _logger.LogInformation("MasterLevelTextBox exists: {Exists}", MasterLevelTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: MasterLevelTextBox exists: {MasterLevelTextBox != null}\n");
                _logger.LogInformation("HeadphoneMixSlider exists: {Exists}", HeadphoneMixSlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: HeadphoneMixSlider exists: {HeadphoneMixSlider != null}\n");
                _logger.LogInformation("HeadphoneMixTextBox exists: {Exists}", HeadphoneMixTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: HeadphoneMixTextBox exists: {HeadphoneMixTextBox != null}\n");
                if (MasterOutputComboBox != null) MasterOutputComboBox.Text = _currentSettings.MasterOutputMode;
                if (CueOutputComboBox != null) CueOutputComboBox.Text = _currentSettings.CueOutputSource;
                if (BoothOutputComboBox != null) BoothOutputComboBox.Text = _currentSettings.BoothOutputSource;
                if (MasterLevelSlider != null) MasterLevelSlider.Value = _currentSettings.MasterOutputLevel;
                if (MasterLevelTextBox != null) MasterLevelTextBox.Text = _currentSettings.MasterOutputLevel.ToString("F1");
                if (HeadphoneMixSlider != null) HeadphoneMixSlider.Value = _currentSettings.HeadphoneMix;
                if (HeadphoneMixTextBox != null) HeadphoneMixTextBox.Text = (_currentSettings.HeadphoneMix * 100).ToString("F0");
                _logger.LogInformation("Output Routing controls initialized");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Output Routing controls initialized\n");

                // Audio Processing
                _logger.LogInformation("Initializing Audio Processing controls...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing Audio Processing controls...\n");
                _logger.LogInformation("AlwaysResampleCheckBox exists: {Exists}", AlwaysResampleCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: AlwaysResampleCheckBox exists: {AlwaysResampleCheckBox != null}\n");
                _logger.LogInformation("HardwareBufferCheckBox exists: {Exists}", HardwareBufferCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: HardwareBufferCheckBox exists: {HardwareBufferCheckBox != null}\n");
                _logger.LogInformation("AllowPullModeCheckBox exists: {Exists}", AllowPullModeCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: AllowPullModeCheckBox exists: {AllowPullModeCheckBox != null}\n");
                _logger.LogInformation("Force16BitCheckBox exists: {Exists}", Force16BitCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Force16BitCheckBox exists: {Force16BitCheckBox != null}\n");
                if (AlwaysResampleCheckBox != null) AlwaysResampleCheckBox.IsChecked = _currentSettings.AlwaysResample;
                if (HardwareBufferCheckBox != null) HardwareBufferCheckBox.IsChecked = _currentSettings.UseHardwareBuffer;
                if (AllowPullModeCheckBox != null) AllowPullModeCheckBox.IsChecked = _currentSettings.AllowPullMode;
                if (Force16BitCheckBox != null) Force16BitCheckBox.IsChecked = _currentSettings.Force16Bit;
                _logger.LogInformation("Audio Processing controls initialized");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Audio Processing controls initialized\n");

                // File & Playback
                _logger.LogInformation("Initializing File & Playback controls...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing File & Playback controls...\n");
                _logger.LogInformation("AutoLoadCuesCheckBox exists: {Exists}", AutoLoadCuesCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: AutoLoadCuesCheckBox exists: {AutoLoadCuesCheckBox != null}\n");
                _logger.LogInformation("RememberPositionCheckBox exists: {Exists}", RememberPositionCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: RememberPositionCheckBox exists: {RememberPositionCheckBox != null}\n");
                _logger.LogInformation("GaplessPlaybackCheckBox exists: {Exists}", GaplessPlaybackCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: GaplessPlaybackCheckBox exists: {GaplessPlaybackCheckBox != null}\n");
                _logger.LogInformation("PreBufferSlider exists: {Exists}", PreBufferSlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: PreBufferSlider exists: {PreBufferSlider != null}\n");
                _logger.LogInformation("PreBufferTextBox exists: {Exists}", PreBufferTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: PreBufferTextBox exists: {PreBufferTextBox != null}\n");
                _logger.LogInformation("MaxFileSizeSlider exists: {Exists}", MaxFileSizeSlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: MaxFileSizeSlider exists: {MaxFileSizeSlider != null}\n");
                _logger.LogInformation("MaxFileSizeTextBox exists: {Exists}", MaxFileSizeTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: MaxFileSizeTextBox exists: {MaxFileSizeTextBox != null}\n");
                if (AutoLoadCuesCheckBox != null) AutoLoadCuesCheckBox.IsChecked = _currentSettings.AutoLoadCuePoints;
                if (RememberPositionCheckBox != null) RememberPositionCheckBox.IsChecked = _currentSettings.RememberLastPosition;
                if (GaplessPlaybackCheckBox != null) GaplessPlaybackCheckBox.IsChecked = _currentSettings.GaplessPlayback;
                if (PreBufferSlider != null) PreBufferSlider.Value = _currentSettings.PreBufferTime;
                if (PreBufferTextBox != null) PreBufferTextBox.Text = _currentSettings.PreBufferTime.ToString("F1");
                if (MaxFileSizeSlider != null) MaxFileSizeSlider.Value = _currentSettings.MaxFileSize;
                if (MaxFileSizeTextBox != null) MaxFileSizeTextBox.Text = _currentSettings.MaxFileSize.ToString();
                _logger.LogInformation("File & Playback controls initialized");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: File & Playback controls initialized\n");

                // Interface
                _logger.LogInformation("Initializing Interface controls...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Initializing Interface controls...\n");
                _logger.LogInformation("ThemeComboBox exists: {Exists}", ThemeComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: ThemeComboBox exists: {ThemeComboBox != null}\n");
                _logger.LogInformation("WaveformZoomSlider exists: {Exists}", WaveformZoomSlider != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: WaveformZoomSlider exists: {WaveformZoomSlider != null}\n");
                _logger.LogInformation("WaveformZoomTextBox exists: {Exists}", WaveformZoomTextBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: WaveformZoomTextBox exists: {WaveformZoomTextBox != null}\n");
                _logger.LogInformation("AutoScrollWaveformCheckBox exists: {Exists}", AutoScrollWaveformCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: AutoScrollWaveformCheckBox exists: {AutoScrollWaveformCheckBox != null}\n");
                _logger.LogInformation("KeyboardLayoutComboBox exists: {Exists}", KeyboardLayoutComboBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: KeyboardLayoutComboBox exists: {KeyboardLayoutComboBox != null}\n");
                _logger.LogInformation("ConfirmDeleteCheckBox exists: {Exists}", ConfirmDeleteCheckBox != null);
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: ConfirmDeleteCheckBox exists: {ConfirmDeleteCheckBox != null}\n");
                if (ThemeComboBox != null) ThemeComboBox.Text = _currentSettings.Theme;
                if (WaveformZoomSlider != null) WaveformZoomSlider.Value = _currentSettings.WaveformZoom;
                if (WaveformZoomTextBox != null) WaveformZoomTextBox.Text = (_currentSettings.WaveformZoom * 100).ToString("F0");
                if (AutoScrollWaveformCheckBox != null) AutoScrollWaveformCheckBox.IsChecked = _currentSettings.AutoScrollWaveform;
                if (KeyboardLayoutComboBox != null) KeyboardLayoutComboBox.Text = _currentSettings.KeyboardLayout;
                if (ConfirmDeleteCheckBox != null) ConfirmDeleteCheckBox.IsChecked = _currentSettings.ConfirmOnDelete;
                _logger.LogInformation("Interface controls initialized");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Interface controls initialized\n");

                _logger.LogInformation("Control initialization complete");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Control initialization complete\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize controls");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Failed to initialize controls: {ex.Message}\n{ex.StackTrace}\n");
                UpdateStatus("Control initialization failed");
            }
        }

        private void UpdateSettingsFromControls()
        {
            // Device Selection
            if (DeviceComboBox?.SelectedItem is AsioDeviceInfo selectedDevice)
            {
                _currentSettings.SelectedAsioDevice = selectedDevice.Id;
            }
            if (BufferSizeSlider != null)
            {
                _currentSettings.BufferSize = (int)BufferSizeSlider.Value;
            }
            if (SampleRateComboBox != null && int.TryParse(SampleRateComboBox.Text, out int sampleRate))
            {
                _currentSettings.SampleRate = sampleRate;
            }

            // Crossfader
            if (CrossfaderCurveComboBox != null) _currentSettings.CrossfaderCurve = CrossfaderCurveComboBox.Text;
            if (CrossfaderRangeSlider != null) _currentSettings.CrossfaderRange = (float)CrossfaderRangeSlider.Value;
            if (CrossfaderSensitivitySlider != null) _currentSettings.CrossfaderSensitivity = (float)CrossfaderSensitivitySlider.Value;
            if (CrossfaderModeComboBox != null) _currentSettings.CrossfaderMode = CrossfaderModeComboBox.Text;
            _currentSettings.CenterDetent = CenterDetentCheckBox?.IsChecked ?? false;

            // Output Routing
            if (MasterOutputComboBox != null) _currentSettings.MasterOutputMode = MasterOutputComboBox.Text;
            if (CueOutputComboBox != null) _currentSettings.CueOutputSource = CueOutputComboBox.Text;
            if (BoothOutputComboBox != null) _currentSettings.BoothOutputSource = BoothOutputComboBox.Text;
            if (MasterLevelSlider != null) _currentSettings.MasterOutputLevel = (float)MasterLevelSlider.Value;
            if (HeadphoneMixSlider != null) _currentSettings.HeadphoneMix = (float)HeadphoneMixSlider.Value;

            // Audio Processing
            _currentSettings.AlwaysResample = AlwaysResampleCheckBox?.IsChecked ?? false;
            _currentSettings.UseHardwareBuffer = HardwareBufferCheckBox?.IsChecked ?? false;
            _currentSettings.AllowPullMode = AllowPullModeCheckBox?.IsChecked ?? false;
            _currentSettings.Force16Bit = Force16BitCheckBox?.IsChecked ?? false;

            // File & Playback
            _currentSettings.AutoLoadCuePoints = AutoLoadCuesCheckBox?.IsChecked ?? false;
            _currentSettings.RememberLastPosition = RememberPositionCheckBox?.IsChecked ?? false;
            _currentSettings.GaplessPlayback = GaplessPlaybackCheckBox?.IsChecked ?? false;
            if (PreBufferSlider != null) _currentSettings.PreBufferTime = (float)PreBufferSlider.Value;
            if (MaxFileSizeSlider != null) _currentSettings.MaxFileSize = (int)MaxFileSizeSlider.Value;

            // Interface
            if (ThemeComboBox != null) _currentSettings.Theme = ThemeComboBox.Text;
            if (WaveformZoomSlider != null) _currentSettings.WaveformZoom = (float)WaveformZoomSlider.Value;
            _currentSettings.AutoScrollWaveform = AutoScrollWaveformCheckBox?.IsChecked ?? false;
            if (KeyboardLayoutComboBox != null) _currentSettings.KeyboardLayout = KeyboardLayoutComboBox.Text;
            _currentSettings.ConfirmOnDelete = ConfirmDeleteCheckBox?.IsChecked ?? false;
        }

        private void ApplySettings()
        {
            try
            {
                UpdateSettingsFromControls();

                // Validate settings
                if (!ValidateSettings())
                {
                    return;
                }

                // Apply to audio engine
                _audioEngine.UpdateAudioSettings(_currentSettings);

                // Save to file
                SaveSettings();

                UpdateStatus("Settings applied successfully");
                _logger.LogInformation("Audio settings applied and saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply audio settings");
                UpdateStatus("Failed to apply settings");
            }
        }

        private bool ValidateSettings()
        {
            // Validate buffer size
            if (_currentSettings.BufferSize < 64 || _currentSettings.BufferSize > 2048)
            {
                UpdateStatus("Buffer size must be between 64 and 2048 samples");
                return false;
            }

            // Validate sample rate
            if (_currentSettings.SampleRate != 44100 && _currentSettings.SampleRate != 48000 &&
                _currentSettings.SampleRate != 96000)
            {
                UpdateStatus("Sample rate must be 44100, 48000, or 96000 Hz");
                return false;
            }

            return true;
        }

        private void RevertToDefaults()
        {
            _currentSettings = new AudioSettings();
            InitializeControls();
            UpdateStatus("Reverted to default settings");
        }

        private void UpdateStatus(string message)
        {
            if (StatusTextBlock != null)
            {
                StatusTextBlock.Text = message;
            }
        }

        // Event Handlers
        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStatus("Device selection changed - restart may be required");
        }

        private void ApplyDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = OutputDeviceComboBox.SelectedItem as CombinedDeviceInfo;
                if (selectedItem == null)
                {
                    UpdateStatus("No device selected");
                    return;
                }
                string deviceId;
                if (selectedItem.Type == "ASIO")
                {
                    var asioDevice = (AsioDeviceInfo)selectedItem.Device;
                    deviceId = $"ASIO:{asioDevice.Id}";
                    Console.WriteLine($"Switching to ASIO device: {asioDevice.Name}");
                }
                else
                {
                    var waveOutDevice = (WaveOutDeviceInfo)selectedItem.Device;
                    deviceId = $"WaveOut:{waveOutDevice.DeviceNumber}";
                    Console.WriteLine($"Switching to WaveOut device: {waveOutDevice.Name}");
                }

                _currentSettings.SelectedOutputDevice = deviceId;
                _audioEngine.UpdateOutputDevice(deviceId);
                UpdateStatus("Output device updated successfully");
                Console.WriteLine($"Output device updated to: {deviceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply device change");
                UpdateStatus($"Failed to apply device: {ex.Message}");
                Console.WriteLine($"Error applying device: {ex.Message}");
            }
        }

        private void BufferSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BufferSizeTextBox != null)
            {
                BufferSizeTextBox.Text = ((int)e.NewValue).ToString();
            }
        }

        private void CrossfaderRangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CrossfaderRangeTextBox != null)
            {
                CrossfaderRangeTextBox.Text = (e.NewValue * 100).ToString("F0");
            }
        }

        private void CrossfaderSensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CrossfaderSensitivityTextBox != null)
            {
                CrossfaderSensitivityTextBox.Text = (e.NewValue * 100).ToString("F0");
            }
        }

        private void MasterLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MasterLevelTextBox != null)
            {
                MasterLevelTextBox.Text = e.NewValue.ToString("F1");
            }
        }

        private void HeadphoneMixSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HeadphoneMixTextBox != null)
            {
                HeadphoneMixTextBox.Text = (e.NewValue * 100).ToString("F0");
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Revert all settings to defaults?", "Confirm Revert",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                RevertToDefaults();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Check for unsaved changes
            UpdateSettingsFromControls();
            if (!SettingsEqual(_currentSettings, _originalSettings))
            {
                var result = MessageBox.Show("You have unsaved changes. Apply them before closing?",
                                           "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ApplySettings();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            Close();
        }

        private bool SettingsEqual(AudioSettings a, AudioSettings b)
        {
            // Simple comparison - in production, use a more robust method
            return JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);
        }
    }
}