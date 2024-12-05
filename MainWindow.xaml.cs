using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using DJMixMaster.Audio;
using DJMixMaster.Visualization;

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

            // Wire up button click events for left deck
            btnLeftLoad.Click += (s, e) => LoadTrack(1);
            btnLeftPlay.Click += (s, e) => TogglePlay(1);
            btnLeftRew.Click += (s, e) => Rewind(1);
            btnLeftFF.Click += (s, e) => FastForward(1);
            
            // Wire up button click events for right deck
            btnRightLoad.Click += (s, e) => LoadTrack(2);
            btnRightPlay.Click += (s, e) => TogglePlay(2);
            btnRightRew.Click += (s, e) => Rewind(2);
            btnRightFF.Click += (s, e) => FastForward(2);

            // Wire up volume and crossfader controls
            sliderLeftVolume.ValueChanged += (s, e) => UpdateVolume(1, (float)e.NewValue);
            sliderRightVolume.ValueChanged += (s, e) => UpdateVolume(2, (float)e.NewValue);
            sliderCrossfader.ValueChanged += (s, e) => UpdateCrossfader((float)e.NewValue);
        }

        private void LoadTrack(int deckNumber)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav;*.m4a;*.aac|All Files|*.*",
                Title = $"Select Track for Deck {deckNumber}"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                audioEngine.LoadTrack(deckNumber, openFileDialog.FileName);
                UpdateWaveform(deckNumber);
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
            // For now, seek back 5 seconds
            // Later we can implement variable speed seeking based on button hold duration
            audioEngine.Seek(deckNumber, TimeSpan.FromSeconds(-5));
        }

        private void FastForward(int deckNumber)
        {
            // For now, seek forward 5 seconds
            // Later we can implement variable speed seeking based on button hold duration
            audioEngine.Seek(deckNumber, TimeSpan.FromSeconds(5));
        }

        private void UpdateVolume(int deckNumber, float volume)
        {
            audioEngine.SetVolume(deckNumber, volume);
        }

        private void UpdateCrossfader(float position)
        {
            // Convert slider value (0 to 1) to crossfader range (-1 to 1)
            float crossfaderPosition = (position * 2) - 1;
            audioEngine.SetCrossfader(crossfaderPosition);
        }

        private void UpdateWaveform(int deckNumber)
        {
            var waveformData = audioEngine.GetWaveformData(deckNumber);
            if (deckNumber == 1)
            {
                leftWaveform.UpdateWaveform(waveformData);
            }
            else
            {
                rightWaveform.UpdateWaveform(waveformData);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            audioEngine.Dispose();
        }
    }
}