using FluentAssertions;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WindowsTimeWidget.Views.Converters;

namespace WindowsTimeWidget.Tests.Views.Converters;

[Trait("Module", "Views.Converters.BoolToFlowDirectionConverter")]
public class BoolToFlowDirectionConverterTests
{
    private readonly BoolToFlowDirectionConverter _sut = new();
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private static readonly object[] NoParams = { null! };
    private static readonly Type TargetType = typeof(FlowDirection);


    [Fact]
    public void Convert_True_ReturnsRightToLeft()
    {
        // Arrange
        const bool input = true;

        // Act
        var result = _sut.Convert(input, TargetType, NoParams[0], Culture);

        // Assert
        result.Should().Be(FlowDirection.RightToLeft);
    }

    [Fact]
    public void Convert_False_ReturnsLeftToRight()
    {
        // Arrange
        const bool input = false;

        // Act
        var result = _sut.Convert(input, TargetType, NoParams[0], Culture);

        // Assert
        result.Should().Be(FlowDirection.LeftToRight);
    }

    [Fact]
    public void Convert_Null_ReturnsLeftToRight()
    {
        // Arrange
        object? input = null;

        // Act
        var result = _sut.Convert(input!, TargetType, NoParams[0], Culture);

        // Assert
        result.Should().Be(FlowDirection.LeftToRight);
    }

    [Theory]
    [InlineData("true")]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData("True")]
    public void Convert_NonBoolValues_ReturnLeftToRight(object input)
    {
        // Arrange + Act
        var result = _sut.Convert(input, TargetType, NoParams[0], Culture);

        // Assert
        result.Should().Be(FlowDirection.LeftToRight);
    }

    [Fact]
    public void Convert_BoxedTrue_ReturnsRightToLeft()
    {
        // Arrange
        object input = true;

        // Act
        var result = _sut.Convert(input, TargetType, NoParams[0], Culture);

        // Assert
        result.Should().Be(FlowDirection.RightToLeft);
    }


    [Fact]
    public void ConvertBack_AlwaysReturnsDoNothing()
    {
        // Arrange
        var value = FlowDirection.RightToLeft;

        // Act
        var result = _sut.ConvertBack(value, typeof(bool), NoParams[0], Culture);

        // Assert
        result.Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_DoesNotThrow_ForNullInput()
    {
        // Arrange + Act
        Action act = () => _sut.ConvertBack(null!, typeof(bool), NoParams[0], Culture);

        // Assert
        act.Should().NotThrow();
    }
}