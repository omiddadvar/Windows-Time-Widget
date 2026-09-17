using System.Globalization;
using WindowsTimeWidget.Models;

namespace WindowsTimeWidget.Services;

public static class DateFormatter
{
    private static readonly string[] PersianMonths =
    {
        "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    private static readonly string[] PersianDays =
    {
        "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه",
        "پنج‌شنبه", "جمعه", "شنبه"
    };

    private static readonly PersianCalendar PersianCal = new();

    /// <summary>
    /// Returns the date line formatted for the requested language.
    /// </summary>
    public static string FormatDate(DateTime local, WidgetLanguage language) => language switch
    {
        WidgetLanguage.Persian => FormatPersianDate(local),
        _ => FormatEnglishDate(local)
    };

    public static string FormatEnglishDate(DateTime local)
        => local.ToString("dddd, MMMM dd, yyyy", CultureInfo.GetCultureInfo("en-US"));

    public static string FormatPersianDate(DateTime local, bool usePersianDigits = true)
    {
        var year = PersianCal.GetYear(local);
        var month = PersianCal.GetMonth(local);
        var day = PersianCal.GetDayOfMonth(local);
        var dayName = PersianDays[(int)local.DayOfWeek];

        string result = $"{dayName}، {day} {PersianMonths[month]}، {year}";
        return usePersianDigits ? ToPersianDigits(result) : result;
    }

    public static string ToPersianDigits(string input)
    {
        const string western = "0123456789";
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var c in input)
        {
            var i = western.IndexOf(c);
            sb.Append(i >= 0 ? persian[i] : c);
        }
        return sb.ToString();
    }
}