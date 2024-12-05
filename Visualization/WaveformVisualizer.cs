using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DJMixMaster.Visualization
{
    public class WaveformVisualizer : Canvas
    {
        private float[] waveformData;
        private readonly PathGeometry waveformPath;
        private readonly Path path;
        private readonly Brush strokeBrush;
        private double playbackPosition;

        public WaveformVisualizer()
        {
            waveformPath = new PathGeometry();
            path = new Path
            {
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Data = waveformPath
            };

            Children.Add(path);
        }

        public void UpdateWaveform(float[] data)
        {
            waveformData = data;
            DrawWaveform();
        }

        public void UpdatePlaybackPosition(double position)
        {
            playbackPosition = position;
            DrawPlaybackMarker();
        }

        private void DrawWaveform()
        {
            if (waveformData == null || waveformData.Length == 0) return;

            var figure = new PathFigure();
            var segments = new PathSegmentCollection();

            double width = ActualWidth;
            double height = ActualHeight;
            double centerY = height / 2;
            double scaleY = height / 2;

            for (int i = 0; i < waveformData.Length; i++)
            {
                double x = (width * i) / waveformData.Length;
                double y = centerY + (waveformData[i] * scaleY);

                if (i == 0)
                {
                    figure.StartPoint = new Point(x, y);
                }
                else
                {
                    segments.Add(new LineSegment(new Point(x, y), true));
                }
            }

            figure.Segments = segments;
            waveformPath.Figures = new PathFigureCollection { figure };
        }

        private void DrawPlaybackMarker()
        {
            // Add playback position marker
            var marker = new Line
            {
                X1 = ActualWidth * playbackPosition,
                Y1 = 0,
                X2 = ActualWidth * playbackPosition,
                Y2 = ActualHeight,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };

            Children.Clear();
            Children.Add(path);
            Children.Add(marker);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            DrawWaveform();
        }
    }
}
