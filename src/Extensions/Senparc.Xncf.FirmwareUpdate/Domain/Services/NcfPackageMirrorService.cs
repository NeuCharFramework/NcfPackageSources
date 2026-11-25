/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfPackageMirrorService.cs
    File description: NcfPackageMirrorService related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260726
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260802
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.4.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260808
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.4.5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

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
/// Mirrors validated NCF host and desktop release assets into the local public package feed.
/// </summary>
public class NcfPackageMirrorService
{
    public const string GitHubReleasesApi = "https://api.github.com/repos/NeuCharFramework/NCF/releases";
    public const string NcfDesktopGitHubReleasesApi = "https://api.github.com/repos/NeuCharFramework/NcfDesktop/releases";
    ///Legacy comment translated to English.
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

    /// <summary>
    /// Initializes the package mirror service.
    /// </summary>
    /// <param name="httpClientFactory">Factory for the configured GitHub HTTP client.</param>
    /// <param name="logger">Mirror operation logger.</param>
    public NcfPackageMirrorService(IHttpClientFactory httpClientFactory, ILogger<NcfPackageMirrorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the local public package-feed root from site configuration with a user-directory fallback.
    /// </summary>
    /// <returns>The local package-feed directory.</returns>
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

    /// <summary>
    /// Synchronizes configured host and desktop release feeds, preserving existing metadata when a feed fails.
    /// </summary>
    /// <param name="serviceProvider">Provider used to load and save mirror configuration.</param>
    /// <param name="manualTrigger">Whether the synchronization was explicitly requested.</param>
    /// <param name="cancellationToken">Token used to cancel network and file operations.</param>
    /// <returns>A localized synchronization summary.</returns>
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

    /// <summary>
    /// Synchronizes the latest release and retained historical versions for one feed.
    /// </summary>
    /// <param name="client">HTTP client used to retrieve release metadata and assets.</param>
    /// <param name="root">The local package-feed root.</param>
    /// <param name="feed">Feed selection and retention policy.</param>
    /// <param name="cancellationToken">Token used to cancel synchronization.</param>
    /// <returns>The feed synchronization result.</returns>
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

    /// <summary>
    /// Fetches and validates the latest non-draft, non-prerelease release.
    /// </summary>
    /// <param name="client">The GitHub HTTP client.</param>
    /// <param name="releasesApi">The release API endpoint.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task containing the validated release.</returns>
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

    /// <summary>
    /// Fetches bounded release-history pages and returns valid published releases newest first.
    /// </summary>
    /// <param name="client">The GitHub HTTP client.</param>
    /// <param name="releasesApi">The release API endpoint.</param>
    /// <param name="maxPages">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">Token used to cancel requests.</param>
    /// <returns>A task containing valid published releases.</returns>
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

    /// <summary>
    /// Selects exactly one validated ZIP asset for each supported runtime identifier.
    /// </summary>
    /// <param name="release">The GitHub release.</param>
    /// <param name="feed">The feed asset naming policy.</param>
    /// <returns>The selected runtime assets.</returns>
    /// <exception cref="InvalidDataException">Thrown when a required runtime asset is missing or duplicated.</exception>
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

    /// <summary>
    /// Validates asset name, size, trusted HTTPS GitHub URL, and digest metadata.
    /// </summary>
    /// <param name="asset">The release asset.</param>
    /// <param name="tagName">The release tag used in diagnostics.</param>
    /// <exception cref="InvalidDataException">Thrown when asset metadata is unsafe or incomplete.</exception>
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

    /// <summary>
    /// Downloads release assets atomically and verifies size and SHA-256 before replacement.
    /// </summary>
    /// <param name="client">The GitHub HTTP client.</param>
    /// <param name="feedRoot">The local feed root.</param>
    /// <param name="release">The release being mirrored.</param>
    /// <param name="assets">The validated assets.</param>
    /// <param name="cancellationToken">Token used to cancel downloads.</param>
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

    /// <summary>
    /// Writes the feed's latest-release metadata atomically after computing local MD5 values.
    /// </summary>
    /// <param name="root">The package-feed root.</param>
    /// <param name="feed">The feed definition.</param>
    /// <param name="latest">The latest release.</param>
    /// <param name="assets">The mirrored assets.</param>
    /// <param name="cancellationToken">Token used to cancel metadata writing.</param>
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

    /// <summary>
    /// Removes version directories not included in the feed retention set.
    /// </summary>
    /// <param name="feedRoot">The local feed root.</param>
    /// <param name="keepTags">Release tags to retain.</param>
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
                //Legacy comment translated to English.
            }
        }
    }

    /// <summary>
    /// Extracts and validates the 64-hex-character SHA-256 digest from asset metadata.
    /// </summary>
    /// <param name="asset">The release asset.</param>
    /// <param name="tagName">The release tag used in diagnostics.</param>
    /// <returns>The expected SHA-256 digest.</returns>
    /// <exception cref="InvalidDataException">Thrown when the digest is missing or malformed.</exception>
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

    /// <summary>Computes a file's SHA-256 digest.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="cancellationToken">Token used to cancel hashing.</param>
    /// <returns>A task containing the lowercase hexadecimal digest.</returns>
    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Computes a file's MD5 digest for legacy feed metadata.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="cancellationToken">Token used to cancel hashing.</param>
    /// <returns>A task containing the lowercase hexadecimal digest.</returns>
    private static async Task<string> ComputeMd5Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await MD5.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Logs a feed synchronization failure to the configured logger and SenparcTrace.
    /// </summary>
    /// <param name="feedName">The feed display name.</param>
    /// <param name="message">The user-facing failure message.</param>
    /// <param name="exception">The underlying exception.</param>
    private void LogSyncFailure(string feedName, string message, Exception exception)
    {
        //Legacy comment translated to English.
        _logger.LogWarning("FirmwareUpdate: {Message}", message);
        SenparcTrace.SendCustomLog(
            $"FirmwareUpdate.{feedName}.SyncFailure",
            $"{message}{Environment.NewLine}{exception}");
    }

    /// <summary>
    /// Converts an exception chain into a stable user-facing synchronization failure category.
    /// </summary>
    /// <param name="exception">The synchronization exception.</param>
    /// <returns>A localized failure reason.</returns>
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

    /// <summary>
    /// Enumerates an exception and all inner exceptions in order.
    /// </summary>
    /// <param name="exception">The root exception.</param>
    /// <returns>The exception chain.</returns>
    private static IEnumerable<Exception> EnumerateExceptionChain(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            yield return current;
        }
    }

    /// <summary>
    /// Converts a release tag into a filesystem-safe directory segment.
    /// </summary>
    /// <param name="tag">The release tag to normalize.</param>
    /// <returns>A safe directory segment, or <c>unknown</c> for empty input.</returns>
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
