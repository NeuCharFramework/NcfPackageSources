using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NcfDesktopApp.GUI.Models;

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
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
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
        var environment = string.Equals(settings.AspNetCoreEnvironment, "Development", System.StringComparison.OrdinalIgnoreCase)
            ? "Development"
            : "Production";
        var recentPaths = (settings.RecentNcfPaths ?? new())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        var toWrite = new DesktopUserSettings
        {
            MirrorServerBaseUrl = normalized,
            LaunchTargetKind = settings.LaunchTargetKind,
            ExternalNcfPath = settings.ExternalNcfPath?.Trim() ?? string.Empty,
            RecentNcfPaths = recentPaths,
            AspNetCoreEnvironment = environment
        };
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(toWrite, JsonOptions));
    }
}
