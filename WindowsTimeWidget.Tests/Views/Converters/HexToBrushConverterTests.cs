using FluentAssertions;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WindowsTimeWidget.Tests.ViewModels;
using WindowsTimeWidget.Views.Converters;

namespace WindowsTimeWidget.Tests.Views.Converters;

[Trait("Module", "Views.Converters.HexToBrushConverter")]
public class HexToBrushConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private SolidColorBrush Convert(string? input)
    {
        var sut = new HexToBrushConverter();
        var result = (SolidColorBrush)sut.Convert(input!, typeof(Brush), null!, Culture);
        return result;
    }


    [Fact]
    public void Convert_EightDigitHex_ReturnsBrushWithExactColor()
    {
        // Arrange
        const string input = "#FF112233";

        // Act
        var brush = Convert(input);

        // Assert
        brush.Color.Should().Be(Color.FromArgb(0xFF, 0x11, 0x22, 0x33));
    }

    [Fact]
    public void Convert_SixDigitHex_ReturnsBrush()
    {
        // Arrange
        const string input = "#112233";

        // Act
        var brush = Convert(input);

        // Assert
        brush.Should().NotBeNull();
        brush.Color.R.Should().Be(0x11);
        brush.Color.G.Should().Be(0x22);
        brush.Color.B.Should().Be(0x33);
    }

    [Fact]
    public void Convert_LowercaseHex_ParsesCorrectly()
    {
        // Arrange
        const string input = "#ffaabbcc";

        // Act
        var brush = Convert(input);

        // Assert
        brush.Color.Should().Be(Color.FromArgb(0xFF, 0xAA, 0xBB, 0xCC));
    }

    [Fact]
    public void Convert_NamedColor_ReturnsBrush()
    {
        // Arrange
        const string input = "Red";

        // Act
        var brush = Convert(input);

        // Assert
        brush.Color.Should().Be(Colors.Red);
    }

    [Fact]
    public void Convert_Null_ReturnsDefaultBrushColor()
    {
        // Arrange + Act
        var brush = Convert(null);

        // Assert
        brush.Color.Should().Be(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x2E));
    }

    [Fact]
    public void Convert_EmptyString_ReturnsDefaultBrushColor()
    {
        // Arrange + Act
        var brush = Convert(string.Empty);

        // Assert
        brush.Color.Should().Be(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x2E));
    }

    [Fact]
    public void Convert_UnparseableString_ReturnsTransparentBrush()
    {
        // Arrange
        const string input = "definitely-not-a-brush";

        // Act
        var brush = Convert(input);

        // Assert
        brush.Should().BeSameAs(Brushes.Transparent);
    }

    [Fact]
    public void Convert_NonStringValue_UsesToString()
    {
        // Arrange
        object input = 123;

        // Act
        var brush = StaRunner.Run(() =>
        {
            var sut = new HexToBrushConverter();
            return (SolidColorBrush)sut.Convert(input, typeof(Brush), null!, Culture);
        });

        // Assert
        brush.Should().BeSameAs(Brushes.Transparent);
    }

    [Fact]
    public void ConvertBack_AlwaysReturnsDoNothing()
    {
        // Arrange
        var brush = Brushes.Red;

        // Act
        var result = StaRunner.Run(() =>
        {
            var sut = new HexToBrushConverter();
            return sut.ConvertBack(brush, typeof(string), null!, Culture);
        });

        // Assert
        result.Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_NullInput_ReturnsDoNothing()
    {
        // Arrange + Act
        var result = StaRunner.Run(() =>
        {
            var sut = new HexToBrushConverter();
            return sut.ConvertBack(null!, typeof(string), null!, Culture);
        });

        // Assert
        result.Should().Be(Binding.DoNothing);
    }
}