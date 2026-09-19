using FluentAssertions;
using WindowsTimeWidget.Services.BackgroundServices;

namespace WindowsTimeWidget.Tests.Services.BackgroundServices;

[Trait("Module", "Services.BackgroundServices.TimeSyncOptions")]
public class TimeSyncOptionsTests
{
    [Fact]
    public void SectionName_IsTimeSync()
    {
        // Act
        var sectionName = TimeSyncOptions.SectionName;

        // Assert
        sectionName.Should().Be("TimeSync");
    }

    [Fact]
    public void Defaults_AreFiveMinutesAndThreeSeconds()
    {
        // Arrange
        var opts = new TimeSyncOptions();

        // Act
        var syncInterval = opts.SyncInterval;
        var startupDelay = opts.StartupDelay;

        // Assert
        syncInterval.Should().Be(TimeSpan.FromMinutes(5));
        startupDelay.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Properties_AreSettable()
    {
        // Arrange
        var opts = new TimeSyncOptions
        {
            SyncInterval = TimeSpan.FromSeconds(30),
            StartupDelay = TimeSpan.FromSeconds(1)
        };

        // Act
        var syncInterval = opts.SyncInterval;
        var startupDelay = opts.StartupDelay;

        // Assert
        syncInterval.Should().Be(TimeSpan.FromSeconds(30));
        startupDelay.Should().Be(TimeSpan.FromSeconds(1));
    }
}