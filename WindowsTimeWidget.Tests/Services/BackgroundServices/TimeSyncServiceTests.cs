using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Services.BackgroundServices;
using WindowsTimeWidget.Tests.TestData;

namespace WindowsTimeWidget.Tests.Services.BackgroundServices;

[Trait("Module", "Services.BackgroundServices.TimeSyncService")]
public class TimeSyncServiceTests
{
    private static TimeSyncService CreateSut(
        Mock<ITimeService>? time = null,
        Mock<ISettingsService>? settings = null,
        TimeSyncOptions? options = null)
    {
        time ??= new Mock<ITimeService>(MockBehavior.Loose);
        settings ??= new Mock<ISettingsService>(MockBehavior.Loose);
        settings.Setup(s => s.Load()).Returns(TestHarness.ValidSettings());

        options ??= new TimeSyncOptions
        {
            StartupDelay = TimeSpan.FromMilliseconds(10),
            SyncInterval = TimeSpan.FromMilliseconds(100)
        };

        return new TimeSyncService(time.Object, settings.Object, Options.Create(options));
    }

    [Fact]
    public async Task ExecuteAsync_CallsSyncFromApi_AfterStartupDelay()
    {
        // Arrange
        var time = new Mock<ITimeService>(MockBehavior.Loose);
        var sut = CreateSut(time: time);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(200, cts.Token);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        time.Verify(t => t.SyncFromApiAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_OnSyncException_UsesSystemTime()
    {
        // Arrange
        var time = new Mock<ITimeService>(MockBehavior.Loose);
        time.Setup(t => t.SyncFromApiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));

        var sut = CreateSut(time: time);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(200, cts.Token);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        time.Verify(t => t.UseSystemTime(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task NextSyncUtc_IsSetWhileRunning()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(200, cts.Token);

        // Assert
        sut.NextSyncUtc.Should().NotBeNull();

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NextSyncUtc_IsClearedAfterShutdown()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(150, cts.Token);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        sut.NextSyncUtc.Should().BeNull();
    }

    [Fact]
    public async Task TriggerSyncAsync_InterruptsIntervalDelay()
    {
        // Arrange
        var syncCount = 0;
        var time = new Mock<ITimeService>(MockBehavior.Loose);
        time.Setup(t => t.SyncFromApiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref syncCount))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(time: time, options: new TimeSyncOptions
        {
            StartupDelay = TimeSpan.FromMilliseconds(10),
            SyncInterval = TimeSpan.FromSeconds(30)
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(200, cts.Token);
        var afterStartup = syncCount;

        await sut.TriggerSyncAsync(cts.Token);
        await Task.Delay(300, cts.Token);
        var afterTrigger = syncCount;

        await sut.StopAsync(CancellationToken.None);

        // Assert
        afterStartup.Should().Be(1);
        afterTrigger.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task TriggerSyncAsync_WhenSignalledTwice_DoesNotOverflow()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(150, cts.Token);

        await sut.TriggerSyncAsync(cts.Token);
        await sut.TriggerSyncAsync(cts.Token);

        await sut.StopAsync(CancellationToken.None);

        // Assert
        // Reaching here without throwing is the assertion.
        // (SemaphoreSlim full-count overflow would throw SemaphoreFullException.)
        true.Should().BeTrue();
    }
}