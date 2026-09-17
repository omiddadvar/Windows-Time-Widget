using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WindowsTimeWidget.Views.Converters;

public class ColorToHexConverter : IValueConverter
{
    private static readonly BrushConverter BrushConverter = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var brush = (SolidColorBrush)BrushConverter.ConvertFromString(NormalizeHex(hex))!;
                return brush.Color;
            }
            catch { }
        }
        return Color.FromArgb(0xCC, 0x1E, 0x1E, 0x2E);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color c)
            return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        return "#CC1E1E2E";
    }

    private static string NormalizeHex(string hex)
    {
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        if (hex.Length == 7) hex = "#CC" + hex[1..];
        return hex;
    }
}