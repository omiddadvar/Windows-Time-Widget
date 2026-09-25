using FluentAssertions;
using System.Globalization;
using System.Windows.Data;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.Views.Converters;

namespace WindowsTimeWidget.Tests.Views.Converters;

[Trait("Module", "Views.Converters.EnumToBoolConverter")]
public class EnumToBoolConverterTests
{
    private readonly EnumToBoolConverter _sut = new();
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        // Arrange + Act
        var result = _sut.Convert(null!, typeof(bool), "Persian", Culture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NullParameter_ReturnsFalse()
    {
        // Arrange
        var value = WidgetLanguage.English;

        // Act
        var result = _sut.Convert(value, typeof(bool), null!, Culture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_BothNull_ReturnsFalse()
    {
        // Arrange + Act
        var result = _sut.Convert(null!, typeof(bool), null!, Culture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_ValueEqualsParameter_ReturnsTrue()
    {
        // Arrange
        var value = WidgetLanguage.Persian;

        // Act
        var result = _sut.Convert(value, typeof(bool), nameof(WidgetLanguage.Persian), Culture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_ValueDiffersFromParameter_ReturnsFalse()
    {
        // Arrange
        var value = WidgetLanguage.English;

        // Act
        var result = _sut.Convert(value, typeof(bool), nameof(WidgetLanguage.Persian), Culture);

        // Assert
        result.Should().Be(false);
    }

    [Theory]
    [InlineData("Persian")]
    [InlineData("persian")]
    [InlineData("PERSIAN")]
    [InlineData("pErSiAn")]
    public void Convert_IsCaseInsensitive(string parameter)
    {
        // Arrange
        var value = WidgetLanguage.Persian;

        // Act
        var result = _sut.Convert(value, typeof(bool), parameter, Culture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_WorksForWidgetSize()
    {
        // Arrange
        var value = WidgetSize.Large;

        // Act
        var result = _sut.Convert(value, typeof(bool), "Large", Culture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_UnknownParameter_ReturnsFalse()
    {
        // Arrange
        var value = WidgetLanguage.English;

        // Act
        var result = _sut.Convert(value, typeof(bool), "Klingon", Culture);

        // Asserts
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_True_ParsesEnum()
    {
        // Arrange
        object value = true;

        // Act
        var result = _sut.ConvertBack(value, typeof(WidgetLanguage),
                                      nameof(WidgetLanguage.Persian), Culture);

        // Assert
        result.Should().Be(WidgetLanguage.Persian);
    }

    [Fact]
    public void ConvertBack_True_ParsesEnumForWidgetSize()
    {
        // Arrange
        object value = true;

        // Act
        var result = _sut.ConvertBack(value, typeof(WidgetSize), "Medium", Culture);

        // Assert
        result.Should().Be(WidgetSize.Medium);
    }

    [Fact]
    public void ConvertBack_False_ReturnsDoNothing()
    {
        // Arrange
        object value = false;

        // Act
        var result = _sut.ConvertBack(value, typeof(WidgetLanguage), "Persian", Culture);

        // Assert
        result.Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_NullParameter_ReturnsDoNothing()
    {
        // Arrange
        object value = true;

        // Act
        var result = _sut.ConvertBack(value, typeof(WidgetLanguage), null!, Culture);

        // Assert
        result.Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_NonBoolValue_ReturnsDoNothing()
    {
        // Arrange
        object value = "true";

        // Act
        var result = _sut.ConvertBack(value, typeof(WidgetLanguage), "Persian", Culture);

        // Assert
        result.Should().Be(Binding.DoNothing);
    }
}