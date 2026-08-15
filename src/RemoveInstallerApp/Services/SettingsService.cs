using System.Text.Json;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

/// <summary>
/// Small JSON-file-backed settings store under %LOCALAPPDATA%. The app is unpackaged (no MSIX
/// identity), so Windows.Storage.ApplicationData isn't available — a plain file is simplest.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RemoveInstallerApp",
        "settings.json");

    public AppSettings Current { get; }

    public SettingsService()
    {
        Current = Load();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable settings file: fall back to defaults rather than crash on launch.
        }

        return new AppSettings
        {
            Language = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase)
                ? "vi-VN"
                : "en-US",
        };
    }
}
