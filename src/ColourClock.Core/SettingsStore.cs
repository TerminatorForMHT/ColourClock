using System.Text.Json;

namespace TheColourClock.Core;

public sealed class AppSettings
{
    public DisplayMode Mode { get; set; } = DisplayMode.Clock;
}

/// <summary>
/// Replaces the original Flash SharedObject ("swf-screensaver") with a JSON file
/// under %LOCALAPPDATA%\TheColourClock\settings.json.
/// </summary>
public static class SettingsStore
{
    private static AppSettings _current = new();

    public static event EventHandler? Changed;

    public static AppSettings Current => _current;

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheColourClock");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is not null)
                {
                    _current = loaded;
                    if (!Enum.IsDefined(_current.Mode))
                    {
                        _current.Mode = DisplayMode.Clock;
                    }
                }
            }
        }
        catch
        {
            _current = new AppSettings();
        }
    }

    public static void SetMode(DisplayMode mode)
    {
        if (_current.Mode == mode)
        {
            return;
        }

        _current.Mode = mode;
        Save();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Settings are best effort; never crash the screensaver.
        }
    }
}
