using FluentAssertions;
using System.IO;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.Services;
using WindowsTimeWidget.Tests.TestData;

namespace WindowsTimeWidget.Tests.Services;

[Trait("Module", "Services.SettingsService")]
public class SettingsServiceTests : IDisposable
{
    private readonly byte[]? _backupBytes;

    public SettingsServiceTests()
    {
        Directory.CreateDirectory(TestHarness.RealSettingsFolder);
        if (File.Exists(TestHarness.RealSettingsFile))
            _backupBytes = File.ReadAllBytes(TestHarness.RealSettingsFile);

        File.Delete(TestHarness.RealSettingsFile);
    }

    public void Dispose()
    {
        try
        {
            if (_backupBytes is not null)
                File.WriteAllBytes(TestHarness.RealSettingsFile, _backupBytes);
            else
                File.Delete(TestHarness.RealSettingsFile);
        }
        catch { }
    }

    private static SettingsService NewService() => new();

    [Fact]
    public void Load_WhenNoFile_ReturnsDefaults()
    {
        // Arrange
        var sut = NewService();

        // Act
        var settings = sut.Load();

        // Assert
        settings.Should().NotBeNull();
        settings.WidgetColor.Should().Be("#CC1E1E2E");
    }

    [Fact]
    public void Save_ThenLoad_ReturnsSameValues()
    {
        // Arrange
        var sut = NewService();
        var original = TestHarness.ValidSettings();
        original.WidgetColor = "#AA112233";
        original.Opacity = 0.75;
        original.Use24HourFormat = false;
        original.Language = WidgetLanguage.Persian;

        // Act
        sut.Save(original);
        var loaded = sut.Load();

        // Assert
        loaded.WidgetColor.Should().Be("#AA112233");
        loaded.Opacity.Should().Be(0.75);
        loaded.Use24HourFormat.Should().BeFalse();
        loaded.Language.Should().Be(WidgetLanguage.Persian);
    }

    [Fact]
    public void Save_WritesJsonFileToExpectedPath()
    {
        // Arrange
        var sut = NewService();
        var settings = TestHarness.ValidSettings();

        // Act
        sut.Save(settings);

        // Assert
        File.Exists(TestHarness.RealSettingsFile).Should().BeTrue();
        File.ReadAllText(TestHarness.RealSettingsFile).Should().Contain("widgetColor");
    }

    [Fact]
    public void Load_ReturnsClone_NotSameReference()
    {
        // Arrange
        var sut = NewService();
        sut.Save(TestHarness.ValidSettings());

        // Act
        var first = sut.Load();
        var second = sut.Load();

        // Assert
        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void Save_Null_DoesNothing()
    {
        // Arrange
        var sut = NewService();
        var before = sut.Load();

        // Act
        sut.Save(null!);
        var after = sut.Load();

        // Assert
        after.Should().BeEquivalentTo(before);
    }

    [Fact]
    public void Save_InvalidSettings_DoesNotOverwriteExisting()
    {
        // Arrange
        var sut = NewService();
        sut.Save(TestHarness.ValidSettings());
        var before = sut.Load();

        var invalid = new WidgetSettings { WidgetColor = "not-a-color", Opacity = 5.0 };
        invalid.IsValid.Should().BeFalse();

        // Act
        sut.Save(invalid);

        // Assert
        sut.Load().WidgetColor.Should().Be(before.WidgetColor);
    }

    [Fact]
    public void Save_RaisesSettingsChanged_WithClone()
    {
        // Arrange
        var sut = NewService();
        WidgetSettings? raised = null;
        sut.SettingsChanged += (_, s) => raised = s;

        var toSave = TestHarness.ValidSettings();

        // Act
        sut.Save(toSave);

        // Assert
        raised.Should().NotBeNull();
        raised.Should().NotBeSameAs(toSave);
        raised!.WidgetColor.Should().Be(toSave.WidgetColor);
    }

    [Fact]
    public void Save_Invalid_DoesNotRaiseEvent()
    {
        // Arrange
        var sut = NewService();
        var raised = false;
        sut.SettingsChanged += (_, _) => raised = true;

        var invalid = new WidgetSettings { WidgetColor = "bad", Opacity = 99 };

        // Act
        sut.Save(invalid);

        // Assert
        raised.Should().BeFalse();
    }

    [Fact]
    public void Load_WhenFileCorrupt_ReturnsDefaults()
    {
        // Arrange
        File.WriteAllText(TestHarness.RealSettingsFile, "{ not valid json ");
        var sut = NewService();

        // Act
        var settings = sut.Load();

        // Assert
        settings.Should().NotBeNull();
        settings.WidgetColor.Should().Be("#CC1E1E2E");
    }
}