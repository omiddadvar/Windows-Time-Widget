namespace WindowsTimeWidget.Services.BackgroundServices;

public record TimeSyncOptions
{
    public const string SectionName = "TimeSync";

    /// <summary>How often to re-sync with the API.</summary>
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Delay before the first sync after app start.</summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(3);
}