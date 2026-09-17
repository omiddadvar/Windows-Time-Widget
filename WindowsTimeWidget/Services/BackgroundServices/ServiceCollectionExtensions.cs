using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WindowsTimeWidget.Abstractions;

namespace WindowsTimeWidget.Services.BackgroundServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTimeSyncBackgroundService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TimeSyncOptions>(
            configuration.GetSection(TimeSyncOptions.SectionName));

        services.AddSingleton<TimeSyncService>();
        services.AddSingleton<ITimeSyncService>(sp => sp.GetRequiredService<TimeSyncService>());
        services.AddHostedService(sp => sp.GetRequiredService<TimeSyncService>());

        return services;
    }
}