using System.Windows.Media;
using System.Windows.Threading;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.Services;

namespace WindowsTimeWidget.ViewModels;


public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ITimeService _timeService;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer _uiTimer;

    private DateTime _currentTime;
    private string _formattedTime = string.Empty;
    private string _formattedDate = string.Empty;
    private string _formattedSecondaryDate = string.Empty;
    private bool _showSecondaryDate;
    private string _timeZoneLabel = string.Empty;
    private string _dayOfWeek = string.Empty;
    private bool _isUsingSystemTime;


    public MainViewModel(ITimeService timeService, ISettingsService settingsService)
    {
        _timeService = timeService;
        _settingsService = settingsService;

        Settings = _settingsService.Load();

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _uiTimer.Tick += (_, _) => Tick();

        _timeService.TimeUpdated += OnTimeUpdated;
        ApplySettings(Settings);
    }

    public WidgetSettings Settings { get; private set; }

    public DateTime CurrentTime
    {
        get => _currentTime;
        private set
        {
            if (SetProperty(ref _currentTime, value))
                RefreshFormatted();
        }
    }

    public string FormattedTime
    {
        get => _formattedTime;
        private set => SetProperty(ref _formattedTime, value);
    }

    public string FormattedDate
    {
        get => _formattedDate;
        private set => SetProperty(ref _formattedDate, value);
    }

    public string TimeZoneLabel
    {
        get => _timeZoneLabel;
        private set => SetProperty(ref _timeZoneLabel, value);
    }

    public string DayOfWeek
    {
        get => _dayOfWeek;
        private set => SetProperty(ref _dayOfWeek, value);
    }

    public bool IsUsingSystemTime
    {
        get => _isUsingSystemTime;
        private set => SetProperty(ref _isUsingSystemTime, value);
    }
    public bool ShowSecondaryDate
    {
        get => _showSecondaryDate;
        private set => SetProperty(ref _showSecondaryDate, value);
    }

    public string FormattedSecondaryDate
    {
        get => _formattedSecondaryDate;
        private set => SetProperty(ref _formattedSecondaryDate, value);
    }
    public double WidgetWidth => Settings.Size.GetDimensions().Width;
    public double WidgetHeight => Settings.Size.GetDimensions().Height;
    public double TimeFontSize => Settings.Size.GetFontSize();
    public double DateFontSize => Settings.Size.GetDateFontSize();
    public bool IsPersianPrimary => Settings.Language == WidgetLanguage.Persian;
    public bool IsPersianSecondary => Settings.ShowBothDates &&
                                  Settings.Language != WidgetLanguage.Persian;
    public Brush BackgroundBrush
    {
        get
        {
            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(Settings.WidgetColor)!;
                brush.Opacity = Settings.Opacity;
                brush.Freeze();
                return brush;
            }
            catch
            {
                return new SolidColorBrush(Color.FromArgb(200, 30, 30, 46));
            }
        }
    }

    public void Start()
    {
        _uiTimer.Start();
        Tick();
        _ = _timeService.SyncFromApiAsync(Settings.TimeZoneId, CancellationToken.None);
    }

    public void Stop() => _uiTimer.Stop();

    public void ApplySettings(WidgetSettings newSettings)
    {
        Settings = newSettings;

        RefreshFormatted();
        OnPropertyChanged(nameof(WidgetWidth));
        OnPropertyChanged(nameof(WidgetHeight));
        OnPropertyChanged(nameof(TimeFontSize));
        OnPropertyChanged(nameof(DateFontSize));
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(IsPersianPrimary));
    }

    public void SaveSettings() => _settingsService.Save(Settings);

    private void Tick()
    {
        CurrentTime = _timeService.GetCurrentTime(Settings.TimeZoneId);
    }

    private void OnTimeUpdated(object? sender, DateTime e)
    {
        IsUsingSystemTime = _timeService.IsUsingSystemTimeFallback;
        CurrentTime = e;
    }

    private void RefreshFormatted()
    {
        var local = DateTime.Now;

        var timeFormat = Settings.Use24HourFormat
            ? (Settings.ShowSeconds ? "HH:mm:ss" : "HH:mm")
            : (Settings.ShowSeconds ? "hh:mm:ss tt" : "hh:mm tt");

        FormattedTime = local.ToString(timeFormat);

        if (Settings.Language == WidgetLanguage.Persian)
        {
            FormattedDate = DateFormatter.FormatPersianDate(local);
            FormattedSecondaryDate = DateFormatter.FormatEnglishDate(local);
        }
        else
        {
            FormattedDate = DateFormatter.FormatEnglishDate(local);
            FormattedSecondaryDate = DateFormatter.FormatPersianDate(local);
        }

        ShowSecondaryDate = Settings.ShowBothDates;
        OnPropertyChanged(nameof(IsPersianPrimary));
    }

    public void Dispose()
    {
        _timeService.TimeUpdated -= OnTimeUpdated;
        _uiTimer.Stop();
    }
}