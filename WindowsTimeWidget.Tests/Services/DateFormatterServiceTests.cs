using FluentAssertions;
using System.Globalization;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.Services;

namespace WindowsTimeWidget.Tests.Services;

[Trait("Module", "Services.DateFormatterService")]
public class DateFormatterServiceTests
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");


    [Fact]
    public void FormatEnglishDate_MatchesExpectedEnUsFormat()
    {
        // Arrange
        var local = new DateTime(2024, 3, 15, 10, 30, 0);

        // Act
        var result = DateFormatter.FormatEnglishDate(local);

        // Assert
        result.Should().Be("Friday, March 15, 2024");
    }

    [Theory]
    [InlineData(2024, 1, 1)]
    [InlineData(2024, 6, 30)]
    [InlineData(2024, 12, 31)]
    public void FormatEnglishDate_IsCultureInvariant(int y, int m, int d)
    {
        // Arrange
        var local = new DateTime(y, m, d);
        var expected = local.ToString("dddd, MMMM dd, yyyy", EnUs);

        // Act
        var result = DateFormatter.FormatEnglishDate(local);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatPersianDate_UsesPersianDigitsByDefault()
    {
        // Arrange
        var local = new DateTime(2024, 3, 15);

        // Act
        var result = DateFormatter.FormatPersianDate(local);

        // Assert
        result.Should().NotMatchRegex("[0-9]");
        result.Should().Contain("،");
    }

    [Fact]
    public void FormatPersianDate_WithWesternDigits_ContainsWesternDigits()
    {
        // Arrange
        var local = new DateTime(2024, 3, 15);

        // Act
        var result = DateFormatter.FormatPersianDate(local, usePersianDigits: false);

        // Assert
        result.Should().MatchRegex("[0-9]");
        result.Should().Contain("،");
    }

    [Fact]
    public void FormatPersianDate_ContainsKnownMonthAndDayNames()
    {
        // Arrange
        // Gregorian 2024-03-15 (Friday) → Persian 25 Esfand 1402
        var local = new DateTime(2024, 3, 15);

        // Act
        var result = DateFormatter.FormatPersianDate(local);

        // Assert
        result.Should().Contain("اسفند");
        result.Should().Contain("جمعه");
    }

    [Fact]
    public void ToPersianDigits_MapsAllWesternDigits()
    {
        // Arrange
        const string input = "0123456789";

        // Act
        var result = DateFormatter.ToPersianDigits(input);

        // Assert
        result.Should().Be("۰۱۲۳۴۵۶۷۸۹");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("تست", "تست")]
    [InlineData("a1b2", "a۱b۲")]
    [InlineData(" 1 2 ", " ۱ ۲ ")]
    public void ToPersianDigits_LeavesNonDigitsUntouched(string input, string expected)
    {
        // Act
        var result = DateFormatter.ToPersianDigits(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatDate_Persian_DispatchesToPersianFormatter()
    {
        // Arrange
        var local = new DateTime(2024, 3, 15);
        var expected = DateFormatter.FormatPersianDate(local);

        // Act
        var result = DateFormatter.FormatDate(local, WidgetLanguage.Persian);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatDate_English_DispatchesToEnglishFormatter()
    {
        // Arrange
        var local = new DateTime(2024, 3, 15);
        var expected = DateFormatter.FormatEnglishDate(local);

        // Act
        var result = DateFormatter.FormatDate(local, WidgetLanguage.English);

        // Assert
        result.Should().Be(expected);
    }
}