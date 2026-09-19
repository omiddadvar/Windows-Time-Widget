using System.Text.Json.Serialization;

namespace WindowsTimeWidget.Models;

public class WidgetSettings
{
    public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
    public string WidgetColor { get; set; } = "#CC1E1E2E";
    public WidgetSize Size { get; set; } = WidgetSize.Medium;
    public WidgetLanguage Language { get; set; } = WidgetLanguage.English;
    public bool ShowBothDates { get; set; } = true;
    public double Opacity { get; set; } = 0.9;
    public bool Use24HourFormat { get; set; } = true;
    public bool ShowSeconds { get; set; } = true;
    public bool ShowDate { get; set; } = true;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(TimeZoneId)
                           && !string.IsNullOrWhiteSpace(WidgetColor)
                           && IsValidHexColor(WidgetColor);

    public WidgetSettings Clone() => (WidgetSettings)MemberwiseClone();


    private bool IsValidHexColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color) || color[0] != '#')
            return false;

        var hex = color.AsSpan(1);

        // Accept #RGB, #RGBA, #RRGGBB, #AARRGGBB
        if (hex.Length != 3 && hex.Length != 4 &&
            hex.Length != 6 && hex.Length != 8)
            return false;

        foreach (var c in hex)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }

        return true;
    }
}