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
using DJMixMaster.UI.Initializers;
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
        private IConfiguration? configuration;
        private DeckEventHandler? deckEventHandler;
        private DispatcherTimer? positionUpdateTimer;

        public MainWindow()
        {
            try
            {
                var initializer = new AudioEngineInitializer(LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug)).CreateLogger<AudioEngineInitializer>());
                var initResult = initializer.Initialize();
                audioEngine = initResult.AudioEngine;
                logger = initResult.Logger;
                logWriter = initResult.LogWriter;
                configuration = initResult.Configuration;

                LogInfo("MainWindow constructor started");

                InitializeComponent();
                InitializeSliders();

                // Create deck event handler
                var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
                deckEventHandler = new DeckEventHandler(loggerFactory.CreateLogger<DeckEventHandler>(), audioEngine);
                deckEventHandler.SetUIElements(leftWaveform, rightWaveform, leftTrackTitle, leftTrackInfo, rightTrackTitle, rightTrackInfo, btnLeftPlay, btnRightPlay, deck1PositionSlider, deck2PositionSlider);

                LogInfo($"Using audio engine: {currentAudioEngineType}");

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

                // Wire up position slider events
                if (deck1PositionSlider != null)
                    deck1PositionSlider.ValueChanged += (s, e) => deckEventHandler.OnPositionSliderValueChanged(deck1PositionSlider, e.NewValue);
                if (deck2PositionSlider != null)
                    deck2PositionSlider.ValueChanged += (s, e) => deckEventHandler.OnPositionSliderValueChanged(deck2PositionSlider, e.NewValue);

                // Start timer to update position sliders (very infrequent to minimize audio interference)
                positionUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                positionUpdateTimer.Tick += (s, e) => { deckEventHandler.UpdatePositionSlider(1); deckEventHandler.UpdatePositionSlider(2); };
                positionUpdateTimer.Start();
            }
            catch (Exception ex)
            {
                LogError($"Error initializing MainWindow: {ex}");
                MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show($"Error initializing sliders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                LogInfo("Application closing");
                positionUpdateTimer?.Stop();
                base.OnClosed(e);
                audioEngine?.Dispose();
                logWriter?.Dispose();
            }
            catch (Exception ex)
            {
                LogError($"Error on close: {ex}");
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
    }
}