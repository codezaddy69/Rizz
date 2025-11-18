using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using DJMixMaster.Audio;
using DJMixMaster.Visualization;
using Button = System.Windows.Controls.Button;

namespace DJMixMaster.UI.Handlers
{
    public class DeckEventHandler
    {
        private readonly ILogger<DeckEventHandler> _logger;
        private readonly IAudioEngine _audioEngine;
        private bool isLeftDeckPlaying = false;
        private bool isRightDeckPlaying = false;
        private bool isLeftDeckLoaded = false;
        private bool isRightDeckLoaded = false;
        private string? leftTrackName;
        private string? rightTrackName;

        // UI elements - would be passed in constructor or methods
        private WaveformVisualizer? leftWaveform;
        private WaveformVisualizer? rightWaveform;
        private TextBlock? leftTrackTitle;
        private TextBlock? leftTrackInfo;
        private TextBlock? rightTrackTitle;
        private TextBlock? rightTrackInfo;
        private Button? btnLeftPlay;
        private Button? btnRightPlay;
        private Slider? deck1PositionSlider;
        private Slider? deck2PositionSlider;

        public DeckEventHandler(ILogger<DeckEventHandler> logger, IAudioEngine audioEngine)
        {
            _logger = logger;
            _audioEngine = audioEngine;
        }

        public void SetUIElements(WaveformVisualizer? leftWaveform, WaveformVisualizer? rightWaveform,
            TextBlock? leftTrackTitle, TextBlock? leftTrackInfo, TextBlock? rightTrackTitle, TextBlock? rightTrackInfo,
            Button? btnLeftPlay, Button? btnRightPlay, Slider? deck1PositionSlider, Slider? deck2PositionSlider)
        {
            this.leftWaveform = leftWaveform;
            this.rightWaveform = rightWaveform;
            this.leftTrackTitle = leftTrackTitle;
            this.leftTrackInfo = leftTrackInfo;
            this.rightTrackTitle = rightTrackTitle;
            this.rightTrackInfo = rightTrackInfo;
            this.btnLeftPlay = btnLeftPlay;
            this.btnRightPlay = btnRightPlay;
            this.deck1PositionSlider = deck1PositionSlider;
            this.deck2PositionSlider = deck2PositionSlider;
        }

