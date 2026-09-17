using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WindowsTimeWidget.Views.Converters;

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            return new BrushConverter().ConvertFromString(value?.ToString() ?? "#CC1E1E2E")!;
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}