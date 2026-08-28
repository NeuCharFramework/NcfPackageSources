/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfPackageMirrorService.cs
    文件功能描述：NcfPackageMirrorService 相关实现
    
    
    创建标识：Senparc - 20260504
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260726
    修改描述：v0.3.0-preview2 改进固件更新镜像源选择与日志摘要

    修改标识：Senparc - 20260802
    修改描述：将 NCF Host 与 NcfDesktop 发布包分通道同步、校验并原子发布清单

    修改标识：Senparc - 20260804
    修改描述：v0.4.0-preview5 扩展 NCF 与 NcfDesktop 发布包同步能力

    修改标识：Senparc - 20260808
    修改描述：友好显示镜像同步异常，并将完整诊断写入 SenparcTrace

----------------------------------------------------------------*/

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Service;

namespace Senparc.Xncf.FirmwareUpdate.Domain.Services;

/// <summary>
/// 从 GitHub 拉取 NCF Host 运行包与 NcfDesktop 桌面应用的 Release 资源，分别写入
/// wwwroot/NcfPackages/host 与 wwwroot/NcfPackages/desktop，并原子发布各自的最新版本元数据。
/// </summary>
public class NcfPackageMirrorService
{
    public const string GitHubReleasesApi = "https://api.github.com/repos/NeuCharFramework/NCF/releases";
    public const string NcfDesktopGitHubReleasesApi = "https://api.github.com/repos/NeuCharFramework/NcfDesktop/releases";
    /// <summary>与 NcfDesktopApp 备用地址一致。</summary>
    public const string PublicPackageBaseUrl = "https://www.ncf.pub/NcfPackages";
    public const string LatestReleaseFileName = "latest-release.json";
    public const string LatestDesktopReleaseFileName = "latest-desktop-release.json";
    public const string HostPackageFolderName = "host";
    public const string DesktopPackageFolderName = "desktop";

    private static readonly SemaphoreSlim SyncGate = new(1, 1);
    private static readonly TimeSpan GitHubMetadataTimeout = TimeSpan.FromSeconds(30);
    private static readonly string[] SupportedRuntimeIdentifiers =
    [
        "linux-arm64",
        "linux-x64",
        "osx-arm64",
        "osx-x64",
        "win-arm64",
        "win-x64"
    ];

    internal static MirrorFeedDefinition HostFeedDefinition { get; } = new(
        "NCF Host",
        GitHubReleasesApi,
        HostPackageFolderName,
        LatestReleaseFileName,
        "ncf-",
        KeepVersionCount: 3,
        MaxPages: 5);

    internal static MirrorFeedDefinition DesktopFeedDefinition { get; } = new(
        "NcfDesktop",
        NcfDesktopGitHubReleasesApi,
        DesktopPackageFolderName,
        LatestDesktopReleaseFileName,
        "ncf-desktop-",
        KeepVersionCount: 1,
        MaxPages: 1);

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

