/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfPackageMirrorService.cs
    文件功能描述：NcfPackageMirrorService 相关实现
    
    
    创建标识：Senparc - 20260504
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Service;

namespace Senparc.Xncf.FirmwareUpdate.Domain.Services;

/// <summary>
/// 从 GitHub 拉取 NeuCharFramework/NCF 的 Release 资源，写入当前站点 wwwroot 下的 NcfPackages 目录，并生成 latest-release.json（供 ncf.pub 镜像与桌面端 Plan B）。
/// </summary>
public class NcfPackageMirrorService
{
    public const string GitHubReleasesApi = "https://api.github.com/repos/NeuCharFramework/NCF/releases";
    /// <summary>与 NcfDesktopApp 备用地址一致</summary>
    public const string PublicPackageBaseUrl = "https://www.ncf.pub/NcfPackages";
    public const string LatestReleaseFileName = "latest-release.json";

    private static readonly SemaphoreSlim SyncGate = new(1, 1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NcfPackageMirrorService> _logger;

    public NcfPackageMirrorService(IHttpClientFactory httpClientFactory, ILogger<NcfPackageMirrorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 安装包本地根目录：优先 <see cref="SiteConfig.WebRootPath"/>/NcfPackages，其次 ContentRoot/wwwroot/NcfPackages，最后回退到用户主目录（设计时等场景）。
    /// </summary>
    public static string GetLocalPackageRoot()
    {
        if (!string.IsNullOrWhiteSpace(SiteConfig.WebRootPath))
        {
            return Path.Combine(SiteConfig.WebRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "NcfPackages");
        }

        if (!string.IsNullOrWhiteSpace(SiteConfig.ApplicationPath))
        {
            return Path.Combine(SiteConfig.ApplicationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "wwwroot", "NcfPackages");
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "wwwroot",
            "NcfPackages");
        SenparcTrace.SendCustomLog("FirmwareUpdate", $"SiteConfig.WebRootPath/ApplicationPath 未设置，已回退到用户目录：{fallback}");
        return fallback;
    }

    /// <param name="manualTrigger">为 true 时忽略「是否启用」与「距上次同步间隔」。</param>
    public async Task<string> RunAsync(IServiceProvider serviceProvider, bool manualTrigger, CancellationToken cancellationToken = default)
    {
        await SyncGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var configService = serviceProvider.GetRequiredService<ServiceBase<FirmwareUpdateConfig>>();
            var config = await configService.GetObjectAsync(_ => true).ConfigureAwait(false);
            if (config == null)
            {
                return "未找到 FirmwareUpdateConfig，请先完成模块安装。";
            }

            if (!manualTrigger)
            {
                if (!config.AutoMirrorEnabled)
                {
                    return "自动镜像未启用，已跳过。";
                }

                var hours = Math.Clamp(config.UpdateIntervalHours, 1, 24);
                if (config.LastPeriodicSyncUtc is { } last &&
                    DateTime.UtcNow - last < TimeSpan.FromHours(hours))
                {
                    return $"距上次同步不足 {hours} 小时，已跳过。";
                }
            }

            var client = _httpClientFactory.CreateClient("Senparc.Xncf.FirmwareUpdate.GitHub");
            var releases = await FetchReleasesAsync(client, cancellationToken).ConfigureAwait(false);
            if (releases.Count == 0)
            {
                return "GitHub 未返回任何 Release。";
            }

            var root = GetLocalPackageRoot();
            Directory.CreateDirectory(root);

            const int keepVersionCount = 3;
            var topReleases = releases.Take(keepVersionCount).ToList();
            var orderedTags = topReleases.Select(r => r.TagName!).ToList();

            foreach (var rel in topReleases)
            {
                await MirrorReleaseAssetsAsync(client, root, rel, cancellationToken).ConfigureAwait(false);
            }

            await WriteLatestReleaseJsonAsync(root, releases[0], cancellationToken).ConfigureAwait(false);
            PruneOldVersionFolders(root, orderedTags);

            config.LastPeriodicSyncUtc = DateTime.UtcNow;
            await configService.SaveObjectAsync(config).ConfigureAwait(false);

            return $"同步完成。已维护最近 {keepVersionCount} 个版本的包目录，并已更新 {LatestReleaseFileName}。";
        }
        finally
        {
            SyncGate.Release();
        }
    }

    private async Task<List<GitHubReleaseDto>> FetchReleasesAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = new List<GitHubReleaseDto>();
        for (var page = 1; page <= 5; page++)
        {
            var url = $"{GitHubReleasesApi}?per_page=30&page={page}";
            _logger.LogInformation("FirmwareUpdate: GET {Url}", url);
            var batch = await client.GetFromJsonAsync<List<GitHubReleaseDto>>(url, options, cancellationToken).ConfigureAwait(false);
            if (batch == null || batch.Count == 0)
            {
                break;
            }

            list.AddRange(batch);
            if (batch.Count < 30)
            {
                break;
            }
        }

        return list
            .Where(r => !string.IsNullOrWhiteSpace(r.TagName) && r.Assets is { Length: > 0 })
            .OrderByDescending(r => r.PublishedAt ?? DateTime.MinValue)
            .ToList();
    }

