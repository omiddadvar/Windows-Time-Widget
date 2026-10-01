using System.Net.Http;
using System.Net.Http.Json;
using TimeZoneConverter;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Models;

namespace WindowsTimeWidget.Services;

public class TimeService : ITimeService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private DateTimeOffset _apiLocalSnapshot;
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
    public async Task SyncFromApiAsync(string timeZoneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            timeZoneId = TimeZoneInfo.Local.Id;

        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var ianaId = TZConvert.TryWindowsToIana(timeZoneId, out var converted)
            ? converted
            : timeZoneId;

            var url = $"api/v1/time/current/zone?timeZone={Uri.EscapeDataString(ianaId)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<TimeApiResponse>(
                cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("API returned empty payload.");

            // Store snapshot
            _apiLocalSnapshot = payload.DateTimeOffset;
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
            var elapsed = DateTimeOffset.UtcNow - _apiLocalSnapshot.ToUniversalTime();
            var projected = _apiLocalSnapshot.ToUniversalTime().Add(elapsed);
            var inZone = TimeZoneInfo.ConvertTime(projected, tz);

            return DateTime.SpecifyKind(inZone.DateTime, DateTimeKind.Unspecified);
        }

        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).DateTime;
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
}
