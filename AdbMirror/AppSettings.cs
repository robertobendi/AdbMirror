using System;
using System.IO;
using System.Text.Json;
using AdbMirror.Core;

namespace AdbMirror;

/// <summary>
/// Simple persisted settings for the Phone Mirror app.
/// </summary>
public sealed class AppSettings
{
    public ScrcpyPreset DefaultPreset { get; set; } = ScrcpyPreset.Balanced;
    public bool AutoMirrorOnConnect { get; set; } = false;
    public bool StartFullscreen { get; set; } = false;
    public bool KeepScreenAwake { get; set; } = true;

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "AdbMirror");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var path = GetSettingsPath();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // ignore
        }
    }
}