        public async Task LoadButton_Click(Button loadButton, int deckNumber)
        {
            try
            {
                Console.WriteLine($"Load button clicked for deck {deckNumber}");
                // Check if we're ejecting or loading
                if ((deckNumber == 1 && isLeftDeckLoaded) || (deckNumber == 2 && isRightDeckLoaded))
                {
                    // Eject the track
                    _logger.LogInformation($"Ejecting track from deck {deckNumber}");

                    // Stop playback if playing
                    if ((deckNumber == 1 && isLeftDeckPlaying) || (deckNumber == 2 && isRightDeckPlaying))
                    {
                        _audioEngine.Stop(deckNumber);
                        if (deckNumber == 1)
                        {
                            isLeftDeckPlaying = false;
                            if (btnLeftPlay != null) btnLeftPlay.Content = "PLAY";
                        }
                        else
                        {
                            isRightDeckPlaying = false;
                            if (btnRightPlay != null) btnRightPlay.Content = "PLAY";
                        }
                    }

                    // Reset deck state
                    if (deckNumber == 1)
                    {
                        isLeftDeckLoaded = false;
                        leftTrackName = null;
                        if (leftTrackTitle != null) leftTrackTitle.Text = "No Track Loaded";
                        if (leftTrackInfo != null) leftTrackInfo.Text = "";
                        leftWaveform?.UpdateWaveform(Array.Empty<float>(), 0);
                        if (deck1PositionSlider != null) deck1PositionSlider.Maximum = 0;
                    }
                    else
                    {
                        isRightDeckLoaded = false;
                        rightTrackName = null;
                        if (rightTrackTitle != null) rightTrackTitle.Text = "No Track Loaded";
                        if (rightTrackInfo != null) rightTrackInfo.Text = "";
                        rightWaveform?.UpdateWaveform(Array.Empty<float>(), 0);
                        if (deck2PositionSlider != null) deck2PositionSlider.Maximum = 0;
                    }

                    // Change button back to LOAD
                    loadButton.Content = "LOAD";
                }
                else
                {
                    // Load a new track
                    _logger.LogInformation("Load button clicked");
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = "Audio Files|*.mp3;*.wav|All Files|*.*"
                    };

                     if (openFileDialog.ShowDialog() == true)
                     {
                         // Check if deck is playing and confirm load
                         if (_audioEngine.IsPlaying(deckNumber))
                         {
                             var result = System.Windows.MessageBox.Show(
                                 $"Deck {deckNumber} is currently playing. Loading a new file will stop playback. Continue?",
                                 "Confirm File Load",
                                 System.Windows.MessageBoxButton.YesNo,
                                 System.Windows.MessageBoxImage.Warning);

                             if (result != System.Windows.MessageBoxResult.Yes)
                             {
                                 _logger.LogInformation("File load cancelled by user for playing deck {Deck}", deckNumber);
                                 return;
                             }
                         }

                         _logger.LogInformation($"Loading file for deck {deckNumber}: {openFileDialog.FileName}");

                         // Show loading state
                         loadButton.Content = "LOADING...";
                         loadButton.IsEnabled = false;

                        try
                        {
                            // Load file on background thread to avoid blocking UI
                            await Task.Run(() => _audioEngine.LoadFile(deckNumber, openFileDialog.FileName));
                            var (waveformData, trackLength) = _audioEngine.GetWaveformData(deckNumber);
                            var sampleRate = _audioEngine.GetSampleRate(deckNumber);
                            _logger.LogInformation($"Loaded file, length: {trackLength} seconds, sample rate: {sampleRate} Hz");

                            // Extract filename from path
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);

                            // Update UI on UI thread
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (deckNumber == 1)
                                {
                                    isLeftDeckLoaded = true;
                                    leftTrackName = fileName;
                                    if (leftTrackTitle != null) leftTrackTitle.Text = fileName;
                                    if (leftTrackInfo != null) leftTrackInfo.Text = $"{TimeSpan.FromSeconds(trackLength):mm\\:ss} @ {sampleRate}Hz";
                                    leftWaveform?.UpdateWaveform(waveformData, trackLength);
                                    if (deck1PositionSlider != null) deck1PositionSlider.Maximum = trackLength;
                                }
                                else
                                {
                                    isRightDeckLoaded = true;
                                    rightTrackName = fileName;
                                    if (rightTrackTitle != null) rightTrackTitle.Text = fileName;
                                    if (rightTrackInfo != null) rightTrackInfo.Text = $"{TimeSpan.FromSeconds(trackLength):mm\\:ss} @ {sampleRate}Hz";
                                    rightWaveform?.UpdateWaveform(waveformData, trackLength);
                                    if (deck2PositionSlider != null) deck2PositionSlider.Maximum = trackLength;
                                }

                                // Change button to EJECT
                                loadButton.Content = "EJECT";
                                loadButton.IsEnabled = true;
                            });
                        }
                        catch (Exception loadEx)
                        {
                            _logger.LogError($"Error loading file: {loadEx}");
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                loadButton.Content = "LOAD";
                                loadButton.IsEnabled = true;
                            });
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in load/eject operation: {ex}");
            }
        }

        public void TogglePlay(int deckNumber)
        {
            _logger.LogInformation("UI: TogglePlay called for deck {Deck}", deckNumber);

            if (deckNumber == 1)
            {
                isLeftDeckPlaying = !isLeftDeckPlaying;
                if (isLeftDeckPlaying)
                {
                    Console.WriteLine("Starting playback on deck 1");
                    Console.WriteLine($"SoundOut state before play: {_audioEngine.GetSoundOutState()}");
                    _audioEngine.Play(1);
                    Console.WriteLine($"Playback started on deck 1, output state: {_audioEngine.GetSoundOutState()}");
                    if (btnLeftPlay != null) btnLeftPlay.Content = "PAUSE";
                    _logger.LogInformation("UI: Deck 1 started playing");
                }
                else
                {
                    Console.WriteLine("Pausing playback on deck 1");
                    _audioEngine.Pause(1);
                    Console.WriteLine($"Playback paused on deck 1, output state: {_audioEngine.GetSoundOutState()}");
                    if (btnLeftPlay != null) btnLeftPlay.Content = "PLAY";
                    _logger.LogInformation("UI: Deck 1 paused");
                }
            }
            else if (deckNumber == 2)
            {
                isRightDeckPlaying = !isRightDeckPlaying;
                if (isRightDeckPlaying)
                {
                    Console.WriteLine("Starting playback on deck 2");
                    Console.WriteLine($"SoundOut state before play: {_audioEngine.GetSoundOutState()}");
                    _audioEngine.Play(2);
                    Console.WriteLine($"Playback started on deck 2, output state: {_audioEngine.GetSoundOutState()}");
                    if (btnRightPlay != null) btnRightPlay.Content = "PAUSE";
                    _logger.LogInformation("UI: Deck 2 started playing");
                }
                else
                {
                    Console.WriteLine("Pausing playback on deck 2");
                    _audioEngine.Pause(2);
                    Console.WriteLine($"Playback paused on deck 2, output state: {_audioEngine.GetSoundOutState()}");
                    if (btnRightPlay != null) btnRightPlay.Content = "PLAY";
                    _logger.LogInformation("UI: Deck 2 paused");
                }
            }
        }

        public void Rewind(int deckNumber)
        {
            var position = _audioEngine.GetPosition(deckNumber);
            var newPosition = Math.Max(0, position - 5);
            _audioEngine.Seek(deckNumber, newPosition);
        }

        public void FastForward(int deckNumber)
        {
            var position = _audioEngine.GetPosition(deckNumber);
            var length = _audioEngine.GetLength(deckNumber);
            var newPosition = Math.Min(length, position + 5);
            _audioEngine.Seek(deckNumber, newPosition);
        }

        public void UpdateVolume(int deckNumber, float volume)
        {
            _logger.LogDebug($"Volume slider for deck {deckNumber} changed to {volume}.");
            _logger.LogDebug($"UI setting volume for deck {deckNumber} to {volume}");
            _audioEngine.SetVolume(deckNumber, volume);
        }

        public void UpdateCrossfader(float position)
        {
            _audioEngine.SetCrossfader(position);
        }

        public void HandleCuePoint(int deckNumber, int cueIndex)
        {
            // If shift is held, set cue point, otherwise jump to it
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                _audioEngine.AddCuePoint(deckNumber);
                UpdateCueButtonStyle(deckNumber, cueIndex, true);
            }
            else
            {
                _audioEngine.JumpToCuePoint(deckNumber, cueIndex);
            }
        }

        private void UpdateCueButtonStyle(int deckNumber, int cueIndex, bool isSet)
        {
            // This would need UI elements passed or accessed differently
            // For now, placeholder
        }

        public void OnPlaybackPositionChanged(object? sender, (int DeckNumber, double Position) e)
        {
            if (e.DeckNumber == 1)
            {
                leftWaveform?.UpdatePlaybackPosition(e.Position);
            }
            else
            {
                rightWaveform?.UpdatePlaybackPosition(e.Position);
            }
        }

        public void OnBeatGridUpdated(object? sender, (int DeckNumber, double[] BeatPositions, double BPM) e)
        {
            // Use dispatcher to ensure UI updates happen on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.DeckNumber == 1)
                {
                    leftWaveform?.UpdateBeatGrid(e.BeatPositions);
                }
                else
                {
                    rightWaveform?.UpdateBeatGrid(e.BeatPositions);
                }
            });
        }

        public void UpdatePositionSlider(int deckNumber)
        {
            if (_audioEngine.IsPlaying(deckNumber))
            {
                var position = _audioEngine.GetPosition(deckNumber);
                var length = _audioEngine.GetLength(deckNumber);

                // For looping, show position within track length
                position = position % length;

                _logger.LogDebug("Updating position for deck {DeckNumber}: Position={Position:F2}s, Length={Length:F2}s", deckNumber, position, length);

                if (deckNumber == 1 && deck1PositionSlider != null)
                {
                    deck1PositionSlider.Value = position;
                    deck1PositionSlider.Maximum = length;
                }
                else if (deckNumber == 2 && deck2PositionSlider != null)
                {
                    deck2PositionSlider.Value = position;
                    deck2PositionSlider.Maximum = length;
                }
            }
        }

        public void OnPositionSliderValueChanged(Slider slider, double newValue)
        {
            int deckNumber = slider == deck1PositionSlider ? 1 : 2;
            _audioEngine.Seek(deckNumber, newValue);
        }
    }
}