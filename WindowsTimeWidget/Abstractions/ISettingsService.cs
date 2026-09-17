using WindowsTimeWidget.Models;

namespace WindowsTimeWidget.Abstractions;

public interface ISettingsService
{
    /// <summary>
    /// Loads settings from disk. Returns defaults if no file exists or on error.
    /// </summary>
    WidgetSettings Load();

    /// <summary>
    /// Persists settings to disk. Fails silently (logs) on error.
    /// </summary>
    void Save(WidgetSettings settings);

    /// <summary>
    /// Raised after a successful Save.
    /// </summary>
    event EventHandler<WidgetSettings>? SettingsChanged;
}