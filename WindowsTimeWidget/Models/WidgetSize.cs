namespace WindowsTimeWidget.Models;

public enum WidgetSize
{
    Small,
    Medium,
    Large
}

public static class WidgetSizeExtensions
{
    public static (double Width, double Height) GetDimensions(this WidgetSize size) => size switch
    {
        WidgetSize.Small => (300, 80),
        WidgetSize.Medium => (350, 95),
        WidgetSize.Large => (400, 110),
        _ => (300, 80)
    };

    public static double GetFontSize(this WidgetSize size) => size switch
    {
        WidgetSize.Small => 22,
        WidgetSize.Medium => 32,
        WidgetSize.Large => 48,
        _ => 22
    };

    public static double GetDateFontSize(this WidgetSize size) => size switch
    {
        WidgetSize.Small => 14,
        WidgetSize.Medium => 18,
        WidgetSize.Large => 22,
        _ => 14
    };
}