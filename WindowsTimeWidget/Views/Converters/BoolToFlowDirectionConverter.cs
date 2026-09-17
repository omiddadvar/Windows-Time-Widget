using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WindowsTimeWidget.Views.Converters;

public class BoolToFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => Binding.DoNothing;
}