using System.IO;
using WindowsTimeWidget.Models;

namespace WindowsTimeWidget.Tests.TestData;

internal static class TestHarness
{
    public static WidgetSettings ValidSettings() => new()
    {
        TimeZoneId = TimeZoneInfo.Local.Id,
        WidgetColor = "#CC1E1E2E",
        Size = WidgetSize.Medium,
        Opacity = 0.9,
        Use24HourFormat = true,
        ShowSeconds = true,
        ShowDate = true,
        Language = WidgetLanguage.English,
        ShowBothDates = false
    };

    public static string RealSettingsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Mohaasaan", "DateTimeWidget");

    public static string RealSettingsFile => Path.Combine(RealSettingsFolder, "settings.json");
}
