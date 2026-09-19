using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Services.BackgroundServices;

namespace WindowsTimeWidget.Tests.Services.BackgroundServices;

[Trait("Module", "Services.BackgroundServices.ServiceCollectionExtensions")]
public class ServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
       => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceCollection BuildServices(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<ITimeService>());
        services.AddSingleton(Mock.Of<ISettingsService>());
        services.AddLogging();
        services.AddTimeSyncBackgroundService(config);
        return services;
    }

    [Fact]
    public void BindsOptionsFromConfiguration()
    {
        // Arrange
        var config = BuildConfig(new()
        {
            ["TimeSync:SyncInterval"] = "00:00:45",
            ["TimeSync:StartupDelay"] = "00:00:05"
        });
        var sp = BuildServices(config).BuildServiceProvider();

        // Act
        var opts = sp.GetRequiredService<IOptions<TimeSyncOptions>>().Value;

        // Assert
        opts.SyncInterval.Should().Be(TimeSpan.FromSeconds(45));
        opts.StartupDelay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void WhenNoConfig_UsesDefaults()
    {
        // Arrange
        var sp = BuildServices(BuildConfig(new())).BuildServiceProvider();

        // Act
        var opts = sp.GetRequiredService<IOptions<TimeSyncOptions>>().Value;

        // Assert
        opts.SyncInterval.Should().Be(TimeSpan.FromMinutes(5));
        opts.StartupDelay.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void RegistersSameInstanceForConcreteAndInterface()
    {
        // Arrange
        var sp = BuildServices(BuildConfig(new())).BuildServiceProvider();

        // Act
        var concrete = sp.GetRequiredService<TimeSyncService>();
        var iface = sp.GetRequiredService<ITimeSyncService>();

        // Assert
        iface.Should().BeSameAs(concrete);
    }

    [Fact]
    public void RegistersHostedService()
    {
        // Arrange
        var sp = BuildServices(BuildConfig(new())).BuildServiceProvider();

        // Act
        var hosted = sp.GetServices<IHostedService>();

        // Assert
        hosted.Should().ContainSingle(h => h is TimeSyncService);
    }
}