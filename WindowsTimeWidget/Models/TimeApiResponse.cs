using System.Text.Json.Serialization;

namespace WindowsTimeWidget.Models;

public class TimeApiResponse
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    [JsonPropertyName("minute")]
    public int Minute { get; set; }

    [JsonPropertyName("seconds")]
    public int Seconds { get; set; }

    [JsonPropertyName("milliSeconds")]
    public int MilliSeconds { get; set; }

    [JsonPropertyName("dateTime")]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = string.Empty;

    [JsonPropertyName("dayOfWeek")]
    public string DayOfWeek { get; set; } = string.Empty;

    [JsonPropertyName("dstActive")]
    public bool DstActive { get; set; }
}