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

                _logger.LogInformation("Loading XAML components...");
                InitializeComponent();
                _logger.LogInformation("XAML components loaded successfully");

                _logger.LogInformation("Loading settings...");
                LoadSettings();

                _logger.LogInformation("Populating device list...");
                PopulateDeviceList();

                _logger.LogInformation("Initializing controls...");
                InitializeControls();

                _logger.LogInformation("AudioSettingsWindow initialization complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AudioSettingsWindow at stage: {Message}", ex.Message);
                _logger.LogError(ex, "Full exception details: {StackTrace}", ex.StackTrace);

                // Try to create a minimal fallback window
                try
                {
                    _logger.LogInformation("Attempting to create fallback window...");
                    CreateFallbackWindow();
                    _logger.LogInformation("Fallback window created successfully");
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback window creation also failed");
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

                _currentSettings.AvailableDevices.Clear();
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
                    _currentSettings.AvailableDevices.Add(deviceInfo);
                }

                _logger.LogInformation("Setting device list as ItemsSource...");
                DeviceComboBox.ItemsSource = _currentSettings.AvailableDevices;
                DeviceComboBox.DisplayMemberPath = "Name";

                // Select current device
                var currentDevice = _currentSettings.AvailableDevices
                    .FirstOrDefault(d => d.Id == _currentSettings.SelectedAsioDevice);
                if (currentDevice != null)
                {
                    _logger.LogInformation("Selecting current device: {Name}", currentDevice.Name);
                    DeviceComboBox.SelectedItem = currentDevice;
                }
                else if (_currentSettings.AvailableDevices.Any())
                {
                    _logger.LogInformation("No current device found, selecting first available");
                    DeviceComboBox.SelectedIndex = 0;
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
                _currentSettings.AvailableDevices.Add(new AsioDeviceInfo
                {
                    Id = "none",
                    Name = "No devices available",
                    Status = "Unavailable"
                });
                DeviceComboBox.ItemsSource = _currentSettings.AvailableDevices;
            }
        }

        private void InitializeControls()
        {
            try
            {
                _logger.LogInformation("Initializing control values...");

                // Device Selection
                if (BufferSizeSlider != null) BufferSizeSlider.Value = _currentSettings.BufferSize;
                if (BufferSizeTextBox != null) BufferSizeTextBox.Text = _currentSettings.BufferSize.ToString();
                if (SampleRateComboBox != null) SampleRateComboBox.Text = _currentSettings.SampleRate.ToString();

                // Crossfader
                if (CrossfaderCurveComboBox != null) CrossfaderCurveComboBox.Text = _currentSettings.CrossfaderCurve;
                if (CrossfaderRangeSlider != null) CrossfaderRangeSlider.Value = _currentSettings.CrossfaderRange;
                if (CrossfaderRangeTextBox != null) CrossfaderRangeTextBox.Text = (_currentSettings.CrossfaderRange * 100).ToString("F0");
                if (CrossfaderSensitivitySlider != null) CrossfaderSensitivitySlider.Value = _currentSettings.CrossfaderSensitivity;
                if (CrossfaderSensitivityTextBox != null) CrossfaderSensitivityTextBox.Text = (_currentSettings.CrossfaderSensitivity * 100).ToString("F0");
                if (CrossfaderModeComboBox != null) CrossfaderModeComboBox.Text = _currentSettings.CrossfaderMode;
                if (CenterDetentCheckBox != null) CenterDetentCheckBox.IsChecked = _currentSettings.CenterDetent;

                // Output Routing
                if (MasterOutputComboBox != null) MasterOutputComboBox.Text = _currentSettings.MasterOutputMode;
                if (CueOutputComboBox != null) CueOutputComboBox.Text = _currentSettings.CueOutputSource;
                if (BoothOutputComboBox != null) BoothOutputComboBox.Text = _currentSettings.BoothOutputSource;
                if (MasterLevelSlider != null) MasterLevelSlider.Value = _currentSettings.MasterOutputLevel;
                if (MasterLevelTextBox != null) MasterLevelTextBox.Text = _currentSettings.MasterOutputLevel.ToString("F1");
                if (HeadphoneMixSlider != null) HeadphoneMixSlider.Value = _currentSettings.HeadphoneMix;
                if (HeadphoneMixTextBox != null) HeadphoneMixTextBox.Text = (_currentSettings.HeadphoneMix * 100).ToString("F0");

                // Audio Processing
                if (AlwaysResampleCheckBox != null) AlwaysResampleCheckBox.IsChecked = _currentSettings.AlwaysResample;
                if (HardwareBufferCheckBox != null) HardwareBufferCheckBox.IsChecked = _currentSettings.UseHardwareBuffer;
                if (AllowPullModeCheckBox != null) AllowPullModeCheckBox.IsChecked = _currentSettings.AllowPullMode;
                if (Force16BitCheckBox != null) Force16BitCheckBox.IsChecked = _currentSettings.Force16Bit;

                // File & Playback
                if (AutoLoadCuesCheckBox != null) AutoLoadCuesCheckBox.IsChecked = _currentSettings.AutoLoadCuePoints;
                if (RememberPositionCheckBox != null) RememberPositionCheckBox.IsChecked = _currentSettings.RememberLastPosition;
                if (GaplessPlaybackCheckBox != null) GaplessPlaybackCheckBox.IsChecked = _currentSettings.GaplessPlayback;
                if (PreBufferSlider != null) PreBufferSlider.Value = _currentSettings.PreBufferTime;
                if (PreBufferTextBox != null) PreBufferTextBox.Text = _currentSettings.PreBufferTime.ToString("F1");
                if (MaxFileSizeSlider != null) MaxFileSizeSlider.Value = _currentSettings.MaxFileSize;
                if (MaxFileSizeTextBox != null) MaxFileSizeTextBox.Text = _currentSettings.MaxFileSize.ToString();

                // Interface
                if (ThemeComboBox != null) ThemeComboBox.Text = _currentSettings.Theme;
                if (WaveformZoomSlider != null) WaveformZoomSlider.Value = _currentSettings.WaveformZoom;
                if (WaveformZoomTextBox != null) WaveformZoomTextBox.Text = (_currentSettings.WaveformZoom * 100).ToString("F0");
                if (AutoScrollWaveformCheckBox != null) AutoScrollWaveformCheckBox.IsChecked = _currentSettings.AutoScrollWaveform;
                if (KeyboardLayoutComboBox != null) KeyboardLayoutComboBox.Text = _currentSettings.KeyboardLayout;
                if (ConfirmDeleteCheckBox != null) ConfirmDeleteCheckBox.IsChecked = _currentSettings.ConfirmOnDelete;

                _logger.LogInformation("Control initialization complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize controls");
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