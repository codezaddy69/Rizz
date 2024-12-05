using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DJMixMaster.Visualization
{
    public class WaveformVisualizer : Canvas
    {
        private float[]? waveformData;
        private readonly PathGeometry waveformPath;
        private readonly Path waveformShape;
        private readonly Line playbackLine;
        private readonly List<Line> beatLines;
        private readonly List<Line> cuePoints;
        private double zoomFactor = 1.0;
        private double viewportStartTime = 0.0;
        private double currentPlaybackTime = 0.0;
        private double trackLengthSeconds = 0.0;

        private const double VIEWPORT_SECONDS = 5.0; // Show 5 seconds of audio
        private const double PIXELS_PER_SECOND = 100.0; // Base scale before zoom

        public WaveformVisualizer()
        {
            waveformPath = new PathGeometry();
            waveformShape = new Path
            {
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Data = waveformPath
            };

            playbackLine = new Line
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                X1 = 0,
                X2 = 0,
                Y1 = 0,
                Opacity = 0.8
            };

            beatLines = new List<Line>();
            cuePoints = new List<Line>();

            Children.Add(waveformShape);
            Children.Add(playbackLine);

            // Enable scrolling for navigation
            MouseWheel += OnMouseWheel;
        }

        private void OnMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Adjust zoom factor based on mouse wheel
            if (e.Delta > 0)
                zoomFactor *= 1.1;
            else
                zoomFactor /= 1.1;

            zoomFactor = Math.Clamp(zoomFactor, 0.1, 10.0);
            DrawWaveform();
        }

        public void UpdateWaveform(float[] data, double trackLength)
        {
            waveformData = data;
            trackLengthSeconds = trackLength;
            DrawWaveform();
        }

        public void UpdatePlaybackPosition(double timeSeconds)
        {
            currentPlaybackTime = timeSeconds;

            // Auto-scroll viewport if playback position is outside visible range
            if (currentPlaybackTime < viewportStartTime || 
                currentPlaybackTime > viewportStartTime + (VIEWPORT_SECONDS / zoomFactor))
            {
                viewportStartTime = currentPlaybackTime - (VIEWPORT_SECONDS / zoomFactor / 2);
                viewportStartTime = Math.Max(0, Math.Min(viewportStartTime, 
                    trackLengthSeconds - (VIEWPORT_SECONDS / zoomFactor)));
            }

            DrawWaveform();
        }

        public void UpdateBeatGrid(List<double> beatPositions)
        {
            // Clear existing beat lines
            foreach (var line in beatLines)
            {
                Children.Remove(line);
            }
            beatLines.Clear();

            // Create new beat lines
            foreach (var beatPos in beatPositions)
            {
                var beatLine = new Line
                {
                    Stroke = Brushes.LightBlue,
                    StrokeThickness = 1,
                    Opacity = 0.5,
                    Y1 = 0
                };
                beatLines.Add(beatLine);
                Children.Add(beatLine);
            }

            DrawWaveform();
        }

        public void AddCuePoint(double timeSeconds)
        {
            var cueLine = new Line
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 2,
                Opacity = 0.8,
                Y1 = 0
            };
            cuePoints.Add(cueLine);
            Children.Add(cueLine);
            DrawWaveform();
        }

        private void DrawWaveform()
        {
            if (waveformData == null || waveformData.Length == 0) return;

            double width = ActualWidth;
            double height = ActualHeight;
            double centerY = height / 2;
            double scaleY = height / 2;

            // Calculate visible sample range
            double samplesPerSecond = waveformData.Length / trackLengthSeconds;
            int startSample = (int)(viewportStartTime * samplesPerSecond);
            int visibleSamples = (int)((VIEWPORT_SECONDS / zoomFactor) * samplesPerSecond);
            
            // Create waveform path
            var figure = new PathFigure();
            var segments = new PathSegmentCollection();

            for (int i = 0; i < visibleSamples && (startSample + i) < waveformData.Length; i++)
            {
                double x = (width * i) / visibleSamples;
                double y = centerY + (waveformData[startSample + i] * scaleY);

                if (i == 0)
                    figure.StartPoint = new Point(x, y);
                else
                    segments.Add(new LineSegment(new Point(x, y), true));
            }

            figure.Segments = segments;
            waveformPath.Figures = new PathFigureCollection { figure };

            // Update playback line position
            double playbackX = ((currentPlaybackTime - viewportStartTime) / (VIEWPORT_SECONDS / zoomFactor)) * width;
            playbackLine.X1 = playbackX;
            playbackLine.X2 = playbackX;
            playbackLine.Y2 = height;

            // Update beat lines
            foreach (var beatLine in beatLines)
            {
                double beatTime = beatLines.IndexOf(beatLine); // Time in seconds
                double beatX = ((beatTime - viewportStartTime) / (VIEWPORT_SECONDS / zoomFactor)) * width;
                beatLine.X1 = beatX;
                beatLine.X2 = beatX;
                beatLine.Y2 = height;
            }

            // Update cue points
            foreach (var cueLine in cuePoints)
            {
                double cueTime = cuePoints.IndexOf(cueLine); // Time in seconds
                double cueX = ((cueTime - viewportStartTime) / (VIEWPORT_SECONDS / zoomFactor)) * width;
                cueLine.X1 = cueX;
                cueLine.X2 = cueX;
                cueLine.Y2 = height;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            DrawWaveform();
        }
    }
}
