using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DJMixMaster.Audio;

using DJMixMaster.UI.Handlers;
using DJMixMaster.Visualization;

namespace DJMixMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IAudioEngine? audioEngine;
        private ILogger? logger;
        private StreamWriter? logWriter;

        private DeckEventHandler? deckEventHandler;
        private DispatcherTimer? positionUpdateTimer;

        public MainWindow(App.AppOptions? options = null)
        {
            try
            {
                audioEngine = new RizzAudioEngine(LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug)).CreateLogger<RizzAudioEngine>());
                logger = LoggerFactory.Create(builder => builder.AddDebug()).CreateLogger<MainWindow>();
                logWriter = new System.IO.StreamWriter("logs/audio_engine.log", true);

                LogInfo("MainWindow constructor started");

                InitializeComponent();
                InitializeSliders();

                // Rizz handles ASIO setup internally

                // Create deck event handler
                var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
                deckEventHandler = new DeckEventHandler(loggerFactory.CreateLogger<DeckEventHandler>(), audioEngine);
                deckEventHandler.SetUIElements(leftWaveform, rightWaveform, leftTrackTitle, leftTrackInfo, rightTrackTitle, rightTrackInfo, btnLeftPlay, btnRightPlay, deck1PositionSlider, deck2PositionSlider);

                LogInfo("Using NAudio audio engine");

                // Open settings window automatically for testing
                // Dispatcher.InvokeAsync(() => OpenSettingsWindow());

                // Wire up audio engine events
                audioEngine.PlaybackPositionChanged += deckEventHandler.OnPlaybackPositionChanged;
                audioEngine.BeatGridUpdated += deckEventHandler.OnBeatGridUpdated;

                // Wire up button click events for left deck
                btnLeftLoad.Click += async (s, e) => await deckEventHandler.LoadButton_Click(btnLeftLoad, 1);
                btnLeftPlay.Click += (s, e) => deckEventHandler.TogglePlay(1);
                btnLeftRew.Click += (s, e) => deckEventHandler.Rewind(1);
                btnLeftFF.Click += (s, e) => deckEventHandler.FastForward(1);
                btnLeftTest.Click += (s, e) => audioEngine.PlayTestTone(1);

                // Wire up button click events for right deck
                btnRightLoad.Click += async (s, e) => await deckEventHandler.LoadButton_Click(btnRightLoad, 2);
                btnRightPlay.Click += (s, e) => deckEventHandler.TogglePlay(2);
                btnRightRew.Click += (s, e) => deckEventHandler.Rewind(2);
                btnRightFF.Click += (s, e) => deckEventHandler.FastForward(2);
                btnRightTest.Click += (s, e) => audioEngine.PlayTestTone(2);

                // Wire up volume and crossfader controls
                sliderLeftVolume.ValueChanged += (s, e) => deckEventHandler.UpdateVolume(1, (float)e.NewValue);
                sliderRightVolume.ValueChanged += (s, e) => deckEventHandler.UpdateVolume(2, (float)e.NewValue);
                sliderCrossfader.ValueChanged += (s, e) => deckEventHandler.UpdateCrossfader((float)e.NewValue);

                // Wire up hot cue buttons
                btnLeftCue1.Click += (s, e) => deckEventHandler.HandleCuePoint(1, 0);
                btnLeftCue2.Click += (s, e) => deckEventHandler.HandleCuePoint(1, 1);
                btnLeftCue3.Click += (s, e) => deckEventHandler.HandleCuePoint(1, 2);
                btnRightCue1.Click += (s, e) => deckEventHandler.HandleCuePoint(2, 0);
                btnRightCue2.Click += (s, e) => deckEventHandler.HandleCuePoint(2, 1);
                btnRightCue3.Click += (s, e) => deckEventHandler.HandleCuePoint(2, 2);

                // Wire up settings button
                btnSettings.Click += (s, e) => OpenSettingsWindow();

                // Wire up position slider events
                if (deck1PositionSlider != null)
                    deck1PositionSlider.ValueChanged += (s, e) => deckEventHandler.OnPositionSliderValueChanged(deck1PositionSlider, e.NewValue);
                if (deck2PositionSlider != null)
                    deck2PositionSlider.ValueChanged += (s, e) => deckEventHandler.OnPositionSliderValueChanged(deck2PositionSlider, e.NewValue);

                // Start timer to update position sliders (very infrequent to minimize audio interference)
                positionUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                positionUpdateTimer.Tick += (s, e) => { deckEventHandler.UpdatePositionSlider(1); deckEventHandler.UpdatePositionSlider(2); };
                positionUpdateTimer.Start();

                // Handle command-line options
                if (options?.AutoLoadFile != null && File.Exists(options.AutoLoadFile))
                {
                    LogInfo($"Auto-loading file: {options.AutoLoadFile}");
                    deckEventHandler.LoadFile(1, options.AutoLoadFile);
                    // Auto-play after a short delay to ensure system is ready
                    Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(() => deckEventHandler.Play(1)));
                }

                if (options?.TestTone == true)
                {
                    LogInfo("Playing test tone...");
                    // TODO: Implement test tone
                    Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() => PlayTestTone()));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing MainWindow: {ex}");
                Console.WriteLine($"Error initializing application: {ex.Message}");
            }
        }

        private void PlayTestTone()
        {
            try
            {
                audioEngine.PlayTestTone(1, 440, 5); // 440Hz for 5 seconds
                LogInfo("Test tone played");
            }
            catch (Exception ex)
            {
                LogError($"Test tone failed: {ex.Message}");
            }
        }

        private void OpenSettingsWindow()
        {
            AudioSettingsWindow? settingsWindow = null;

            try
            {
                LogInfo("Opening settings window...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Opening settings window...\n");

                // Check prerequisites
                if (audioEngine == null)
                {
                    throw new InvalidOperationException("AudioEngine is not initialized");
                }

                if (logger == null)
                {
                    throw new InvalidOperationException("Logger is not initialized");
                }

                LogInfo("Creating logger factory...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Creating logger factory...\n");
                var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
                if (loggerFactory == null)
                {
                    throw new InvalidOperationException("Failed to create logger factory");
                }

                LogInfo("Creating settings logger...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Creating settings logger...\n");
                var settingsLogger = loggerFactory.CreateLogger<AudioSettingsWindow>();
                if (settingsLogger == null)
                {
                    throw new InvalidOperationException("Failed to create settings logger");
                }

                LogInfo("Creating settings window...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Creating settings window...\n");
                settingsWindow = new AudioSettingsWindow(settingsLogger, audioEngine);
                if (settingsWindow == null)
                {
                    throw new InvalidOperationException("Settings window constructor returned null");
                }
                LogInfo("Settings window created successfully");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Settings window created successfully\n");

                LogInfo("Configuring window properties...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Configuring window properties...\n");
                // settingsWindow.Owner = this; // Removed for WSL compatibility
                // settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                LogInfo("Showing settings dialog...");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Showing settings dialog...\n");
                var result = settingsWindow.ShowDialog();

                LogInfo($"Settings window closed with result: {result}");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Settings window closed with result: {result}\n");
            }
            catch (Exception ex)
            {
                LogError($"Error opening settings window: {ex.Message}");
                File.AppendAllText("logs/debug.log", $"{DateTime.Now}: Error opening settings window: {ex.Message}\n{ex.StackTrace}\n");
                LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    LogError($"Inner exception: {ex.InnerException.Message}");
                    LogError($"Inner stack trace: {ex.InnerException.StackTrace}");
                }

                string errorDetails = $"Failed to open settings window: {ex.Message}\n\n";
                errorDetails += $"Exception Type: {ex.GetType().Name}\n";
                if (ex.InnerException != null)
                {
                    errorDetails += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                errorDetails += "Check application logs for more details.";

                Console.WriteLine(errorDetails);
            }
        }

        private void LogInfo(string message)
        {
            logger?.LogInformation(message);
            logWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} INFO: {message}");
            logWriter?.Flush();
        }

        private void LogError(string message)
        {
            logger?.LogError(message);
            logWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR: {message}");
            logWriter?.Flush();
        }

        private void InitializeSliders()
        {
            try
            {
                // Initialize volume sliders
                if (sliderLeftVolume != null)
                {
                    sliderLeftVolume.Minimum = 0;
                    sliderLeftVolume.Maximum = 1;
                    sliderLeftVolume.Value = 1;
                }
                if (sliderRightVolume != null)
                {
                    sliderRightVolume.Minimum = 0;
                    sliderRightVolume.Maximum = 1;
                    sliderRightVolume.Value = 1;
                }
                if (sliderCrossfader != null)
                {
                    sliderCrossfader.Minimum = -1;
                    sliderCrossfader.Maximum = 1;
                    sliderCrossfader.Value = 0;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing sliders: {ex}");
            }
        }
    }
}