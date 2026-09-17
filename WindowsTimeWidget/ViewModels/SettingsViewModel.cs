using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Models;

namespace WindowsTimeWidget.ViewModels;


public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ITimeService _timeService;

    private WidgetSettings _workingCopy;
    private TimeZoneItem? _selectedTimeZone;
    private string _colorHex = "#CC1E1E2E";
    private WidgetSize _selectedSize = WidgetSize.Medium;
    private double _opacity = 0.9;
    private bool _use24Hour = true;
    private bool _showSeconds = true;
    private bool _showDate = true;

    public SettingsViewModel(ISettingsService settingsService, ITimeService timeService)
    {
        _settingsService = settingsService;
        _timeService = timeService;

        _workingCopy = _settingsService.Load();

        TimeZones = new ObservableCollection<TimeZoneItem>(
            TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => new TimeZoneItem
                {
                    Id = tz.Id,
                    DisplayName = tz.DisplayName,
                    Offset = tz.BaseUtcOffset
                })
                .OrderBy(tz => tz.Offset)
                .ThenBy(tz => tz.DisplayName));

        LoadFromSettings(_workingCopy);

        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => Cancel());
        ResetCommand = new RelayCommand(_ => LoadFromSettings(new WidgetSettings()));
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());

        SaveRequested += _ => { };
    }

    public event Action<WidgetSettings> SaveRequested;
    public event Action? CloseRequested;

    public ObservableCollection<TimeZoneItem> TimeZones { get; }
    public IReadOnlyList<WidgetSize> Sizes { get; } = Enum.GetValues<WidgetSize>();

    public TimeZoneItem? SelectedTimeZone
    {
        get => _selectedTimeZone;
        set => SetProperty(ref _selectedTimeZone, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set => SetProperty(ref _colorHex, value);
    }

    public WidgetSize SelectedSize
    {
        get => _selectedSize;
        set => SetProperty(ref _selectedSize, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

    public bool Use24Hour
    {
        get => _use24Hour;
        set => SetProperty(ref _use24Hour, value);
    }

    public bool ShowSeconds
    {
        get => _showSeconds;
        set => SetProperty(ref _showSeconds, value);
    }

    public bool ShowDate
    {
        get => _showDate;
        set => SetProperty(ref _showDate, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand CloseCommand { get; }


    private void LoadFromSettings(WidgetSettings s)
    {
        _workingCopy = s.Clone();

        SelectedTimeZone = TimeZones.FirstOrDefault(tz => tz.Id == s.TimeZoneId)
                           ?? TimeZones.FirstOrDefault(tz => tz.Id == TimeZoneInfo.Local.Id)
                           ?? TimeZones.FirstOrDefault();

        ColorHex = s.WidgetColor;
        SelectedSize = s.Size;
        Opacity = s.Opacity;
        Use24Hour = s.Use24HourFormat;
        ShowSeconds = s.ShowSeconds;
        ShowDate = s.ShowDate;
    }

    private void Save()
    {
        _workingCopy.TimeZoneId = SelectedTimeZone?.Id ?? TimeZoneInfo.Local.Id;
        _workingCopy.WidgetColor = NormalizeHex(ColorHex);
        _workingCopy.Size = SelectedSize;
        _workingCopy.Opacity = Opacity;
        _workingCopy.Use24HourFormat = Use24Hour;
        _workingCopy.ShowSeconds = ShowSeconds;
        _workingCopy.ShowDate = ShowDate;

        _settingsService.Save(_workingCopy);
        _ = _timeService.SyncFromApiAsync(_workingCopy.TimeZoneId, CancellationToken.None);

        SaveRequested?.Invoke(_workingCopy);
        CloseRequested?.Invoke();
    }

    private void Cancel() => CloseRequested?.Invoke();

    private static string NormalizeHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return "#CC1E1E2E";
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        if (hex.Length == 7) hex = "#CC" + hex[1..];
        return hex;
    }
}