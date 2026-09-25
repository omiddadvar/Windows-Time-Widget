namespace WindowsTimeWidget.Abstractions;

/// <summary>
/// Marker interface for the background sync service.
/// Exposed mainly for DI / testing — the actual work lives in the BackgroundService base.
/// </summary>
public interface ITimeSyncService
{
    /// <summary>
    /// Triggers an immediate sync, outside the normal interval.
    /// </summary>
    void TriggerSync();

    /// <summary>
    /// The next scheduled sync time (UTC). Null if service is stopped.
    /// </summary>
    DateTime? NextSyncUtc { get; }
}