    private static async Task MirrorReleaseAssetsAsync(HttpClient client, string root, GitHubReleaseDto release, CancellationToken cancellationToken)
    {
        var tag = release.TagName!;
        var dir = Path.Combine(root, MakeSafeDirectorySegment(tag));
        Directory.CreateDirectory(dir);

        foreach (var asset in release.Assets ?? Array.Empty<GitHubAssetDto>())
        {
            if (string.IsNullOrWhiteSpace(asset.Name) || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            {
                continue;
            }

            var targetPath = Path.Combine(dir, asset.Name);
            if (File.Exists(targetPath))
            {
                var fi = new FileInfo(targetPath);
                if (fi.Length == asset.Size && asset.Size > 0)
                {
                    continue;
                }
            }

            using var resp = await client.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var temp = targetPath + ".tmp";
            await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(temp, targetPath);
        }
    }

    private static async Task WriteLatestReleaseJsonAsync(string root, GitHubReleaseDto latest, CancellationToken cancellationToken)
    {
        var tagSeg = MakeSafeDirectorySegment(latest.TagName!);
        var mirror = new GitHubReleaseMirrorDto
        {
            TagName = latest.TagName,
            Name = latest.Name,
            Assets = (latest.Assets ?? Array.Empty<GitHubAssetDto>())
                .Where(a => !string.IsNullOrWhiteSpace(a.Name))
                .Select(a => new GitHubAssetMirrorDto
                {
                    Name = a.Name,
                    Size = a.Size,
                    BrowserDownloadUrl = $"{PublicPackageBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(tagSeg)}/{Uri.EscapeDataString(a.Name ?? string.Empty)}"
                })
                .ToArray()
        };

        var path = Path.Combine(root, LatestReleaseFileName);
        var json = JsonSerializer.Serialize(mirror, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    private static void PruneOldVersionFolders(string root, IReadOnlyCollection<string> keepTags)
    {
        var keepSafe = new HashSet<string>(keepTags.Select(MakeSafeDirectorySegment), StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var sub in Directory.GetDirectories(root))
        {
            var name = Path.GetFileName(sub);
            if (keepSafe.Contains(name))
            {
                continue;
            }

            try
            {
                Directory.Delete(sub, recursive: true);
            }
            catch
            {
                // 忽略删除失败（可能被占用）
            }
        }
    }

    /// <summary>用于目录名与 URL 段，避免非法路径字符。</summary>
    public static string MakeSafeDirectorySegment(string tag)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = tag.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAssetDto[]? Assets { get; set; }
    }

    private sealed class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    private sealed class GitHubReleaseMirrorDto
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAssetMirrorDto[]? Assets { get; set; }
    }

    private sealed class GitHubAssetMirrorDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
