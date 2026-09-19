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
        var local = new DateTime(2024, 3, 15, 10, 30, 0);

        DateFormatter.FormatEnglishDate(local).Should().Be("Friday, March 15, 2024");
    }

    [Theory]
    [InlineData(2024, 1, 1)]
    [InlineData(2024, 6, 30)]
    [InlineData(2024, 12, 31)]
    public void FormatEnglishDate_IsCultureInvariant(int y, int m, int d)
    {
        var local = new DateTime(y, m, d);
        var expected = local.ToString("dddd, MMMM dd, yyyy", EnUs);

        DateFormatter.FormatEnglishDate(local).Should().Be(expected);
    }

    [Fact]
    public void FormatPersianDate_UsesPersianDigitsByDefault()
    {
        var result = DateFormatter.FormatPersianDate(new DateTime(2024, 3, 15));

        result.Should().NotMatchRegex("[0-9]");
        result.Should().Contain("،");
    }

    [Fact]
    public void FormatPersianDate_WithWesternDigits_ContainsWesternDigits()
    {
        var result = DateFormatter.FormatPersianDate(
            new DateTime(2024, 3, 15), usePersianDigits: false);

        result.Should().MatchRegex("[0-9]");
        result.Should().Contain("،");
    }

    [Fact]
    public void FormatPersianDate_ContainsKnownMonthAndDayNames()
    {
        // Gregorian 2024-03-15 (Friday) → Persian 25 Esfand 1402
        var result = DateFormatter.FormatPersianDate(new DateTime(2024, 3, 15));

        result.Should().Contain("اسفند");
        result.Should().Contain("جمعه");
    }

    [Fact]
    public void ToPersianDigits_MapsAllWesternDigits()
    {
        DateFormatter.ToPersianDigits("0123456789").Should().Be("۰۱۲۳۴۵۶۷۸۹");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("تست", "تست")]
    [InlineData("a1b2", "a۱b۲")]
    [InlineData(" 1 2 ", " ۱ ۲ ")]
    public void ToPersianDigits_LeavesNonDigitsUntouched(string input, string expected)
    {
        DateFormatter.ToPersianDigits(input).Should().Be(expected);
    }

    [Fact]
    public void FormatDate_Persian_DispatchesToPersianFormatter()
    {
        var local = new DateTime(2024, 3, 15);

        DateFormatter.FormatDate(local, WidgetLanguage.Persian)
            .Should().Be(DateFormatter.FormatPersianDate(local));
    }

    [Fact]
    public void FormatDate_English_DispatchesToEnglishFormatter()
    {
        var local = new DateTime(2024, 3, 15);

        DateFormatter.FormatDate(local, WidgetLanguage.English)
            .Should().Be(DateFormatter.FormatEnglishDate(local));
    }
}
