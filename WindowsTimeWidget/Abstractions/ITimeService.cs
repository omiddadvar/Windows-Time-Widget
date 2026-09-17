namespace WindowsTimeWidget.Abstractions;

public interface ITimeService
{
    /// <summary>
    /// Raised whenever the current time is refreshed (either via API sync or system fallback).
    /// </summary>
    event EventHandler<DateTime>? TimeUpdated;

    /// <summary>
    /// True when the most recent sync failed and system time is being used.
    /// </summary>
    bool IsUsingSystemTimeFallback { get; }

    /// <summary>
    /// UTC time of the last successful API sync, or null if never synced.
    /// </summary>
    DateTime? LastSuccessfulSyncUtc { get; }

    /// <summary>
    /// Fetches the current time for the given timezone from the API.
    /// Falls back to system time on failure.
    /// </summary>
    Task SyncFromApiAsync(string timeZoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current time in the requested timezone. If an API snapshot
    /// is available, projects forward from that snapshot; otherwise uses system time.
    /// </summary>
    DateTime GetCurrentTime(string timeZoneId);

    /// <summary>
    /// Forces the service to use system time (used when API is unreachable).
    /// </summary>
    void UseSystemTime();
}