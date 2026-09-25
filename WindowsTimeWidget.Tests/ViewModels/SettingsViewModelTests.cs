using FluentAssertions;
using Moq;
using System.IO;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.Services;
using WindowsTimeWidget.Tests.TestData;
using WindowsTimeWidget.ViewModels;

namespace WindowsTimeWidget.Tests.ViewModels;

[Trait("Module", "ViewModels.SettingsViewModel")]
public class SettingsViewModelTests : IDisposable
{
    private readonly byte[]? _backup;

    public SettingsViewModelTests()
    {
        Directory.CreateDirectory(TestHarness.RealSettingsFolder);
        if (File.Exists(TestHarness.RealSettingsFile))
            _backup = File.ReadAllBytes(TestHarness.RealSettingsFile);
        File.Delete(TestHarness.RealSettingsFile);
    }

    public void Dispose()
    {
        try
        {
            if (_backup is not null)
                File.WriteAllBytes(TestHarness.RealSettingsFile, _backup);
            else
                File.Delete(TestHarness.RealSettingsFile);
        }
        catch { }
    }

    private static (SettingsViewModel sut, SettingsService settings, Mock<ITimeService> time)
        CreateSut(WidgetSettings? seed = null)
    {
        var settings = new SettingsService();
        if (seed is not null) settings.Save(seed);

        var time = new Mock<ITimeService>(MockBehavior.Loose);
        time.Setup(t => t.SyncFromApiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new SettingsViewModel(settings, time.Object), settings, time);
    }



    [Fact]
    public void Ctor_PopulatesTimeZones()
    {
        // Arrange + Act
        var (sut, _, _) = CreateSut();

        // Assert
        sut.TimeZones.Should().NotBeEmpty();
        sut.TimeZones.Should().BeInAscendingOrder(tz => tz.Offset);
    }

    [Fact]
    public void Ctor_SelectsCurrentLocalTimeZone_WhenSettingsMissing()
    {
        // Arrange + Act
        var (sut, _, _) = CreateSut();

        // Assert
        sut.SelectedTimeZone.Should().NotBeNull();
        sut.SelectedTimeZone!.Id.Should().Be(TimeZoneInfo.Local.Id);
    }

    [Fact]
    public void Ctor_LoadsValuesFromPersistedSettings()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.WidgetColor = "#AA112233";
        seed.Opacity = 0.55;
        seed.Use24HourFormat = false;
        seed.ShowSeconds = false;
        seed.ShowDate = false;
        seed.Language = WidgetLanguage.Persian;
        seed.ShowBothDates = true;

        // Act
        var (sut, _, _) = CreateSut(seed);

        // Assert
        sut.ColorHex.Should().Be("#AA112233");
        sut.Opacity.Should().Be(0.55);
        sut.Use24Hour.Should().BeFalse();
        sut.ShowSeconds.Should().BeFalse();
        sut.ShowDate.Should().BeFalse();
        sut.SelectedLanguage.Should().Be(WidgetLanguage.Persian);
        sut.ShowBothDates.Should().BeTrue();
    }

    [Fact]
    public void SaveCommand_PersistsWorkingCopy()
    {
        // Arrange
        var (sut, settings, _) = CreateSut();
        sut.ColorHex = "#FF00AA55";
        sut.Opacity = 0.8;
        sut.Use24Hour = false;

        // Act
        sut.SaveCommand.Execute(null);

        // Assert
        var saved = settings.Load();
        saved.WidgetColor.Should().Be("#FF00AA55");
        saved.Opacity.Should().Be(0.8);
        saved.Use24HourFormat.Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_TrimsAndNormalizesColorHex()
    {
        // Arrange
        var (sut, settings, _) = CreateSut();
        sut.ColorHex = "  00AAFF  ";

        // Act
        sut.SaveCommand.Execute(null);

        // Assert
        settings.Load().WidgetColor.Should().Be("#CC00AAFF");
    }

    [Fact]
    public void SaveCommand_FallsBackToDefault_WhenColorBlank()
    {
        // Arrange
        var (sut, settings, _) = CreateSut();
        sut.ColorHex = "   ";

        // Act
        sut.SaveCommand.Execute(null);

        // Assert
        settings.Load().WidgetColor.Should().Be("#CC1E1E2E");
    }

    [Fact]
    public void SaveCommand_UsesSelectedTimeZoneId()
    {
        // Arrange
        var (sut, settings, _) = CreateSut();
        var target = sut.TimeZones.First(tz => tz.Id != sut.SelectedTimeZone!.Id);
        sut.SelectedTimeZone = target;

        // Act
        sut.SaveCommand.Execute(null);

        // Assert
        settings.Load().TimeZoneId.Should().Be(target.Id);
    }

    [Fact]
    public void SaveCommand_TriggersImmediateSyncFromApi()
    {
        // Arrange
        var (sut, _, time) = CreateSut();
        var target = sut.TimeZones.First();
        sut.SelectedTimeZone = target;

        // Act
        sut.SaveCommand.Execute(null);

        // Assert
        time.Verify(t => t.SyncFromApiAsync(target.Id, It.IsAny<CancellationToken>()),
                    Times.Once);
    }

    [Fact]
    public void SaveCommand_RaisesSaveRequested_WithWorkingCopy()
    {
        // Arrange
        var (sut, _, _) = CreateSut();
        WidgetSettings? received = null;
        sut.SaveRequested += s => received = s;

        // Act
        sut.SaveCommand.Execute(null);

        // Assert
        received.Should().NotBeNull();
        received!.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SaveCommand_RaisesCloseRequested()
    {
        // Arrange
        var (sut, _, _) = CreateSut();
        var closed = false;
        sut.CloseRequested += () => closed = true;

        // Act
        sut.SaveCommand.Execute(null);

        // Assert
        closed.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequested_ButDoesNotSave()
    {
        // Arrange
        var (sut, settings, _) = CreateSut();
        var before = settings.Load();
        sut.ColorHex = "#FF000000";
        var closed = false;
        sut.CloseRequested += () => closed = true;

        // Act
        sut.CancelCommand.Execute(null);

        // Assert
        closed.Should().BeTrue();
        settings.Load().WidgetColor.Should().Be(before.WidgetColor);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested_Only()
    {
        // Arrange
        var (sut, _, _) = CreateSut();
        var closed = false;
        sut.CloseRequested += () => closed = true;

        // Act
        sut.CloseCommand.Execute(null);

        // Assert
        closed.Should().BeTrue();
    }


    [Fact]
    public void ResetCommand_ReloadsDefaults_WithoutSaving()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.WidgetColor = "#FFAA0000";
        var (sut, settings, _) = CreateSut(seed);

        // Act
        sut.ResetCommand.Execute(null);

        // Assert
        sut.ColorHex.Should().Be("#CC1E1E2E");
        settings.Load().WidgetColor.Should().Be("#FFAA0000");
    }

    [Fact]
    public void SettingColorHex_RaisesPropertyChanged()
    {
        // Arrange
        var (sut, _, _) = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // Act
        sut.ColorHex = "#12345678";

        // Assert
        raised.Should().Contain(nameof(SettingsViewModel.ColorHex));
    }

    [Fact]
    public void SettingSelectedTimeZone_RaisesPropertyChanged()
    {
        // Arrange
        var (sut, _, _) = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // Act
        sut.SelectedTimeZone = sut.TimeZones.Last();

        // Assert
        raised.Should().Contain(nameof(SettingsViewModel.SelectedTimeZone));
    }
}