using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WindowsTimeWidget.Abstractions;

namespace WindowsTimeWidget.Services.BackgroundServices;

public class TimeSyncService : BackgroundService, ITimeSyncService
{
    private readonly ITimeService _timeService;
    private readonly ISettingsService _settingsService;
    private readonly TimeSyncOptions _options;

    private readonly SemaphoreSlim _triggerSignal = new(0, 1);
    private DateTime? _nextSyncUtc;

    public TimeSyncService(
        ITimeService timeService,
        ISettingsService settingsService,
        IOptions<TimeSyncOptions> options)
    {
        _timeService = timeService;
        _settingsService = settingsService;
        _options = options.Value;
    }

    public DateTime? NextSyncUtc => _nextSyncUtc;

    public Task TriggerSyncAsync(CancellationToken cancellationToken = default)
    {
        if (_triggerSignal.CurrentCount == 0)
            _triggerSignal.Release();
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(_options.StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            _nextSyncUtc = DateTime.UtcNow + _options.SyncInterval;

            try
            {
                var settings = _settingsService.Load();
                var tz = string.IsNullOrWhiteSpace(settings.TimeZoneId)
                    ? TimeZoneInfo.Local.Id
                    : settings.TimeZoneId;

                await _timeService.SyncFromApiAsync(tz, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _timeService.UseSystemTime();
            }

            var delay = Task.Delay(_options.SyncInterval, stoppingToken);
            var signal = _triggerSignal.WaitAsync(stoppingToken);
            await Task.WhenAny(delay, signal);

            while (_triggerSignal.CurrentCount > 0)
                await _triggerSignal.WaitAsync(stoppingToken);
        }

        _nextSyncUtc = null;
    }
}