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
            string colorString = string.IsNullOrEmpty(value?.ToString() ?? string.Empty)
                ? "#CC1E1E2E" :
                value!.ToString()!;
            return new BrushConverter().ConvertFromString(colorString)!;
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}