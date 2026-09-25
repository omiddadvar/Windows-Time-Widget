using FluentAssertions;
using System.Globalization;
using System.Windows.Media;
using WindowsTimeWidget.Views.Converters;

namespace WindowsTimeWidget.Tests.Views.Converters;

[Trait("Module", "Views.Converters.ColorToHexConverter")]
public class ColorToHexConverterTests
{
    private readonly ColorToHexConverter _sut = new();
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private static readonly Color FallbackColor =
        Color.FromArgb(0xCC, 0x1E, 0x1E, 0x2E);


    [Fact]
    public void Convert_EightDigitHex_ReturnsExactColor()
    {
        // Arrange
        const string input = "#FF112233";

        // Act
        var result = _sut.Convert(input, typeof(Color), null!, Culture);

        // Assert
        result.Should().Be(Color.FromArgb(0xFF, 0x11, 0x22, 0x33));
    }

    [Fact]
    public void Convert_SixDigitHex_AddsOpaqueAlphaPrefix()
    {
        // Arrange
        const string input = "#112233";

        // Act
        var result = _sut.Convert(input, typeof(Color), null!, Culture);

        // Assert
        result.Should().Be(Color.FromArgb(0xCC, 0x11, 0x22, 0x33));
    }

    [Fact]
    public void Convert_HexWithoutHash_PrependsHash()
    {
        // Arrange
        const string input = "112233";

        // Act
        var result = _sut.Convert(input, typeof(Color), null!, Culture);

        // Assert
        result.Should().Be(Color.FromArgb(0xCC, 0x11, 0x22, 0x33));
    }

    [Fact]
    public void Convert_WithSurroundingWhitespace_TrimsAndParses()
    {
        // Arrange
        const string input = "   #AABBCC   ";

        // Act
        var result = _sut.Convert(input, typeof(Color), null!, Culture);

        // Assert
        result.Should().Be(Color.FromArgb(0xCC, 0xAA, 0xBB, 0xCC));
    }

    [Fact]
    public void Convert_LowercaseHex_ParsesCorrectly()
    {
        // Arrange
        const string input = "#ffaabbcc";

        // Act
        var result = _sut.Convert(input, typeof(Color), null!, Culture);

        // Assert
        result.Should().Be(Color.FromArgb(0xFF, 0xAA, 0xBB, 0xCC));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("not-a-color")]
    [InlineData("#ZZZZZZ")]
    public void Convert_InvalidString_ReturnsFallbackColor(string? input)
    {
        // Arrange + Act
        var result = _sut.Convert(input!, typeof(Color), null!, Culture);

        // Assert
        result.Should().Be(FallbackColor);
    }

    [Fact]
    public void Convert_NonStringValue_ReturnsFallbackColor()
    {
        // Arrange
        object input = 42;

        // Act
        var result = _sut.Convert(input, typeof(Color), null!, Culture);

        // Assert
        result.Should().Be(FallbackColor);
    }


    [Fact]
    public void ConvertBack_FullyOpaqueColor_ReturnsEightDigitHex()
    {
        // Arrange
        var input = Color.FromArgb(0xFF, 0x11, 0x22, 0x33);

        // Act
        var result = _sut.ConvertBack(input, typeof(string), null!, Culture);

        // Assert
        result.Should().Be("#FF112233");
    }

    [Fact]
    public void ConvertBack_TransparentColor_ReturnsEightDigitHex()
    {
        // Arrange
        var input = Color.FromArgb(0x00, 0x00, 0x00, 0x00);

        // Act
        var result = _sut.ConvertBack(input, typeof(string), null!, Culture);

        // Assert
        result.Should().Be("#00000000");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-color")]
    [InlineData(42)]
    public void ConvertBack_NonColor_ReturnsFallbackHexString(object? input)
    {
        // Arrange + Act
        var result = _sut.ConvertBack(input!, typeof(string), null!, Culture);

        // Assert
        result.Should().Be("#CC1E1E2E");
    }

    [Theory]
    [InlineData("#FF112233")]
    [InlineData("#CC1E1E2E")]
    [InlineData("#00AABBCC")]
    public void Convert_Then_ConvertBack_RoundTripsEightDigitHex(string original)
    {
        // Arrange
        var color = (Color)_sut.Convert(original, typeof(Color), null!, Culture);

        // Act
        var back = (string)_sut.ConvertBack(color, typeof(string), null!, Culture);

        // Assert
        back.Should().Be(original);
    }
}