    /// <param name="manualTrigger">为 true 时忽略「是否启用」与「距上次完整同步间隔」。</param>
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
                    return $"距上次完整同步不足 {hours} 小时，已跳过。";
                }
            }

            var root = GetLocalPackageRoot();
            Directory.CreateDirectory(root);
            var client = _httpClientFactory.CreateClient("Senparc.Xncf.FirmwareUpdate.GitHub");
            var results = new List<MirrorFeedSyncResult>();

            foreach (var feed in new[] { HostFeedDefinition, DesktopFeedDefinition })
            {
                try
                {
                    results.Add(await SyncFeedAsync(client, root, feed, cancellationToken).ConfigureAwait(false));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var reason = GetSyncFailureReason(ex);
                    var retryHint = manualTrigger ? "可稍后重试" : "将在下次计划任务中重试";
                    var message = $"{feed.DisplayName} 同步失败：{reason}；已保留原有清单，{retryHint}";
                    LogSyncFailure(feed.DisplayName, message, ex);
                    results.Add(new MirrorFeedSyncResult(feed.DisplayName, false, false, message));
                }
            }

            var fullySynchronized = results.All(result => result.IsComplete);
            if (fullySynchronized)
            {
                config.LastPeriodicSyncUtc = DateTime.UtcNow;
                await configService.SaveObjectAsync(config).ConfigureAwait(false);
            }

            var summary = string.Join("；", results.Select(result => result.Message));
            if (fullySynchronized)
            {
                return $"同步完成。{summary}";
            }

            var publishedAny = results.Any(result => result.LatestPublished);
            return publishedAny
                ? $"同步部分完成。{summary}；未更新完整同步时间，将在下次继续重试。"
                : $"同步未完成。{summary}";
        }
        finally
        {
            SyncGate.Release();
        }
    }

    internal async Task<MirrorFeedSyncResult> SyncFeedAsync(
        HttpClient client,
        string root,
        MirrorFeedDefinition feed,
        CancellationToken cancellationToken)
    {
        using var metadataTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        metadataTimeoutCts.CancelAfter(GitHubMetadataTimeout);

        var latestTask = FetchLatestReleaseAsync(client, feed.ReleasesApi, metadataTimeoutCts.Token);
        var historyTask = FetchReleasesAsync(client, feed.ReleasesApi, feed.MaxPages, metadataTimeoutCts.Token);
        await Task.WhenAll(latestTask, historyTask).ConfigureAwait(false);

        var latest = await latestTask.ConfigureAwait(false);
        var history = await historyTask.ConfigureAwait(false);
        var selectedReleases = new[] { latest }
            .Concat(history.Where(release => !string.Equals(release.TagName, latest.TagName, StringComparison.OrdinalIgnoreCase)))
            .Take(feed.KeepVersionCount)
            .ToList();

        var feedRoot = Path.Combine(root, feed.FolderName);
        Directory.CreateDirectory(feedRoot);

        var latestAssets = SelectExpectedAssets(latest, feed);
        await MirrorReleaseAssetsAsync(client, feedRoot, latest, latestAssets, cancellationToken).ConfigureAwait(false);
        await WriteLatestReleaseJsonAsync(root, feed, latest, latestAssets, cancellationToken).ConfigureAwait(false);

        var retentionFailures = new List<string>();
        foreach (var release in selectedReleases.Skip(1))
        {
            try
            {
                var assets = SelectExpectedAssets(release, feed);
                await MirrorReleaseAssetsAsync(client, feedRoot, release, assets, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var reason = GetSyncFailureReason(ex);
                retentionFailures.Add($"{release.TagName}: {reason}");
                LogSyncFailure(
                    feed.DisplayName,
                    $"{feed.DisplayName} 历史版本 {release.TagName} 同步失败：{reason}；已保留现有历史目录",
                    ex);
            }
        }

        if (retentionFailures.Count == 0)
        {
            PruneOldVersionFolders(feedRoot, selectedReleases.Select(release => release.TagName!).ToArray());
        }

        var latestMessage = $"{feed.DisplayName} 已发布 {latest.TagName}（{latestAssets.Count} 个 ZIP）";
        if (retentionFailures.Count == 0)
        {
            return new MirrorFeedSyncResult(feed.DisplayName, true, true, latestMessage);
        }

        return new MirrorFeedSyncResult(
            feed.DisplayName,
            true,
            false,
            $"{latestMessage}，但历史保留版本同步失败：{string.Join("、", retentionFailures)}");
    }

    private async Task<GitHubReleaseDto> FetchLatestReleaseAsync(
        HttpClient client,
        string releasesApi,
        CancellationToken cancellationToken)
    {
        var url = $"{releasesApi}/latest";
        _logger.LogInformation("FirmwareUpdate: GET {Url}", url);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var release = await client.GetFromJsonAsync<GitHubReleaseDto>(url, options, cancellationToken).ConfigureAwait(false);
        if (release == null ||
            release.Draft ||
            release.Prerelease ||
            string.IsNullOrWhiteSpace(release.TagName) ||
            release.Assets is not { Length: > 0 })
        {
            throw new InvalidDataException($"{releasesApi} 未返回有效的正式 Release。");
        }

        return release;
    }

    private async Task<List<GitHubReleaseDto>> FetchReleasesAsync(
        HttpClient client,
        string releasesApi,
        int maxPages,
        CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = new List<GitHubReleaseDto>();
        for (var page = 1; page <= maxPages; page++)
        {
            var url = $"{releasesApi}?per_page=30&page={page}";
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
            .Where(release =>
                !release.Draft &&
                !release.Prerelease &&
                !string.IsNullOrWhiteSpace(release.TagName) &&
                release.Assets is { Length: > 0 })
            .OrderByDescending(release => release.PublishedAt ?? DateTime.MinValue)
            .ToList();
    }

    internal static IReadOnlyList<GitHubAssetDto> SelectExpectedAssets(
        GitHubReleaseDto release,
        MirrorFeedDefinition feed)
    {
        var assets = release.Assets ?? [];
        var selected = new List<GitHubAssetDto>(SupportedRuntimeIdentifiers.Length);

        foreach (var runtimeIdentifier in SupportedRuntimeIdentifiers)
        {
            var expectedPrefix = $"{feed.AssetNamePrefix}{runtimeIdentifier}-";
            var matches = assets
                .Where(asset =>
                    asset.Name?.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) == true &&
                    asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length != 1)
            {
                throw new InvalidDataException(
                    $"Release {release.TagName} 应包含且仅包含一个 {expectedPrefix}*.zip，实际为 {matches.Length} 个。");
            }

            ValidateAsset(matches[0], release.TagName);
            selected.Add(matches[0]);
        }

        return selected;
    }

    private static void ValidateAsset(GitHubAssetDto asset, string? tagName)
    {
        if (string.IsNullOrWhiteSpace(asset.Name) ||
            !string.Equals(Path.GetFileName(asset.Name), asset.Name, StringComparison.Ordinal) ||
            asset.Size <= 0)
        {
            throw new InvalidDataException($"Release {tagName} 包含无效的资源名称或大小。");
        }

        if (!Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Release {tagName} 的资源 {asset.Name} 不是受信任的 GitHub HTTPS 下载地址。");
        }

        _ = GetExpectedSha256(asset, tagName);
    }

    private static async Task MirrorReleaseAssetsAsync(
        HttpClient client,
        string feedRoot,
        GitHubReleaseDto release,
        IReadOnlyList<GitHubAssetDto> assets,
        CancellationToken cancellationToken)
    {
        var tag = release.TagName!;
        var directory = Path.Combine(feedRoot, MakeSafeDirectorySegment(tag));
        Directory.CreateDirectory(directory);

        foreach (var asset in assets)
        {
            var targetPath = Path.Combine(directory, asset.Name!);
            var expectedSha256 = GetExpectedSha256(asset, tag);
            if (File.Exists(targetPath) &&
                new FileInfo(targetPath).Length == asset.Size &&
                string.Equals(
                    await ComputeSha256Async(targetPath, cancellationToken).ConfigureAwait(false),
                    expectedSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tempPath = $"{targetPath}.tmp-{Guid.NewGuid():N}";
            try
            {
                using var response = await client.GetAsync(
                    asset.BrowserDownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
                }

                var actualSize = new FileInfo(tempPath).Length;
                if (actualSize != asset.Size)
                {
                    throw new InvalidDataException(
                        $"资源 {asset.Name} 大小校验失败：期望 {asset.Size}，实际 {actualSize}。");
                }

                var actualSha256 = await ComputeSha256Async(tempPath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"资源 {asset.Name} SHA-256 校验失败。");
                }

                File.Move(tempPath, targetPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }

    private static async Task WriteLatestReleaseJsonAsync(
        string root,
        MirrorFeedDefinition feed,
        GitHubReleaseDto latest,
        IReadOnlyList<GitHubAssetDto> assets,
        CancellationToken cancellationToken)
    {
        var tagSegment = MakeSafeDirectorySegment(latest.TagName!);
        var versionRoot = Path.Combine(root, feed.FolderName, tagSegment);
        var mirroredAssets = new List<GitHubAssetMirrorDto>(assets.Count);
        foreach (var asset in assets)
        {
            var assetPath = Path.Combine(versionRoot, asset.Name!);
            mirroredAssets.Add(new GitHubAssetMirrorDto
            {
                Name = asset.Name,
                Size = asset.Size,
                Md5 = await ComputeMd5Async(assetPath, cancellationToken).ConfigureAwait(false),
                BrowserDownloadUrl = $"{PublicPackageBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(feed.FolderName)}/{Uri.EscapeDataString(tagSegment)}/{Uri.EscapeDataString(asset.Name!)}"
            });
        }

        var mirror = new GitHubReleaseMirrorDto
        {
            TagName = latest.TagName,
            Name = latest.Name,
            Assets = mirroredAssets.ToArray()
        };

        var path = Path.Combine(root, feed.LatestFileName);
        var tempPath = $"{path}.tmp-{Guid.NewGuid():N}";
        try
        {
            var json = JsonSerializer.Serialize(mirror, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static void PruneOldVersionFolders(string feedRoot, IReadOnlyCollection<string> keepTags)
    {
        var keepSafe = new HashSet<string>(keepTags.Select(MakeSafeDirectorySegment), StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(feedRoot))
        {
            return;
        }

        foreach (var subdirectory in Directory.GetDirectories(feedRoot))
        {
            var name = Path.GetFileName(subdirectory);
            if (keepSafe.Contains(name))
            {
                continue;
            }

            try
            {
                Directory.Delete(subdirectory, recursive: true);
            }
            catch
            {
                // 删除失败不会影响已发布清单；下次同步继续尝试。
            }
        }
    }

    private static string GetExpectedSha256(GitHubAssetDto asset, string? tagName)
    {
        const string prefix = "sha256:";
        if (asset.Digest?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new InvalidDataException($"Release {tagName} 的资源 {asset.Name} 缺少 SHA-256 摘要。");
        }

        var value = asset.Digest[prefix.Length..];
        if (value.Length != 64 || !value.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException($"Release {tagName} 的资源 {asset.Name} SHA-256 摘要格式无效。");
        }

        return value;
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    private static async Task<string> ComputeMd5Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await MD5.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    private void LogSyncFailure(string feedName, string message, Exception exception)
    {
        // 控制台只呈现可操作的摘要，完整异常留在 SenparcTrace 中供诊断。
        _logger.LogWarning("FirmwareUpdate: {Message}", message);
        SenparcTrace.SendCustomLog(
            $"FirmwareUpdate.{feedName}.SyncFailure",
            $"{message}{Environment.NewLine}{exception}");
    }

    internal static string GetSyncFailureReason(Exception exception)
    {
        var exceptions = EnumerateExceptionChain(exception).ToArray();

        if (exceptions.Any(ex =>
                ex is TaskCanceledException or TimeoutException ||
                string.Equals(
                    ex.GetType().FullName,
                    "Polly.Timeout.TimeoutRejectedException",
                    StringComparison.Ordinal)))
        {
            return $"请求超时（{GitHubMetadataTimeout.TotalSeconds:0} 秒，可能是网络、代理或 GitHub 暂时不可用）";
        }

        var httpException = exceptions.OfType<HttpRequestException>().FirstOrDefault();
        if (httpException?.StatusCode is { } statusCode)
        {
            return $"GitHub 返回 HTTP {(int)statusCode}";
        }

        if (httpException != null)
        {
            return "无法连接 GitHub（请检查网络或代理）";
        }

        if (exceptions.Any(ex => ex is JsonException))
        {
            return "GitHub 返回的数据格式无效";
        }

        if (exceptions.OfType<InvalidDataException>().FirstOrDefault() is { } invalidDataException)
        {
            return invalidDataException.Message;
        }

        if (exceptions.OfType<IOException>().FirstOrDefault() is { } ioException)
        {
            return $"文件读写失败：{ioException.Message}";
        }

        return "发生未预期错误，请查看 SenparcTrace 日志";
    }

    private static IEnumerable<Exception> EnumerateExceptionChain(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            yield return current;
        }
    }

    /// <summary>用于目录名与 URL 段，避免非法路径字符。</summary>
    public static string MakeSafeDirectorySegment(string tag)
    {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars())
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };
        var chars = tag.Select(character => invalid.Contains(character) ? '_' : character).ToArray();
        var safe = new string(chars);
        return string.IsNullOrWhiteSpace(safe) || safe is "." or ".." ? "_" : safe;
    }

    internal sealed record MirrorFeedDefinition(
        string DisplayName,
        string ReleasesApi,
        string FolderName,
        string LatestFileName,
        string AssetNamePrefix,
        int KeepVersionCount,
        int MaxPages);

    internal sealed record MirrorFeedSyncResult(
        string DisplayName,
        bool LatestPublished,
        bool RetentionComplete,
        string Message)
    {
        public bool IsComplete => LatestPublished && RetentionComplete;
    }

    internal sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAssetDto[]? Assets { get; set; }
    }

    internal sealed class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("digest")]
        public string? Digest { get; set; }
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

        [JsonPropertyName("md5")]
        public string? Md5 { get; set; }
    }
}
