using FluentAssertions;
using System.IO;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.Services;
using WindowsTimeWidget.Tests.TestData;

namespace WindowsTimeWidget.Tests.Services;

[Trait("Module", "Services.SettingsService")]
public class SettingsServiceTests
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
        var settings = NewService().Load();

        settings.Should().NotBeNull();
        settings.WidgetColor.Should().Be("#CC1E1E2E");
    }

    [Fact]
    public void Save_ThenLoad_ReturnsSameValues()
    {
        var sut = NewService();
        var original = TestHarness.ValidSettings();
        original.WidgetColor = "#AA112233";
        original.Opacity = 0.75;
        original.Use24HourFormat = false;
        original.Language = WidgetLanguage.Persian;

        sut.Save(original);
        var loaded = sut.Load();

        loaded.WidgetColor.Should().Be("#AA112233");
        loaded.Opacity.Should().Be(0.75);
        loaded.Use24HourFormat.Should().BeFalse();
        loaded.Language.Should().Be(WidgetLanguage.Persian);
    }

    [Fact]
    public void Save_WritesJsonFileToExpectedPath()
    {
        NewService().Save(TestHarness.ValidSettings());

        File.Exists(TestHarness.RealSettingsFile).Should().BeTrue();
        File.ReadAllText(TestHarness.RealSettingsFile).Should().Contain("widgetColor");
    }

    [Fact]
    public void Load_ReturnsClone_NotSameReference()
    {
        var sut = NewService();
        sut.Save(TestHarness.ValidSettings());

        var first = sut.Load();
        var second = sut.Load();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void Save_Null_DoesNothing()
    {
        var sut = NewService();
        var before = sut.Load();

        sut.Save(null!);
        var after = sut.Load();

        after.Should().BeEquivalentTo(before);
    }

    [Fact]
    public void Save_InvalidSettings_DoesNotOverwriteExisting()
    {
        var sut = NewService();
        sut.Save(TestHarness.ValidSettings());
        var before = sut.Load();

        var invalid = new WidgetSettings { WidgetColor = "not-a-color", Opacity = 5.0 };
        invalid.IsValid.Should().BeFalse();
        sut.Save(invalid);

        sut.Load().WidgetColor.Should().Be(before.WidgetColor);
    }

    [Fact]
    public void Save_RaisesSettingsChanged_WithClone()
    {
        var sut = NewService();
        WidgetSettings? raised = null;
        sut.SettingsChanged += (_, s) => raised = s;

        var toSave = TestHarness.ValidSettings();
        sut.Save(toSave);

        raised.Should().NotBeNull();
        raised.Should().NotBeSameAs(toSave);
        raised!.WidgetColor.Should().Be(toSave.WidgetColor);
    }

    [Fact]
    public void Save_Invalid_DoesNotRaiseEvent()
    {
        var sut = NewService();
        var raised = false;
        sut.SettingsChanged += (_, _) => raised = true;

        sut.Save(new WidgetSettings { WidgetColor = "bad", Opacity = 99 });

        raised.Should().BeFalse();
    }

    [Fact]
    public void Load_WhenFileCorrupt_ReturnsDefaults()
    {
        File.WriteAllText(TestHarness.RealSettingsFile, "{ not valid json ");

        var settings = NewService().Load();

        settings.Should().NotBeNull();
        settings.WidgetColor.Should().Be("#CC1E1E2E");
    }
}
