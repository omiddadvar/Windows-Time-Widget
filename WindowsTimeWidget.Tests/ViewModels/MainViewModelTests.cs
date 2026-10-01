using FluentAssertions;
using Moq;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.Services;
using WindowsTimeWidget.Tests.TestData;
using WindowsTimeWidget.ViewModels;

namespace WindowsTimeWidget.Tests.ViewModels;

[Trait("Module", "ViewModels.MainViewModel")]
public class MainViewModelTests
{
    private static (MainViewModel sut, SettingsService settings, Mock<ITimeService> time)
        CreateSut(WidgetSettings? seed = null, DateTime? fixedTime = null)
    {
        var settings = new SettingsService();
        settings.Save(seed ?? TestHarness.ValidSettings());

        var time = new Mock<ITimeService>(MockBehavior.Loose);
        time.Setup(t => t.GetCurrentTime(It.IsAny<string>()))
            .Returns(fixedTime ?? new DateTime(2024, 3, 15, 10, 30, 0));
        time.SetupGet(t => t.IsUsingSystemTimeFallback).Returns(false);
        time.Setup(t => t.SyncFromApiAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new MainViewModel(time.Object, settings), settings, time);
    }

    [Fact]
    public void Ctor_LoadsSettingsFromService()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.WidgetColor = "#FF123456";

