using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DJMixMaster.Converters
{
    public class BrushOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush && parameter is string opacityStr)
            {
                if (double.TryParse(opacityStr, out double opacity))
                {
                    Color color = brush.Color;
                    return new SolidColorBrush(Color.FromArgb(
                        (byte)(opacity * 255),
                        color.R,
                        color.G,
                        color.B
                    ));
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
