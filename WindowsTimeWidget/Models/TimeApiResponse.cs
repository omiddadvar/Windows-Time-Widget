using System.Text.Json.Serialization;

namespace WindowsTimeWidget.Models;

public class TimeApiResponse
{
    [JsonPropertyName("date_time")]
    public string DateTimeString { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("day_of_week")]
    public string DayOfWeek { get; set; } = string.Empty;

    [JsonPropertyName("dst_active")]
    public bool DstActive { get; set; }

    [JsonPropertyName("timezone")]
    public string TimeZone { get; set; } = string.Empty;

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }


    public DateTimeOffset DateTimeOffset => DateTimeOffset.Parse(DateTimeString);
}