using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using DJMixMaster.Audio;
using DJMixMaster.Visualization;
using System.Collections.Generic;

namespace DJMixMaster
{
    public partial class MainWindow : Window
    {
        private readonly AudioEngine audioEngine;
        private bool isLeftDeckPlaying = false;
        private bool isRightDeckPlaying = false;

        public MainWindow()
        {
            InitializeComponent();
            audioEngine = new AudioEngine();

            // Wire up audio engine events
            audioEngine.PlaybackPositionChanged += OnPlaybackPositionChanged;
            audioEngine.BeatGridUpdated += OnBeatGridUpdated;

            // Wire up button click events for left deck
            btnLeftLoad.Click += (s, e) => LoadButton_Click(s, e);
            btnLeftPlay.Click += (s, e) => TogglePlay(1);
            btnLeftRew.Click += (s, e) => Rewind(1);
            btnLeftFF.Click += (s, e) => FastForward(1);
            
            // Wire up button click events for right deck
            btnRightLoad.Click += (s, e) => LoadButton_Click(s, e);
            btnRightPlay.Click += (s, e) => TogglePlay(2);
            btnRightRew.Click += (s, e) => Rewind(2);
            btnRightFF.Click += (s, e) => FastForward(2);

            // Wire up volume and crossfader controls
            sliderLeftVolume.ValueChanged += (s, e) => UpdateVolume(1, (float)e.NewValue);
            sliderRightVolume.ValueChanged += (s, e) => UpdateVolume(2, (float)e.NewValue);
            sliderCrossfader.ValueChanged += (s, e) => UpdateCrossfader((float)e.NewValue);

            // Wire up hot cue buttons
            btnLeftCue1.Click += (s, e) => HandleCuePoint(1, 0);
            btnLeftCue2.Click += (s, e) => HandleCuePoint(1, 1);
            btnLeftCue3.Click += (s, e) => HandleCuePoint(1, 2);
            btnRightCue1.Click += (s, e) => HandleCuePoint(2, 0);
            btnRightCue2.Click += (s, e) => HandleCuePoint(2, 1);
            btnRightCue3.Click += (s, e) => HandleCuePoint(2, 2);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Audio Files|*.mp3;*.wav|All Files|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    int deckNumber = GetDeckNumber(sender);
                    audioEngine.LoadFile(deckNumber, openFileDialog.FileName);
                    var (waveformData, trackLength) = audioEngine.GetWaveformData(deckNumber);
                    
                    if (deckNumber == 1)
                    {
                        leftWaveform.UpdateWaveform(waveformData, trackLength);
                    }
                    else
                    {
                        rightWaveform.UpdateWaveform(waveformData, trackLength);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TogglePlay(int deckNumber)
        {
            if (deckNumber == 1)
            {
                isLeftDeckPlaying = !isLeftDeckPlaying;
                if (isLeftDeckPlaying)
                {
                    audioEngine.Play(1);
                    btnLeftPlay.Content = "⏸"; // Change to pause symbol
                }
                else
                {
                    audioEngine.Pause(1);
                    btnLeftPlay.Content = "PLAY";
                }
            }
            else
            {
                isRightDeckPlaying = !isRightDeckPlaying;
                if (isRightDeckPlaying)
                {
                    audioEngine.Play(2);
                    btnRightPlay.Content = "⏸"; // Change to pause symbol
                }
                else
                {
                    audioEngine.Pause(2);
                    btnRightPlay.Content = "PLAY";
                }
            }
        }

        private void Rewind(int deckNumber)
        {
            audioEngine.Seek(deckNumber, TimeSpan.FromSeconds(-5));
        }

        private void FastForward(int deckNumber)
        {
            audioEngine.Seek(deckNumber, TimeSpan.FromSeconds(5));
        }

        private void UpdateVolume(int deckNumber, float volume)
        {
            Logger.LogDebug($"Volume slider for deck {deckNumber} changed to {volume}.");
            Logger.LogDebug($"UI setting volume for deck {deckNumber} to {volume}");
            audioEngine.SetVolume(deckNumber, volume);
        }

        private void UpdateCrossfader(float position)
        {
            audioEngine.SetCrossfader(position);
        }

        private void HandleCuePoint(int deckNumber, int cueIndex)
        {
            // If shift is held, set cue point, otherwise jump to it
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                audioEngine.AddCuePoint(deckNumber);
                UpdateCueButtonStyle(deckNumber, cueIndex, true);
            }
            else
            {
                audioEngine.JumpToCuePoint(deckNumber, cueIndex);
            }
        }

        private void UpdateCueButtonStyle(int deckNumber, int cueIndex, bool isSet)
        {
            var button = deckNumber == 1 ? 
                new[] { btnLeftCue1, btnLeftCue2, btnLeftCue3 }[cueIndex] :
                new[] { btnRightCue1, btnRightCue2, btnRightCue3 }[cueIndex];

            if (isSet)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            }
        }

        private void OnPlaybackPositionChanged(object? sender, (int DeckNumber, double Position) e)
        {
            if (e.DeckNumber == 1)
            {
                leftWaveform.UpdatePlaybackPosition(e.Position);
            }
            else
            {
                rightWaveform.UpdatePlaybackPosition(e.Position);
            }
        }

        private void OnBeatGridUpdated(object? sender, (int DeckNumber, double[] BeatPositions, double BPM) e)
        {
            // Update beat grid visualization if needed
            if (e.DeckNumber == 1)
            {
                // Update left deck beat grid
                leftWaveform.UpdateBeatGrid(e.BeatPositions);
            }
            else
            {
                // Update right deck beat grid
                rightWaveform.UpdateBeatGrid(e.BeatPositions);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            audioEngine.Dispose();
        }

        private int GetDeckNumber(object sender)
        {
            if (sender == btnLeftLoad)
            {
                return 1;
            }
            else if (sender == btnRightLoad)
            {
                return 2;
            }
            else
            {
                throw new Exception("Unknown deck number");
            }
        }
    }
}