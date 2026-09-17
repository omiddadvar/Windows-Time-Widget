using System.Net.Http;
using System.Net.Http.Json;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Models;

namespace WindowsTimeWidget.Services;

public class TimeService : ITimeService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private DateTime _apiSnapshotUtc;
    private DateTime _apiLocalSnapshot;
    private bool _hasApiSnapshot;
    private bool _isUsingSystemTimeFallback = true;

    public TimeService(
        HttpClient httpClient,
        ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public event EventHandler<DateTime>? TimeUpdated;

    public bool IsUsingSystemTimeFallback => _isUsingSystemTimeFallback;
    public DateTime? LastSuccessfulSyncUtc => _hasApiSnapshot ? _apiSnapshotUtc : null;

    public async Task SyncFromApiAsync(string timeZoneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            timeZoneId = TimeZoneInfo.Local.Id;

        // Prevent overlapping syncs
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var url = $"api/v1/time/current/zone?timeZone={Uri.EscapeDataString(timeZoneId)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TimeApiResponse>(
                cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("API returned empty payload.");

            // Store snapshot
            _apiLocalSnapshot = payload.DateTime;
            _apiSnapshotUtc = DateTime.SpecifyKind(payload.DateTime, DateTimeKind.Unspecified)
                                     .Subtract(GetOffset(timeZoneId))
                                     .ToUniversalTime();
            _hasApiSnapshot = true;
            _isUsingSystemTimeFallback = false;

            TimeUpdated?.Invoke(this, GetCurrentTime(timeZoneId));
        }
        catch (Exception ex)
        {
            UseSystemTime();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public DateTime GetCurrentTime(string timeZoneId)
    {
        var tz = ResolveTimeZone(timeZoneId);

        if (_hasApiSnapshot && !_isUsingSystemTimeFallback)
        {
            var elapsed = DateTime.UtcNow - _apiSnapshotUtc;
            var projected = _apiLocalSnapshot.Add(elapsed);
            return DateTime.SpecifyKind(projected, DateTimeKind.Unspecified);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    public void UseSystemTime()
    {
        _isUsingSystemTimeFallback = true;
        TimeUpdated?.Invoke(this, GetCurrentTime(_settingsService.Load().TimeZoneId));
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Local; }
    }

    private static TimeSpan GetOffset(string id)
        => ResolveTimeZone(id).GetUtcOffset(DateTime.UtcNow);
}
