using System.IO;
using System.Text.Json;

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// 将桌面用户设置读写至 AppData 目录下的 JSON 文件。
/// </summary>
public static class DesktopSettingsStore
{
    private const string FileName = "desktop-user-settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string SettingsFilePath => Path.Combine(NcfService.AppDataPath, FileName);

    public static string NormalizeMirrorServerBase(string? url)
    {
        var s = (url ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(s))
        {
            return DesktopUserSettings.DefaultMirrorServerBaseUrl.TrimEnd('/');
        }

        return s.TrimEnd('/');
    }

    public static DesktopUserSettings Load()
    {
        try
        {
            var path = SettingsFilePath;
            if (!File.Exists(path))
            {
                return new DesktopUserSettings();
            }

            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<DesktopUserSettings>(json, JsonOptions);
            return loaded ?? new DesktopUserSettings();
        }
        catch
        {
            return new DesktopUserSettings();
        }
    }

    public static void Save(DesktopUserSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        Directory.CreateDirectory(NcfService.AppDataPath);
        var normalized = NormalizeMirrorServerBase(settings.MirrorServerBaseUrl);
        var toWrite = new DesktopUserSettings { MirrorServerBaseUrl = normalized };
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(toWrite, JsonOptions));
    }
}
