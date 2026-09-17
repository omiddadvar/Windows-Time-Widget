namespace WindowsTimeWidget.Models;

public class TimeZoneItem
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public TimeSpan Offset { get; init; }

    public string FullDisplay => $"{DisplayName} ({FormatOffset(Offset)})";

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var abs = offset.Duration();
        return $"UTC{sign}{abs.Hours:D2}:{abs.Minutes:D2}";
    }
}