        // Act
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed));

        // Assert
        sut.Settings.WidgetColor.Should().Be("#FF123456");
    }

    [Fact]
    public void Start_KicksOffSyncFromApi()
    {
        // Arrange
        var (sut, _, time) = StaRunner.Run(() => CreateSut());

        // Act
        StaRunner.Run(() => sut.Start());

        // Assert
        time.Verify(t => t.SyncFromApiAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Start_UpdatesCurrentTime_FromService()
    {
        // Arrange
        var injected = new DateTime(2026, 9, 26, 14, 26, 0);
        var (sut, _, _) = StaRunner.Run(() => CreateSut(fixedTime: injected));

        // Act
        StaRunner.Run(() => sut.Start());

        // Assert
        sut.CurrentTime.Should().Be(injected);
    }

    [Fact]
    public void Stop_StopsUiTimer_WithoutThrowing()
    {
        // Arrange
        var (sut, _, _) = StaRunner.Run(() => CreateSut());
        StaRunner.Run(() => sut.Start());

        // Act
        Action act = () => StaRunner.Run(() => sut.Stop());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void FormattedTime_DerivesFromCurrentTime_NotWallClock()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.Use24HourFormat = true;
        seed.ShowSeconds = false;

        var injected = new DateTime(2026, 9, 26, 14, 26, 0);
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed, injected));

        // Act
        StaRunner.Run(() => sut.Start());

        // Assert
        sut.FormattedTime.Should().Be("14:26");
    }

    [Fact]
    public void FormattedTime_Uses24HourFormat_WhenConfigured()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.Use24HourFormat = false;
        seed.ShowSeconds = false;

        var injected = new DateTime(2026, 9, 26, 14, 26, 0);
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed, injected));

        // Act
        StaRunner.Run(() => sut.Start());

        // Assert
        sut.FormattedTime.Should().Be("02:26 PM");
    }

    [Fact]
    public void FormattedTime_IncludesSeconds_WhenConfigured()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.Use24HourFormat = true;
        seed.ShowSeconds = true;

        var injected = new DateTime(2026, 9, 26, 14, 26, 42);
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed, injected));

        // Act
        StaRunner.Run(() => sut.Start());

        // Assert
        sut.FormattedTime.Should().Be("14:26:42");
    }

    [Fact]
    public void FormattedDate_IsGregorian_WhenLanguageEnglish()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.Language = WidgetLanguage.English;

        var injected = new DateTime(2024, 3, 15, 10, 0, 0);
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed, injected));

        // Act
        StaRunner.Run(() => sut.Start());

        // Assert
        sut.FormattedDate.Should().Be("Friday, March 15, 2024");
    }

    [Fact]
    public void FormattedDate_IsPersian_WhenLanguagePersian()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.Language = WidgetLanguage.Persian;

        var injected = new DateTime(2024, 3, 15, 10, 0, 0);
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed, injected));

        // Act
        StaRunner.Run(() => sut.Start());

        // Assert
        sut.FormattedDate.Should().Contain("اسفند");
    }

    [Fact]
    public void ApplySettings_UpdatesDerivedProperties()
    {
        // Arrange
        var (sut, _, _) = StaRunner.Run(() => CreateSut());
        var updated = TestHarness.ValidSettings();
        updated.Size = WidgetSize.Large;
        updated.Language = WidgetLanguage.Persian;
        updated.WidgetColor = "#FF00FF00";

        // Act
        StaRunner.Run(() => sut.ApplySettings(updated));

        // Assert
        sut.IsPersianPrimary.Should().BeTrue();
        sut.WidgetWidth.Should().Be(updated.Size.GetDimensions().Width);
        sut.WidgetHeight.Should().Be(updated.Size.GetDimensions().Height);
    }

    [Fact]
    public void ApplySettings_RaisesPropertyChangedForDerivedProperties()
    {
        // Arrange
        var (sut, _, _) = StaRunner.Run(() => CreateSut());
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // Act
        StaRunner.Run(() => sut.ApplySettings(TestHarness.ValidSettings()));

        // Assert
        raised.Should().Contain(nameof(MainViewModel.WidgetWidth))
                      .And.Contain(nameof(MainViewModel.WidgetHeight))
                      .And.Contain(nameof(MainViewModel.TimeFontSize))
                      .And.Contain(nameof(MainViewModel.DateFontSize))
                      .And.Contain(nameof(MainViewModel.BackgroundBrush))
                      .And.Contain(nameof(MainViewModel.IsPersianPrimary))
                      .And.Contain(nameof(MainViewModel.TimeZoneLabel));
    }

    [Fact]
    public void BackgroundBrush_WhenColorValid_ReturnsBrushWithConfiguredOpacity()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.WidgetColor = "#CC1E1E2E";
        seed.Opacity = 0.6;

        // Act
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed));
        var brush = sut.BackgroundBrush;

        // Assert
        brush.Should().NotBeNull();
        brush.Opacity.Should().BeApproximately(0.6, 0.001);
    }

    [Fact]
    public void BackgroundBrush_WhenColorInvalid_FallsBackToDefault()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.WidgetColor = "not-a-color";

        // Act
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed));
        var brush = sut.BackgroundBrush;

        // Assert
        brush.Should().NotBeNull();
    }

    [Fact]
    public void TimeUpdated_EventFromService_UpdatesIsUsingSystemTime()
    {
        // Arrange
        var (sut, _, time) = StaRunner.Run(() => CreateSut());
        time.SetupGet(t => t.IsUsingSystemTimeFallback).Returns(true);

        // Act
        StaRunner.Run(() => time.Raise(
            t => t.TimeUpdated += null,
            [
                sut,
                new DateTime(2024, 3, 15, 11, 0, 0)
            ]));

        // Assert
        sut.IsUsingSystemTime.Should().BeTrue();
    }

    [Fact]
    public void TimeUpdated_EventFromService_UpdatesCurrentTime()
    {
        // Arrange
        var (sut, _, time) = StaRunner.Run(() => CreateSut());
        var raised = new DateTime(2026, 9, 26, 14, 26, 0);

        // Act
        StaRunner.Run(() => time.Raise(
            t => t.TimeUpdated += null,
            [sut, raised]
        ));

        // Assert
        sut.CurrentTime.Should().Be(raised);
    }

    [Fact]
    public void IsPersianPrimary_TrueWhenLanguagePersian()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.Language = WidgetLanguage.Persian;

        // Act
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed));

        // Assert
        sut.IsPersianPrimary.Should().BeTrue();
    }

    [Fact]
    public void ShowSecondaryDate_FollowsShowBothDatesSetting()
    {
        // Arrange
        var seed = TestHarness.ValidSettings();
        seed.ShowBothDates = true;

        // Act
        var (sut, _, _) = StaRunner.Run(() => CreateSut(seed));

        // Assert
        sut.ShowSecondaryDate.Should().BeTrue();
    }

    [Fact]
    public void SaveSettings_DelegatesToSettingsService()
    {
        // Arrange
        var (sut, settings, _) = StaRunner.Run(() => CreateSut());
        sut.Settings.WidgetColor = "#FFABCDEF";

        // Act
        StaRunner.Run(() => sut.SaveSettings());

        // Assert
        settings.Load().WidgetColor.Should().Be("#FFABCDEF");
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var (sut, _, _) = StaRunner.Run(() => CreateSut());

        // Act
        Action act = () => StaRunner.Run(() => sut.Dispose());

        // Assert
        act.Should().NotThrow();
    }
}