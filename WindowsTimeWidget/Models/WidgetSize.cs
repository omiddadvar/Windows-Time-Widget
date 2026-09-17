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
        WidgetSize.Small => (200, 30),
        WidgetSize.Medium => (250, 40),
        WidgetSize.Large => (300, 50),
        _ => (320, 50)
    };

    public static double GetFontSize(this WidgetSize size) => size switch
    {
        WidgetSize.Small => 16,
        WidgetSize.Medium => 22,
        WidgetSize.Large => 32,
        _ => 22
    };

    public static double GetDateFontSize(this WidgetSize size) => size switch
    {
        WidgetSize.Small => 11,
        WidgetSize.Medium => 14,
        WidgetSize.Large => 18,
        _ => 14
    };
}