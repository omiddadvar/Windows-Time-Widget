using System.IO;
using System.Text.Json;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Models;

namespace WindowsTimeWidget.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;
    private readonly object _gate = new();
    private WidgetSettings _cached;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DateTimeWidget");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");

        _cached = LoadInternal() ?? new WidgetSettings();
    }

    public event EventHandler<WidgetSettings>? SettingsChanged;

    public WidgetSettings Load()
    {
        lock (_gate) return _cached.Clone();
    }

    public void Save(WidgetSettings settings)
    {
        if (settings is null || !settings.IsValid)
        {
            return;
        }

        try
        {
            lock (_gate)
            {
                var json = JsonSerializer.Serialize(settings, SerializerOptions);
                File.WriteAllText(_settingsPath, json);
                _cached = settings.Clone();
            }

            SettingsChanged?.Invoke(this, settings.Clone());
        }
        catch { }
    }

    private WidgetSettings? LoadInternal()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<WidgetSettings>(json, SerializerOptions);
            return loaded;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}