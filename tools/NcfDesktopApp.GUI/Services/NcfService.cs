/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfService.cs
    文件功能描述：NCF 包下载、运行时安装与更新源选择服务
    
    
    创建标识：Senparc - 20250718
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强更新源选择、下载反馈与桌面窗口兼容性

----------------------------------------------------------------*/
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// CLI 进程输出处理委托
/// </summary>
/// <param name="output">输出内容</param>
/// <param name="isError">是否为错误输出</param>
public delegate void ProcessOutputHandler(string output, bool isError);

public class NcfService
{
    private const string GitHubLatestReleaseUrl = "https://api.github.com/repos/NeuCharFramework/NCF/releases/latest";
    private const int DefaultRequiredDotnetMajorVersion = 10;
    private static readonly TimeSpan SourceProbeTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient _httpClient;
    private readonly ILogger<NcfService>? _logger;
    private ReleaseSourceCandidate? _lastSelectedSource;
    private ReleaseSourceCandidate? _lastAlternateSource;
    
    // 路径配置
    public static string AppDataPath { get; private set; } = string.Empty;
    public static string DownloadsPath { get; private set; } = string.Empty;
    public static string NcfRuntimePath { get; private set; } = string.Empty;

    /// <summary>
    /// 镜像更新源站点根地址（默认 https://www.ncf.pub）。元数据地址为 {此属性}/NcfPackages/latest-release.json。
    /// 可由用户在设置中修改，并通过 desktop-user-settings.json 持久化。
    /// </summary>
    public string MirrorServerBaseUrl { get; set; } = DesktopUserSettings.DefaultMirrorServerBaseUrl;

    /// <summary>
    /// 最近一次更新源测速和选择结果，供界面展示。
    /// </summary>
    public string? LastSourceSelectionSummary { get; private set; }

    /// <summary>
    /// 镜像元数据 latest-release.json 的完整 URL。
    /// </summary>
    public string GetMirrorMetadataUrl()
    {
        var b = DesktopSettingsStore.NormalizeMirrorServerBase(MirrorServerBaseUrl);
        return $"{b}/NcfPackages/latest-release.json";
    }

    /// <summary>
    /// 镜像模块生成的 JSON 常把 <c>browser_download_url</c> 写成固定域名（如 ncf.pub）；使用自定义镜像根时，改为从当前镜像同源路径下载，避免 404。
    /// </summary>
    public string ApplyMirrorBaseToPackageDownloadUrl(string? browserDownloadUrl)
    {
        if (string.IsNullOrWhiteSpace(browserDownloadUrl))
        {
            return browserDownloadUrl ?? string.Empty;
        }

        var customBase = DesktopSettingsStore.NormalizeMirrorServerBase(MirrorServerBaseUrl);
        var defaultBase = DesktopSettingsStore.NormalizeMirrorServerBase(DesktopUserSettings.DefaultMirrorServerBaseUrl);
        if (string.Equals(customBase, defaultBase, StringComparison.OrdinalIgnoreCase))
        {
            return browserDownloadUrl;
        }

        const string marker = "/NcfPackages/";
        var idx = browserDownloadUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return browserDownloadUrl;
        }

        var relativeFromPackages = browserDownloadUrl.Substring(idx);
        var resolved = $"{customBase}{relativeFromPackages}";
        if (!string.Equals(resolved, browserDownloadUrl, StringComparison.Ordinal))
        {
            _logger?.LogInformation("已按自定义镜像根重写安装包下载地址: {Resolved}", resolved);
        }

        return resolved;
    }

    // 🆕 配置文件冲突处理回调
    // 参数: fileName, oldContent, newContent
    // 返回: true=使用新文件（覆盖），false=保留旧文件
    public Func<string, string, string, Task<bool>>? OnAppSettingsConflict { get; set; }
    
    /// <summary>
    /// CLI 进程输出回调（参数：输出内容, 是否为错误输出）
    /// </summary>
    public ProcessOutputHandler? OnProcessOutput { get; set; }

    /// <summary>
    /// 下载日志回调。用于将终端下载日志同步显示到桌面应用的操作日志区域。
    /// </summary>
    public Action<string>? OnDownloadLog { get; set; }
    
    static NcfService()
    {
        InitializePaths();
    }
    
    public NcfService(HttpClient httpClient, ILogger<NcfService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    private static void InitializePaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NcfDesktopApp");
        }
        else
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            AppDataPath = Path.Combine(userHome, ".config", "NcfDesktopApp");
        }
        
        DownloadsPath = Path.Combine(AppDataPath, "Downloads");
        NcfRuntimePath = Path.Combine(AppDataPath, "Runtime");
        
        // 确保目录存在
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(DownloadsPath);
        Directory.CreateDirectory(NcfRuntimePath);
    }
    
    public async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NCF-Desktop-App");

        var githubTask = FetchReleaseCandidateAsync(
            "GitHub",
            TryGetLatestReleaseFromGitHubAsync,
            cancellationToken);
        var mirrorTask = FetchReleaseCandidateAsync(
            GetMirrorSourceDisplayName(),
            TryGetLatestReleaseFromMirrorAsync,
            cancellationToken);

        await Task.WhenAll(githubTask, mirrorTask).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var github = await githubTask.ConfigureAwait(false);
        var mirror = await mirrorTask.ConfigureAwait(false);
        var selected = await SelectPreferredSourceAsync(github, mirror, cancellationToken).ConfigureAwait(false);
        return selected?.Release;
    }

    private async Task<ReleaseSourceCandidate?> FetchReleaseCandidateAsync(
        string sourceName,
        Func<CancellationToken, Task<GitHubRelease?>> fetchRelease,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SourceProbeTimeout);

        var stopwatch = Stopwatch.StartNew();
        var release = await fetchRelease(timeoutCts.Token).ConfigureAwait(false);
        stopwatch.Stop();
        cancellationToken.ThrowIfCancellationRequested();

        var targetAsset = release == null ? null : GetTargetAsset(release);
        if (release == null ||
            string.IsNullOrWhiteSpace(release.TagName) ||
            targetAsset?.Name == null ||
            string.IsNullOrWhiteSpace(targetAsset.BrowserDownloadUrl))
        {
            _logger?.LogWarning("{Source} 未返回当前平台可用的安装包", sourceName);
            return null;
        }

        return new ReleaseSourceCandidate(sourceName, release, stopwatch.Elapsed);
    }

    private async Task<ReleaseSourceCandidate?> SelectPreferredSourceAsync(
        ReleaseSourceCandidate? github,
        ReleaseSourceCandidate? mirror,
        CancellationToken cancellationToken)
    {
        _lastSelectedSource = null;
        _lastAlternateSource = null;

        if (github == null && mirror == null)
        {
            LastSourceSelectionSummary = "更新源测速失败：GitHub 与镜像均不可用";
            return null;
        }

        if (github != null && mirror != null &&
            !string.Equals(github.Release.TagName, mirror.Release.TagName, StringComparison.OrdinalIgnoreCase))
        {
            _lastSelectedSource = github;
            LastSourceSelectionSummary =
                $"镜像版本 {mirror.Release.TagName ?? "未知"} 与 GitHub 最新版本 {github.Release.TagName ?? "未知"} 不一致，优先使用 GitHub";
            _logger?.LogWarning("{Summary}", LastSourceSelectionSummary);
            return github;
        }

        var githubLatencyTask = MeasurePackageEndpointLatencyAsync(github, cancellationToken);
        var mirrorLatencyTask = MeasurePackageEndpointLatencyAsync(mirror, cancellationToken);
        await Task.WhenAll(githubLatencyTask, mirrorLatencyTask).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (github != null)
        {
            github.PackageLatency = await githubLatencyTask.ConfigureAwait(false);
        }

        if (mirror != null)
        {
            mirror.PackageLatency = await mirrorLatencyTask.ConfigureAwait(false);
        }

        // 默认优先使用配置的镜像（全新安装为 www.ncf.pub），GitHub 作为下载失败时的备用源。
        // 上面的版本一致性保护仍然有效：镜像版本落后或异常时会直接选择 GitHub。
        var selected = mirror ?? github!;
        var alternate = mirror != null ? github : null;

        _lastSelectedSource = selected;
        _lastAlternateSource = alternate;
        LastSourceSelectionSummary = BuildSourceSelectionSummary(github, mirror, selected);
        _logger?.LogInformation("{Summary}", LastSourceSelectionSummary);
        return selected;
    }

    private async Task<TimeSpan?> MeasurePackageEndpointLatencyAsync(
        ReleaseSourceCandidate? candidate,
        CancellationToken cancellationToken)
    {
        if (candidate == null)
        {
            return null;
        }

        var asset = GetTargetAsset(candidate.Release);
        var downloadUrl = ApplyMirrorBaseToPackageDownloadUrl(asset?.BrowserDownloadUrl);
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return null;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SourceProbeTimeout);

        try
        {
            var headLatency = await MeasureResponseHeaderLatencyAsync(
                HttpMethod.Head,
                downloadUrl,
                useRangeHeader: false,
                timeoutCts.Token).ConfigureAwait(false);
            if (headLatency.HasValue)
            {
                return headLatency;
            }

            return await MeasureResponseHeaderLatencyAsync(
                HttpMethod.Get,
                downloadUrl,
                useRangeHeader: true,
                timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("{Source} 下载地址测速超时", candidate.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "{Source} 下载地址测速失败", candidate.Name);
            return null;
        }
    }

    private async Task<TimeSpan?> MeasureResponseHeaderLatencyAsync(
        HttpMethod method,
        string downloadUrl,
        bool useRangeHeader,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, downloadUrl);
        if (useRangeHeader)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
        }

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        return response.IsSuccessStatusCode ? stopwatch.Elapsed : null;
    }

    private string GetMirrorSourceDisplayName()
    {
        var mirrorBase = DesktopSettingsStore.NormalizeMirrorServerBase(MirrorServerBaseUrl);
        return Uri.TryCreate(mirrorBase, UriKind.Absolute, out var uri)
            ? uri.Host
            : "镜像";
    }

    private static string BuildSourceSelectionSummary(
        ReleaseSourceCandidate? github,
        ReleaseSourceCandidate? mirror,
        ReleaseSourceCandidate selected)
    {
        static string FormatLatency(ReleaseSourceCandidate? candidate)
        {
            if (candidate == null)
            {
                return "不可用";
            }

            var latency = candidate.PackageLatency ?? candidate.MetadataLatency;
            var suffix = candidate.PackageLatency.HasValue ? string.Empty : "（元数据）";
            return $"{latency.TotalMilliseconds:F0} ms{suffix}";
        }

        return $"下载源：默认优先使用 {mirror?.Name ?? "镜像"} {FormatLatency(mirror)}，GitHub {FormatLatency(github)}；当前使用 {selected.Name}";
    }

    private async Task<GitHubRelease?> TryGetLatestReleaseFromGitHubAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogInformation("从 GitHub 获取最新版本信息...");
            var response = await _httpClient.GetStringAsync(GitHubLatestReleaseUrl, cancellationToken).ConfigureAwait(false);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);
            _logger?.LogInformation("获取到最新版本(GitHub): {Tag}", release?.TagName);
            return release;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "从 GitHub 获取 latest 失败");
            return null;
        }
    }

    private async Task<GitHubRelease?> TryGetLatestReleaseFromMirrorAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = GetMirrorMetadataUrl();
            _logger?.LogInformation("从镜像元数据获取版本: {Url}", url);
            var json = await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, options);
            _logger?.LogInformation("获取到最新版本(镜像元数据): {Tag}", release?.TagName);
            return release;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "从镜像元数据获取最新版本失败");
            return null;
        }
    }
    
    public GitHubAsset? GetTargetAsset(GitHubRelease release)
    {
        if (release.Assets == null) return null;
        
        var platform = GetCurrentPlatform();
        
        foreach (var asset in release.Assets)
        {
            if (asset.Name?.Contains(platform, StringComparison.OrdinalIgnoreCase) == true)
            {
                return asset;
            }
        }
        
        return null;
    }
    
    public Task<bool> CheckIfDownloadNeededAsync(string fileName, long expectedSize)
    {
        var filePath = Path.Combine(DownloadsPath, fileName);
        
        if (!File.Exists(filePath))
        {
            return Task.FromResult(true);
        }
        
        var fileInfo = new FileInfo(filePath);
        return Task.FromResult(fileInfo.Length != expectedSize);
    }
    
    public async Task DownloadFileAsync(string downloadUrl, string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(DownloadsPath, fileName);
        var downloadInfoPath = filePath + ".download"; // 下载信息文件
        
        // 检查是否有未完成的下载（断点续传）
        long existingFileSize = 0;
        bool canResume = false;
        
        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            existingFileSize = fileInfo.Length;
            
            // 检查是否有下载信息文件（包含 URL 和版本信息）
            if (File.Exists(downloadInfoPath))
            {
                try
                {
                    var savedUrl = await File.ReadAllTextAsync(downloadInfoPath, cancellationToken);
                    
                    // 比较 URL 是否一致（URL 包含版本号）
                    if (savedUrl.Trim() == downloadUrl.Trim())
                    {
                        canResume = true;
                        _logger?.LogInformation($"✅ 检测到同一版本的未完成下载，可以断点续传");
                    }
                    else
                    {
                        _logger?.LogWarning($"⚠️ 检测到不同版本的文件，删除旧文件");
                        _logger?.LogInformation($"   旧版本: {savedUrl}");
                        _logger?.LogInformation($"   新版本: {downloadUrl}");
                        
                        // 删除旧文件和下载信息
                        File.Delete(filePath);
                        File.Delete(downloadInfoPath);
                        existingFileSize = 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"⚠️ 无法读取下载信息，重新下载: {ex.Message}");
                    File.Delete(filePath);
                    if (File.Exists(downloadInfoPath))
                    {
                        File.Delete(downloadInfoPath);
                    }
                    existingFileSize = 0;
                }
            }
            else
            {
                // 没有下载信息文件，无法确认版本，删除重新下载
                _logger?.LogWarning($"⚠️ 未找到下载信息文件，无法确认版本，重新下载");
                File.Delete(filePath);
                existingFileSize = 0;
            }
        }
        
        // 保存下载信息（URL 作为版本标识）
        if (existingFileSize == 0)
        {
            await File.WriteAllTextAsync(downloadInfoPath, downloadUrl, cancellationToken);
        }

        WriteDownloadSourceLog(downloadUrl, existingFileSize, canResume);
        
        // 创建 HTTP 请求，支持断点续传
        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        
        if (existingFileSize > 0 && canResume)
        {
            // 使用 Range 请求头从上次中断的位置继续下载
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingFileSize, null);
            _logger?.LogInformation($"📥 从 {existingFileSize:N0} 字节处继续下载: {fileName}");
        }
        else
        {
            _logger?.LogInformation($"📥 开始下载: {fileName}");
        }
        
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        // 检查服务器响应
        if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            // 服务器不支持断点续传或文件已完整下载
            _logger?.LogWarning($"服务器不支持断点续传或文件已完整，重新下载: {fileName}");
            existingFileSize = 0;
            
            // 删除旧文件重新下载
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            // 重新请求完整文件
            request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            using var retryResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            retryResponse.EnsureSuccessStatusCode();
            
            await DownloadToFileAsync(retryResponse, filePath, 0, progress, cancellationToken);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
        {
            // 206 Partial Content - 服务器支持断点续传
            _logger?.LogInformation($"✅ 服务器支持断点续传，继续下载");
            await DownloadToFileAsync(response, filePath, existingFileSize, progress, cancellationToken);
        }
        else if (response.IsSuccessStatusCode)
        {
            // 200 OK - 服务器返回完整文件（可能不支持 Range 或文件从头开始）
            if (existingFileSize > 0)
            {
                _logger?.LogWarning($"服务器不支持断点续传，重新下载: {fileName}");
                File.Delete(filePath);
            }
            await DownloadToFileAsync(response, filePath, 0, progress, cancellationToken);
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }
        
        _logger?.LogInformation($"✅ 下载完成: {fileName}");
        
        // 下载完成后删除下载信息文件
        if (File.Exists(downloadInfoPath))
        {
            try
            {
                File.Delete(downloadInfoPath);
                _logger?.LogInformation($"🧹 已清理下载信息文件");
            }
            catch
            {
                // 忽略删除失败
            }
        }
    }
    
    /// <summary>
    /// 下载数据到文件（支持断点续传）
    /// </summary>
    private async Task DownloadToFileAsync(
        HttpResponseMessage response, 
        string filePath, 
        long existingFileSize, 
        IProgress<double>? progress, 
        CancellationToken cancellationToken)
    {
        var totalBytes = (response.Content.Headers.ContentLength ?? 0) + existingFileSize;
        var downloadedBytes = existingFileSize;
        var sessionStartBytes = existingFileSize;
        var downloadStopwatch = Stopwatch.StartNew();
        var lastConsolePercent = totalBytes > 0
            ? (int)Math.Floor((double)downloadedBytes / totalBytes * 100 / 5) * 5
            : -1;

        if (existingFileSize == 0 && totalBytes > 0)
        {
            WriteDownloadProgressLog(0, downloadedBytes, totalBytes, null, null);
            lastConsolePercent = 0;
        }
        
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        
        // 如果是断点续传，使用 Append 模式；否则使用 Create 模式
        var fileMode = existingFileSize > 0 ? FileMode.Append : FileMode.Create;
        using var fileStream = new FileStream(filePath, fileMode, FileAccess.Write, FileShare.None);
        
        var buffer = new byte[81920]; // 使用 80KB 缓冲区提升性能
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            downloadedBytes += bytesRead;
            
            if (totalBytes > 0)
            {
                var progressPercent = (double)downloadedBytes / totalBytes * 100;
                progress?.Report(progressPercent);

                var reachedConsolePercent = Math.Min(
                    100,
                    (int)Math.Floor(progressPercent / 5) * 5);
                while (lastConsolePercent < reachedConsolePercent)
                {
                    lastConsolePercent += 5;
                    var sessionDownloadedBytes = downloadedBytes - sessionStartBytes;
                    var bytesPerSecond = downloadStopwatch.Elapsed.TotalSeconds > 0
                        ? sessionDownloadedBytes / downloadStopwatch.Elapsed.TotalSeconds
                        : 0;
                    var remainingBytes = Math.Max(0, totalBytes - downloadedBytes);
                    var estimatedRemaining = bytesPerSecond > 0
                        ? TimeSpan.FromSeconds(remainingBytes / bytesPerSecond)
                        : (TimeSpan?)null;

                    WriteDownloadProgressLog(
                        lastConsolePercent,
                        downloadedBytes,
                        totalBytes,
                        bytesPerSecond > 0 ? bytesPerSecond : null,
                        estimatedRemaining);
                }
            }
        }
    }

    private void WriteDownloadSourceLog(string downloadUrl, long existingFileSize, bool canResume)
    {
        var source = Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : downloadUrl;
        WriteDownloadLog($"🌐 实际下载源: {source}");
        WriteDownloadLog($"🔗 下载地址: {downloadUrl}");

        if (canResume && existingFileSize > 0)
        {
            WriteDownloadLog($"📥 断点续传: {FormatByteSize(existingFileSize)}");
        }
    }

    private void WriteDownloadProgressLog(
        int percentage,
        long downloadedBytes,
        long totalBytes,
        double? bytesPerSecond,
        TimeSpan? estimatedRemaining)
    {
        var speedText = bytesPerSecond.HasValue
            ? FormatByteSize((long)bytesPerSecond.Value) + "/s"
            : "计算中";
        var remainingText = estimatedRemaining.HasValue
            ? FormatRemainingTime(estimatedRemaining.Value)
            : "计算中";

        WriteDownloadLog(
            $"📊 下载进度: {percentage,3}% " +
            $"({FormatByteSize(downloadedBytes)} / {FormatByteSize(totalBytes)})，" +
            $"速度: {speedText}，预计剩余: {remainingText}");
    }

    private void WriteDownloadLog(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        OnDownloadLog?.Invoke(message);
    }

    private static string FormatByteSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = Math.Max(0, bytes);
        var unitIndex = 0;
        var displayValue = (double)value;
        while (displayValue >= 1024 && unitIndex < units.Length - 1)
        {
            displayValue /= 1024;
            unitIndex++;
        }

        return $"{displayValue:F1} {units[unitIndex]}";
    }

    private static string FormatRemainingTime(TimeSpan remaining)
    {
        if (remaining < TimeSpan.FromSeconds(1))
        {
            return "不足 1 秒";
        }

        return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }
    
    /// <summary>
    /// 获取当前已安装的 NeuCharFramework 版本
    /// </summary>
    /// <returns>当前版本号，如果未安装则返回 null</returns>
    public async Task<string?> GetInstalledVersionAsync()
    {
        var versionFile = Path.Combine(NcfRuntimePath, "version.txt");
        var senparcWebDll = Path.Combine(NcfRuntimePath, "Senparc.Web.dll");
        
        // 检查是否已安装（至少存在主程序文件）
        if (!File.Exists(senparcWebDll))
        {
            return null;
        }
        
        // 检查版本文件
        if (!File.Exists(versionFile))
        {
            return null;
        }
        
        try
        {
            var version = await File.ReadAllTextAsync(versionFile);
            return version.Trim();
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<bool> CheckIfExtractNeededAsync(string version)
    {
        var versionFile = Path.Combine(NcfRuntimePath, "version.txt");
        var senparcWebDll = Path.Combine(NcfRuntimePath, "Senparc.Web.dll");
        
        if (!File.Exists(senparcWebDll))
        {
            return true;
        }
        
        if (!File.Exists(versionFile))
        {
            return true;
        }
        
        var currentVersion = await File.ReadAllTextAsync(versionFile);
        return currentVersion.Trim() != version.Trim();
    }
    
    public async Task ExtractZipAsync(string zipFileName, string version, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var zipPath = Path.Combine(DownloadsPath, zipFileName);
        
        _logger?.LogInformation("开始提取文件...");
        
        // 🎯 新增：保护重要文件和文件夹
        await PreserveImportantFilesAsync();
        
        // 清理旧文件（但保留重要文件）
        await SafeCleanRuntimeDirectoryAsync();
        
        await ExtractZipWithCorrectPathsAsync(zipPath, NcfRuntimePath, progress, cancellationToken);
        
        // 🎯 新增：恢复保护的文件
        await RestoreImportantFilesAsync();
        
        // 🎯 新增：macOS 解压后自动处理
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await PostProcessMacOSExecutablesAsync();
        }
        
        // 保存版本信息
        await SaveVersionAsync(version);
        
        _logger?.LogInformation("文件提取完成");
    }
    
    public async Task<int> FindAvailablePortAsync(int startPort = 5001, int endPort = 5300)
    {
        for (int port = startPort; port <= endPort; port++)
        {
            if (await IsPortInUseAsync(port))
            {
                continue;
            }
            
            try
            {
                using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch
            {
                continue;
            }
        }
        
        throw new InvalidOperationException($"无法找到可用端口（范围: {startPort} - {endPort}）");
    }
    
    public Task<Process> StartNcfProcessAsync(int port, CancellationToken cancellationToken = default)
    {
        return StartNcfProcessAsync(port, null, cancellationToken);
    }

    public async Task<Process> StartNcfProcessAsync(
        int port,
        string? desktopBridgeToken,
        CancellationToken cancellationToken = default)
    {
        var resolution = NcfLaunchTargetResolver.ResolveManagedRuntime(NcfRuntimePath);
        if (!resolution.IsValid)
        {
            throw new FileNotFoundException(resolution.ErrorMessage, NcfRuntimePath);
        }

        return await StartNcfProcessAsync(
            resolution.Target!,
            port,
            desktopBridgeToken,
            "Production",
            cancellationToken);
    }

    /// <summary>
    /// 启动显式解析后的 NCF 目标。外部目标不会被更新器清理、解压或修改文件属性。
    /// </summary>
    public async Task<Process> StartNcfProcessAsync(
        NcfLaunchTarget target,
        int port,
        string? desktopBridgeToken,
        string aspNetCoreEnvironment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        var environment = string.Equals(aspNetCoreEnvironment, "Development", StringComparison.OrdinalIgnoreCase)
            ? "Development"
            : "Production";

        return target.IsSourceProject
            ? await StartSourceProjectAsync(target, port, desktopBridgeToken, environment, cancellationToken)
            : await StartPublishedTargetAsync(target, port, desktopBridgeToken, environment, cancellationToken);
    }

    private async Task<Process> StartPublishedTargetAsync(
        NcfLaunchTarget target,
        int port,
        string? desktopBridgeToken,
        string environment,
        CancellationToken cancellationToken)
    {
        var ncfAppDir = target.WorkingDirectory;

        // 路径定义（基于实际 NCF 目录）
        if (target.IsManaged && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // 只对桌面端自己管理和下载的 Runtime 处理隔离属性。
            TryRemoveQuarantine(ncfAppDir);
        }
        var dllPath = Path.Combine(ncfAppDir, "Senparc.Web.dll");
        var exePathWin = Path.Combine(ncfAppDir, "Senparc.Web.exe");
        var exePathUnix = Path.Combine(ncfAppDir, "Senparc.Web"); // 自包含可执行（无扩展名）

        _logger?.LogInformation($"启动NCF站点，端口: {port}");

        // 优先使用自包含可执行文件
        ProcessStartInfo startInfo;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(exePathWin))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = exePathWin,
                Arguments = $"--urls=http://localhost:{port}",
                WorkingDirectory = ncfAppDir,
                UseShellExecute = false,
                CreateNoWindow = false
            };
        }
        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(exePathUnix))
        {
            // 外部目录保持原样；仅允许修改桌面端自己管理的 Runtime 文件权限。
            if (target.IsManaged)
            {
                TryMakeExecutable(exePathUnix);
            }
            startInfo = new ProcessStartInfo
            {
                FileName = exePathUnix,
                Arguments = $"--urls=http://localhost:{port}",
                WorkingDirectory = ncfAppDir,
                UseShellExecute = false,
                CreateNoWindow = false
            };
        }
        else
        {
            // 回退到框架依赖方式：dotnet Senparc.Web.dll
            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException($"未找到 NCF 启动文件（既没有自包含可执行，也没有 dll）: {ncfAppDir}");
            }

            var dotnetPath = await ResolveCompatibleDotnetPathAsync(
                ncfAppDir,
                allowAutomaticInstall: target.IsManaged,
                cancellationToken);

            startInfo = new ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = $"Senparc.Web.dll --urls=http://localhost:{port}",
                WorkingDirectory = ncfAppDir,
                UseShellExecute = false,
                CreateNoWindow = false
            };
        }

        // 通用环境变量
        startInfo.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        ApplyDesktopBridgeEnvironment(startInfo, desktopBridgeToken);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            startInfo.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
        }

        // 如果使用本地 dotnet ，补充 DOTNET_ROOT 和 PATH 以保证宿主可定位到运行时
        ApplyDotnetEnvironment(startInfo, startInfo.FileName);

        // 捕获进程输出，便于诊断
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
        
        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception ex) when (File.Exists(dllPath) && !IsDotnetHost(startInfo.FileName))
        {
            _logger?.LogWarning(ex, "自包含入口启动失败，回退到 dotnet DLL 方式");
            process = null;
        }
        
        // 附加输出捕获事件处理
        AttachProcessOutputHandlers(process);
        // 若自包含可执行在 macOS 被 Gatekeeper 杀死或依赖缺失导致瞬退，尝试回退到 dotnet 方式
        if ((process == null || process.HasExited) && File.Exists(Path.Combine(ncfAppDir, "Senparc.Web.dll")))
        {
            _logger?.LogWarning("检测到自包含启动失败，回退到 dotnet 方式...");
            var dotnetPath = await ResolveCompatibleDotnetPathAsync(
                ncfAppDir,
                allowAutomaticInstall: target.IsManaged,
                cancellationToken);

            var fb = new ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = $"Senparc.Web.dll --urls=http://localhost:{port}",
                WorkingDirectory = ncfAppDir,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            fb.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
            fb.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
            ApplyDesktopBridgeEnvironment(fb, desktopBridgeToken);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                fb.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
            }
            ApplyDotnetEnvironment(fb, dotnetPath);
            process = Process.Start(fb);
            AttachProcessOutputHandlers(process);
        }

        // 若自包含进程在极短时间内崩溃（被 Gatekeeper 杀死），再做一次回退检查
        if (process != null && !process.HasExited)
        {
            try
            {
                await Task.Delay(1500, cancellationToken);
                if (process.HasExited && File.Exists(Path.Combine(ncfAppDir, "Senparc.Web.dll")))
                {
                    _logger?.LogWarning("自包含进程瞬退，回退到 dotnet DLL 启动...");
                    var dotnetPath2 = await ResolveCompatibleDotnetPathAsync(
                        ncfAppDir,
                        allowAutomaticInstall: target.IsManaged,
                        cancellationToken);
                    var fb2 = new ProcessStartInfo
                    {
                        FileName = dotnetPath2,
                        Arguments = $"Senparc.Web.dll --urls=http://localhost:{port}",
                        WorkingDirectory = ncfAppDir,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    };
                    fb2.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
                    fb2.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
                    ApplyDesktopBridgeEnvironment(fb2, desktopBridgeToken);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        fb2.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
                    }
                    ApplyDotnetEnvironment(fb2, dotnetPath2);
                    process = Process.Start(fb2);
                    AttachProcessOutputHandlers(process);
                }
            }
            catch { }
        }

        if (process == null)
        {
            throw new InvalidOperationException("无法启动NCF进程");
        }
        return process;
    }

    private async Task<Process> StartSourceProjectAsync(
        NcfLaunchTarget target,
        int port,
        string? desktopBridgeToken,
        string environment,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(target.EntryPath))
        {
            throw new FileNotFoundException("NCF 源码项目不存在。", target.EntryPath);
        }

        var sdkResolution = DotnetSdkResolver.Resolve(target.TargetFramework, target.WorkingDirectory);
        if (!sdkResolution.IsValid)
        {
            throw new InvalidOperationException(sdkResolution.ErrorMessage);
        }

        var startInfo = CreateSourceProjectStartInfo(
            target,
            sdkResolution.DotnetPath!,
            port,
            desktopBridgeToken,
            environment);
        ApplyDotnetEnvironment(startInfo, sdkResolution.DotnetPath!);

        _logger?.LogInformation(
            "从源码启动 NCF: {Project}，SDK: {SdkVersion}，dotnet: {DotnetPath}",
            target.EntryPath,
            sdkResolution.SelectedSdkVersion,
            sdkResolution.DotnetPath);
        var process = Process.Start(startInfo)
                      ?? throw new InvalidOperationException("无法启动 dotnet run 进程");
        AttachProcessOutputHandlers(process);

        // 给 dotnet CLI 一个短暂的同步失败窗口，便于尽早反馈缺少构建产物或 SDK 错误。
        try
        {
            await Task.Delay(500, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            throw;
        }

        if (process.HasExited)
        {
            throw new InvalidOperationException(
                $"dotnet run 启动后立即退出（ExitCode: {process.ExitCode}）。请查看上方 CLI 输出；源码模式未执行 restore。");
        }

        return process;
    }

    internal static ProcessStartInfo CreateSourceProjectStartInfo(
        NcfLaunchTarget target,
        string dotnetPath,
        int port,
        string? desktopBridgeToken,
        string environment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = dotnetPath,
            WorkingDirectory = target.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(target.EntryPath);
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        ApplyDesktopBridgeEnvironment(startInfo, desktopBridgeToken);
        return startInfo;
    }

    private static void ApplyDesktopBridgeEnvironment(ProcessStartInfo startInfo, string? desktopBridgeToken)
    {
        if (!string.IsNullOrWhiteSpace(desktopBridgeToken))
        {
            startInfo.Environment["NCF_DESKTOP_BRIDGE_TOKEN"] = desktopBridgeToken;
        }
    }

    private static bool IsDotnetHost(string path)
    {
        var fileName = Path.GetFileName(path);
        return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase);
    }

    internal static void ApplyDotnetEnvironment(ProcessStartInfo startInfo, string dotnetPath)
    {
        if (!IsDotnetHost(dotnetPath))
        {
            return;
        }

        var dotnetRoot = DotnetSdkResolver.GetDotnetRoot(dotnetPath);
        if (string.IsNullOrWhiteSpace(dotnetRoot))
        {
            return;
        }

        startInfo.Environment["DOTNET_ROOT"] = dotnetRoot;
        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        startInfo.Environment["PATH"] = string.IsNullOrEmpty(existingPath)
            ? dotnetRoot
            : dotnetRoot + Path.PathSeparator + existingPath;
    }

    private async Task<string> ResolveCompatibleDotnetPathAsync(
        string ncfAppDir,
        bool allowAutomaticInstall,
        CancellationToken cancellationToken)
    {
        var requiredMajorVersion = GetRequiredDotnetMajorVersion(ncfAppDir);
        var localDotnet = GetLocalDotnetPath();

        if (HasCompatibleDotnetRuntime(localDotnet, requiredMajorVersion))
        {
            _logger?.LogInformation($"使用本地 .NET {requiredMajorVersion} 运行时: {localDotnet}");
            return localDotnet;
        }

        foreach (var candidate in DotnetSdkResolver.GetCandidatePaths())
        {
            if (HasCompatibleDotnetRuntime(candidate, requiredMajorVersion))
            {
                _logger?.LogInformation(
                    "使用系统 .NET {RequiredMajorVersion} 运行时: {DotnetPath}",
                    requiredMajorVersion,
                    candidate);
                return candidate;
            }
        }

        if (!allowAutomaticInstall)
        {
            throw new InvalidOperationException(
                $"未找到兼容的 .NET {requiredMajorVersion} ASP.NET Core 运行时。外部目标模式不会下载运行时或修改目标目录，请先安装对应运行时。");
        }

        _logger?.LogWarning($"未找到兼容的 .NET {requiredMajorVersion} ASP.NET Core 运行时，将安装到用户目录");
        return await EnsureDotnetAvailableAsync(requiredMajorVersion, cancellationToken);
    }

    private static int GetRequiredDotnetMajorVersion(string ncfAppDir)
    {
        var runtimeConfigPath = Path.Combine(ncfAppDir, "Senparc.Web.runtimeconfig.json");
        if (!File.Exists(runtimeConfigPath))
        {
            return DefaultRequiredDotnetMajorVersion;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(runtimeConfigPath));
            if (!document.RootElement.TryGetProperty("runtimeOptions", out var runtimeOptions))
            {
                return DefaultRequiredDotnetMajorVersion;
            }

            if (runtimeOptions.TryGetProperty("tfm", out var tfmElement))
            {
                var tfm = tfmElement.GetString();
                return DotnetSdkResolver.GetTargetFrameworkMajorVersion(tfm ?? string.Empty);
            }
        }
        catch
        {
            // runtimeconfig 损坏时使用当前 Web 项目的默认目标版本，后续启动日志会给出更明确错误。
        }

        return DefaultRequiredDotnetMajorVersion;
    }

    private static bool HasCompatibleDotnetRuntime(string dotnetPath, int requiredMajorVersion)
    {
        if (Path.IsPathRooted(dotnetPath) && !File.Exists(dotnetPath))
        {
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = "--list-runtimes",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            if (!p.HasExited || p.ExitCode != 0)
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return false;
            }

            var requiredPrefix = requiredMajorVersion + ".";
            var hasNetCoreRuntime = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(line => line.StartsWith("Microsoft.NETCore.App " + requiredPrefix, StringComparison.Ordinal));
            var hasAspNetCoreRuntime = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(line => line.StartsWith("Microsoft.AspNetCore.App " + requiredPrefix, StringComparison.Ordinal));
            return hasNetCoreRuntime && hasAspNetCoreRuntime;
        }
        catch
        {
            return false;
        }
    }

    private static void TryMakeExecutable(string path)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            if (!File.Exists(path)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            })?.WaitForExit(2000);
        }
        catch
        {
            // 忽略授予执行权限失败
        }
    }

    private static void TryRemoveQuarantine(string directory)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
            if (!Directory.Exists(directory)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/xattr",
                Arguments = $"-dr com.apple.quarantine \"{directory}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            })?.WaitForExit(2000);
        }
        catch { }
    }

    private static string GetLocalDotnetInstallDir()
    {
        // 将用户级 dotnet 安装在运行时目录下，避免需要管理员权限
        return Path.Combine(NcfRuntimePath, ".dotnet");
    }

    private static string GetLocalDotnetPath()
    {
        var dir = GetLocalDotnetInstallDir();
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(dir, "dotnet.exe")
            : Path.Combine(dir, "dotnet");
    }

    private async Task<string> EnsureDotnetAvailableAsync(int requiredMajorVersion, CancellationToken cancellationToken)
    {
        var localDotnet = GetLocalDotnetPath();
        if (HasCompatibleDotnetRuntime(localDotnet, requiredMajorVersion))
        {
            return localDotnet;
        }

        await InstallLocalDotnetRuntimeAsync(requiredMajorVersion, cancellationToken);
        // 为 Unix 平台确保可执行权限
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TryMakeExecutable(localDotnet);
        }

        if (!HasCompatibleDotnetRuntime(localDotnet, requiredMajorVersion))
        {
            throw new InvalidOperationException($"自动安装 .NET Runtime 失败，请手动安装 .NET {requiredMajorVersion} ASP.NET Core 运行时或使用自包含的 NCF 包。");
        }
        return localDotnet;
    }

    private async Task InstallLocalDotnetRuntimeAsync(int requiredMajorVersion, CancellationToken cancellationToken)
    {
        try
        {
            var installDir = GetLocalDotnetInstallDir();
            Directory.CreateDirectory(installDir);

            var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            var channel = $"{requiredMajorVersion}.0";
            _logger?.LogInformation($"准备安装 .NET {requiredMajorVersion} 运行时到: {installDir} (架构: {arch})");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // 使用官方 dotnet-install.ps1 安装到用户目录，无需管理员权限
                var scriptUrl = "https://dot.net/v1/dotnet-install.ps1";
                var scriptPath = Path.Combine(installDir, "dotnet-install.ps1");
                var scriptBytes = await _httpClient.GetByteArrayAsync(scriptUrl, cancellationToken);
                await File.WriteAllBytesAsync(scriptPath, scriptBytes, cancellationToken);
                _logger?.LogInformation("下载 dotnet-install.ps1 完成，开始安装 .NET Runtime...");

                // 先安装 .NET Runtime（包含 dotnet 主机）
                var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Runtime dotnet -Channel {channel} -Architecture {arch} -InstallDir \"{installDir}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = ResolvePowerShellExecutable(),
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    await p.WaitForExitAsync(cancellationToken);
                    var o = await p.StandardOutput.ReadToEndAsync();
                    var e = await p.StandardError.ReadToEndAsync();
                    _logger?.LogInformation("dotnet runtime 安装输出:\n" + o);
                    if (!string.IsNullOrWhiteSpace(e)) _logger?.LogWarning("dotnet runtime 安装警告/错误:\n" + e);
                }

                // 再安装 ASP.NET Core Runtime（提供 Microsoft.AspNetCore.App 框架）
                var argsAsp = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Runtime aspnetcore -Channel {channel} -Architecture {arch} -InstallDir \"{installDir}\"";
                var psiAsp = new ProcessStartInfo
                {
                    FileName = ResolvePowerShellExecutable(),
                    Arguments = argsAsp,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var pAsp = Process.Start(psiAsp);
                if (pAsp != null)
                {
                    await pAsp.WaitForExitAsync(cancellationToken);
                    var o2 = await pAsp.StandardOutput.ReadToEndAsync();
                    var e2 = await pAsp.StandardError.ReadToEndAsync();
                    _logger?.LogInformation("aspnetcore runtime 安装输出:\n" + o2);
                    if (!string.IsNullOrWhiteSpace(e2)) _logger?.LogWarning("aspnetcore runtime 安装警告/错误:\n" + e2);
                }
            }
            else
            {
                // macOS/Linux 使用 dotnet-install.sh
                var scriptUrl = "https://dot.net/v1/dotnet-install.sh";
                var scriptPath = Path.Combine(installDir, "dotnet-install.sh");
                var scriptBytes = await _httpClient.GetByteArrayAsync(scriptUrl, cancellationToken);
                await File.WriteAllBytesAsync(scriptPath, scriptBytes, cancellationToken);
                TryMakeExecutable(scriptPath);
                _logger?.LogInformation("下载 dotnet-install.sh 完成，开始安装 .NET Runtime...");

                // 先安装 .NET Runtime（包含 dotnet 主机）
                var args = $"\"{scriptPath}\" --runtime dotnet --channel {channel} --architecture {arch} --install-dir \"{installDir}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    await p.WaitForExitAsync(cancellationToken);
                }

                // 再安装 ASP.NET Core Runtime（提供 Microsoft.AspNetCore.App 框架）
                var argsAsp = $"\"{scriptPath}\" --runtime aspnetcore --channel {channel} --architecture {arch} --install-dir \"{installDir}\"";
                var psiAsp = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = argsAsp,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var pAsp = Process.Start(psiAsp);
                if (pAsp != null)
                {
                    await pAsp.WaitForExitAsync(cancellationToken);
                }
            }

            // 校验安装是否成功
            var localDotnet = GetLocalDotnetPath();
            var checkInfo = new ProcessStartInfo
            {
                FileName = localDotnet,
                Arguments = "--list-runtimes",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var chk = Process.Start(checkInfo);
            if (chk != null)
            {
                await chk.WaitForExitAsync(cancellationToken);
                var outText = await chk.StandardOutput.ReadToEndAsync();
                _logger?.LogInformation("已安装的运行时:\n" + outText);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "自动安装 .NET Runtime 失败");
            throw;
        }
    }

    private static string ResolvePowerShellExecutable()
    {
        // 优先使用 powershell.exe，回退到 powershell 或 pwsh
        var candidates = new[] { "powershell.exe", "powershell", "pwsh" };
        foreach (var c in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = c,
                    Arguments = "-NoProfile -Command \"$PSVersionTable.PSVersion\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) continue;
                p.WaitForExit(2000);
                if (p.ExitCode == 0) return c;
            }
            catch
            {
                // ignore
            }
        }
        return "powershell.exe";
    }
    
    public Task<bool> WaitForSiteReadyAsync(
        string siteUrl,
        Process? process,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        return WaitForSiteReadyAsync(
            siteUrl,
            process,
            timeoutSeconds,
            requireNcfBranding: true,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> WaitForSiteReadyAsync(
        string siteUrl,
        Process? process,
        int timeoutSeconds,
        bool requireNcfBranding,
        CancellationToken cancellationToken = default)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var startTime = DateTime.UtcNow;
        var uri = new Uri(siteUrl);
        var port = uri.Port;
        var consecutiveOk = 0;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;
                
            if (process?.HasExited == true)
                return false;
            
            // 先判断端口是否已被占用（监听中）
            try
            {
                if (!await IsPortInUseAsync(port))
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }
            }
            catch { }

            try
            {
                using var response = await _httpClient.GetAsync(siteUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    // 托管版本保持品牌校验；外部旧版本允许首页没有 NCF/Senparc 标识。
                    var looksLikeNcf = content.IndexOf("Senparc", StringComparison.OrdinalIgnoreCase) >= 0
                                        || content.IndexOf("NCF", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!requireNcfBranding || looksLikeNcf)
                    {
                        consecutiveOk++;
                    }
                    else
                    {
                        consecutiveOk = 0;
                    }

                    if (consecutiveOk >= 2)
                    {
                        return true; // 连续两次 2xx 认为就绪，避免偶发 200 假阳性
                    }
                }
                else
                {
                    _logger?.LogWarning($"NCF 就绪检查返回状态码: {(int)response.StatusCode}");
                    consecutiveOk = 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug($"就绪检查失败: {ex.Message}");
                consecutiveOk = 0;
            }
            
            await Task.Delay(2000, cancellationToken);
        }
        
        return false;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            return await GetLatestReleaseAsync().ConfigureAwait(false) != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetLatestVersionAsync()
    {
        try
        {
            var release = await GetLatestReleaseAsync();
            return release?.TagName ?? "获取失败";
        }
        catch
        {
            return "获取失败";
        }
    }

    public async Task DownloadLatestReleaseAsync(IProgress<(string message, double percentage)> progress, bool showDetailedInfo, CancellationToken cancellationToken = default)
    {
        var release = await GetLatestReleaseAsync(cancellationToken);
        if (release == null)
        {
            throw new InvalidOperationException("无法获取最新版本信息");
        }

        var targetAsset = GetTargetAsset(release);
        if (targetAsset == null)
        {
            throw new InvalidOperationException("未找到适合当前平台的下载包");
        }

        if (!string.IsNullOrWhiteSpace(LastSourceSelectionSummary))
        {
            progress.Report((LastSourceSelectionSummary, -1));
        }

        var needsDownload = await CheckIfDownloadNeededAsync(targetAsset.Name!, targetAsset.Size);
        
        if (needsDownload)
        {
            progress.Report(($"正在下载 {targetAsset.Name}...", -1));
            
            var downloadProgress = new Progress<double>(value =>
            {
                progress.Report(($"下载中... {value:F1}%", value * 0.6));
            });

            var downloadUrl = ApplyMirrorBaseToPackageDownloadUrl(targetAsset.BrowserDownloadUrl);
            try
            {
                await DownloadFileAsync(downloadUrl, targetAsset.Name!, downloadProgress, cancellationToken);
            }
            catch (Exception ex) when (
                !cancellationToken.IsCancellationRequested &&
                (ex is HttpRequestException || ex is TaskCanceledException))
            {
                var alternateAsset = _lastAlternateSource == null
                    ? null
                    : GetTargetAsset(_lastAlternateSource.Release);
                var alternateUrl = ApplyMirrorBaseToPackageDownloadUrl(alternateAsset?.BrowserDownloadUrl);
                if (alternateAsset?.Name == null ||
                    string.IsNullOrWhiteSpace(alternateUrl) ||
                    string.Equals(downloadUrl, alternateUrl, StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }

                var failedSourceName = _lastSelectedSource?.Name ?? "首选源";
                var alternateSourceName = _lastAlternateSource!.Name;
                _logger?.LogWarning(ex, "{Source} 下载失败，切换到 {AlternateSource}", failedSourceName, alternateSourceName);
                progress.Report(($"⚠️ {failedSourceName} 下载失败，正在切换到 {alternateSourceName}...", -1));
                await DownloadFileAsync(alternateUrl, alternateAsset.Name, downloadProgress, cancellationToken);
                _lastSelectedSource = _lastAlternateSource;
                _lastAlternateSource = null;
                LastSourceSelectionSummary = $"{failedSourceName} 下载失败，已自动切换到 {_lastSelectedSource.Name}";
            }
            progress.Report(("✅ 下载完成", 60));
        }
        else
        {
            progress.Report(("✅ 文件已存在，跳过下载", 60));
        }
    }

    public async Task ExtractFilesAsync(IProgress<(string message, double percentage)> progress, CancellationToken cancellationToken = default)
    {
        var release = await GetLatestReleaseAsync(cancellationToken);
        if (release == null) return;

        var targetAsset = GetTargetAsset(release);
        if (targetAsset == null) return;

        var needsExtract = await CheckIfExtractNeededAsync(release.TagName!);
        
        if (needsExtract)
        {
            progress.Report(("正在提取文件...", -1));

            var extractProgress = new Progress<double>(value =>
            {
                progress.Report(($"提取中... {value:F1}%", 60 + (value * 0.3)));
            });

            await ExtractZipAsync(targetAsset.Name!, release.TagName!, extractProgress, cancellationToken);
            progress.Report(("✅ 文件提取完成", 90));
        }
        else
        {
            progress.Report(("✅ 文件已是最新版本，跳过提取", 90));
        }
    }

    public Task CleanupDownloadsAsync()
    {
        try
        {
            if (Directory.Exists(DownloadsPath))
            {
                var files = Directory.GetFiles(DownloadsPath, "*.zip");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // 忽略清理错误
        }
        return Task.CompletedTask;
    }
    
    #region 私有方法
    
    /// <summary>
    /// 为进程附加输出捕获事件处理
    /// </summary>
    private void AttachProcessOutputHandlers(Process? process)
    {
        if (process == null) return;
        
        // 捕获标准输出
        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                try
                {
                    OnProcessOutput?.Invoke(args.Data, false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"处理进程输出时出错: {ex.Message}");
                }
            }
        };
        
        // 捕获错误输出
        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                try
                {
                    OnProcessOutput?.Invoke(args.Data, true);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"处理进程错误输出时出错: {ex.Message}");
                }
            }
        };
        
        // 开始异步读取
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        // 注册进程退出事件
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) =>
        {
            try
            {
                OnProcessOutput?.Invoke("--- 进程已退出 ---", false);
            }
            catch { }
        };
    }
    
    private static string GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }
        
        throw new PlatformNotSupportedException("不支持的平台");
    }
    
    private async Task<bool> IsPortInUseAsync(int port)
    {
        try
        {
            ProcessStartInfo startInfo;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"netstat -an | findstr :{port}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            }
            else
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "lsof",
                    Arguments = $"-i :{port}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            }
            
            using var process = Process.Start(startInfo);
            if (process == null) return false;
            
            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return !string.IsNullOrWhiteSpace(output);
            }
            else
            {
                return process.ExitCode == 0;
            }
        }
        catch
        {
            return false;
        }
    }
    
    private async Task ExtractZipWithCorrectPathsAsync(string zipPath, string extractPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var totalEntries = archive.Entries.Count;
        var processedEntries = 0;
        
        foreach (var entry in archive.Entries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
            if (string.IsNullOrEmpty(entry.Name))
            {
                processedEntries++;
                continue;
            }
            
            var relativePath = entry.FullName.Replace('\\', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(extractPath, relativePath);
            
            var directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            using var entryStream = entry.Open();
            using var fileStream = File.Create(fullPath);
            await entryStream.CopyToAsync(fileStream, cancellationToken);
            
            processedEntries++;
            progress?.Report((double)processedEntries / totalEntries * 100);
        }
    }
    
    /// <summary>
    /// macOS 解压后处理：自动设置权限、移除隔离属性、执行代码签名
    /// </summary>
    private async Task PostProcessMacOSExecutablesAsync()
    {
        try
        {
            _logger?.LogInformation("🍎 正在处理 macOS 可执行文件...");
            
            // 查找所有可能的可执行文件
            var potentialExecutables = new[]
            {
                "Senparc.Web",
                "NcfDesktopApp.GUI",
                // 可以添加其他可执行文件
            };
            
            var processedCount = 0;
            foreach (var execName in potentialExecutables)
            {
                var execPath = Path.Combine(NcfRuntimePath, execName);
                if (File.Exists(execPath))
                {
                    await ProcessMacOSExecutableAsync(execPath);
                    processedCount++;
                }
                
                // 也检查子目录
                processedCount += await ProcessExecutablesInDirectoryAsync(NcfRuntimePath, execName);
            }
            
            _logger?.LogInformation($"✅ macOS 可执行文件处理完成，共处理 {processedCount} 个文件");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ macOS 可执行文件处理失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理单个macOS可执行文件：权限、隔离属性、代码签名
    /// </summary>
    private async Task ProcessMacOSExecutableAsync(string executablePath)
    {
        try
        {
            _logger?.LogInformation($"🔧 处理可执行文件: {Path.GetFileName(executablePath)}");
            
            // 1. 设置执行权限
            await RunMacOSCommandAsync("/bin/chmod", $"+x \"{executablePath}\"", "设置执行权限");
            
            // 2. 移除隔离属性
            await RunMacOSCommandAsync("/usr/bin/xattr", $"-d com.apple.quarantine \"{executablePath}\"", "移除隔离属性");
            
            // 3. Ad-hoc 代码签名
            var signSuccess = await RunMacOSCommandAsync("/usr/bin/codesign", $"--force --sign - \"{executablePath}\"", "Ad-hoc代码签名");
            
            // 4. 验证签名（可选）
            if (signSuccess)
            {
                var verifySuccess = await RunMacOSCommandAsync("/usr/bin/codesign", $"--verify \"{executablePath}\"", "验证签名", false);
                _logger?.LogInformation($"📋 签名验证: {(verifySuccess ? "✅ 成功" : "⚠️ 失败")} - {Path.GetFileName(executablePath)}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ 处理可执行文件失败 {Path.GetFileName(executablePath)}: {ex.Message}");
        }
    }

    /// <summary>
    /// 在目录中递归查找并处理可执行文件
    /// </summary>
    private async Task<int> ProcessExecutablesInDirectoryAsync(string directory, string executableName)
    {
        var processedCount = 0;
        try
        {
            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var execPath = Path.Combine(subDir, executableName);
                if (File.Exists(execPath))
                {
                    await ProcessMacOSExecutableAsync(execPath);
                    processedCount++;
                }
                
                // 递归处理子目录（限制深度避免无限循环）
                if (subDir.Split(Path.DirectorySeparatorChar).Length < directory.Split(Path.DirectorySeparatorChar).Length + 3)
                {
                    processedCount += await ProcessExecutablesInDirectoryAsync(subDir, executableName);
                }
            }
        }
        catch
        {
            // 忽略目录访问错误
        }
        return processedCount;
    }

    /// <summary>
    /// 运行macOS命令行工具
    /// </summary>
    private async Task<bool> RunMacOSCommandAsync(string fileName, string arguments, string description, bool logErrors = true)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process == null) return false;

            var timeoutTask = Task.Delay(5000); // 5秒超时
            var processTask = Task.Run(() => process.WaitForExit());
            
            var completedTask = await Task.WhenAny(processTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                process.Kill();
                if (logErrors) _logger?.LogWarning($"⏱️ {description} 超时");
                return false;
            }

            var success = process.ExitCode == 0;
            if (!success && logErrors)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger?.LogWarning($"❌ {description} 失败: {error}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            if (logErrors) _logger?.LogWarning($"💥 {description} 执行异常: {ex.Message}");
            return false;
        }
    }

    private async Task SaveVersionAsync(string version)
    {
        var versionFile = Path.Combine(NcfRuntimePath, "version.txt");
        await File.WriteAllTextAsync(versionFile, version);
    }

    /// <summary>
    /// 保护重要文件和文件夹到临时位置
    /// </summary>
    private async Task PreserveImportantFilesAsync()
    {
        try
        {
            _logger?.LogInformation("🛡️ 开始保护重要文件...");
            
            var backupPath = GetBackupPath();
            
            // 确保备份目录存在
            Directory.CreateDirectory(backupPath);
            
            // 保护 App_Data 文件夹
            var appDataPath = Path.Combine(NcfRuntimePath, "App_Data");
            if (Directory.Exists(appDataPath))
            {
                var backupAppDataPath = Path.Combine(backupPath, "App_Data");
                await CopyDirectoryAsync(appDataPath, backupAppDataPath);
                _logger?.LogInformation("✅ App_Data 文件夹已备份");
            }
            
            // 🆕 保护 logs 文件夹
            var logsPath = Path.Combine(NcfRuntimePath, "logs");
            if (Directory.Exists(logsPath))
            {
                var backupLogsPath = Path.Combine(backupPath, "logs");
                await CopyDirectoryAsync(logsPath, backupLogsPath);
                _logger?.LogInformation("✅ logs 文件夹已备份");
            }
            
            // 备用：如果存在 log 文件夹（向后兼容）
            var logPath = Path.Combine(NcfRuntimePath, "log");
            if (Directory.Exists(logPath))
            {
                var backupLogPath = Path.Combine(backupPath, "log");
                await CopyDirectoryAsync(logPath, backupLogPath);
                _logger?.LogInformation("✅ log 文件夹已备份");
            }
            
            // 备份 appsettings.json 文件
            await BackupAppSettingsFilesAsync(backupPath);
            
            _logger?.LogInformation("✅ 重要文件保护完成");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ 保护重要文件时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 备份所有 appsettings*.json 文件
    /// </summary>
    private Task BackupAppSettingsFilesAsync(string backupPath)
    {
        try
        {
            var settingsBackupPath = Path.Combine(backupPath, "appsettings");
            Directory.CreateDirectory(settingsBackupPath);
            
            // 查找所有 appsettings*.json 文件
            var settingsFiles = Directory.GetFiles(NcfRuntimePath, "appsettings*.json", SearchOption.AllDirectories);
            
            foreach (var settingsFile in settingsFiles)
            {
                var fileName = Path.GetFileName(settingsFile);
                var relativePath = Path.GetRelativePath(NcfRuntimePath, settingsFile);
                var backupFilePath = Path.Combine(settingsBackupPath, relativePath.Replace(Path.DirectorySeparatorChar, '_'));
                
                // 创建备份文件的目录
                var backupFileDir = Path.GetDirectoryName(backupFilePath);
                if (!string.IsNullOrEmpty(backupFileDir))
                {
                    Directory.CreateDirectory(backupFileDir);
                }
                
                File.Copy(settingsFile, backupFilePath, true);
                
                // 添加时间戳到备份文件名
                var timestampedBackup = Path.Combine(GetBackupPath(), $"{fileName}.{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                File.Copy(settingsFile, timestampedBackup, true);
                
                _logger?.LogInformation($"✅ 已备份配置文件: {fileName}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ 备份配置文件时出错: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 安全清理 Runtime 目录（保留重要文件）
    /// </summary>
    private Task SafeCleanRuntimeDirectoryAsync()
    {
        try
        {
            _logger?.LogInformation("🧹 开始安全清理 Runtime 目录...");
            
            if (!Directory.Exists(NcfRuntimePath))
            {
                Directory.CreateDirectory(NcfRuntimePath);
                return Task.CompletedTask;
            }
            
            // 获取所有文件和文件夹
            var files = Directory.GetFiles(NcfRuntimePath, "*", SearchOption.AllDirectories);
            var directories = Directory.GetDirectories(NcfRuntimePath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length); // 先删除深层目录
            
            // 删除文件（跳过重要文件）
            foreach (var file in files)
            {
                if (ShouldPreserveFile(file))
                {
                    continue; // 跳过重要文件
                }
                
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"⚠️ 无法删除文件 {file}: {ex.Message}");
                }
            }
            
            // 删除目录（跳过重要目录）
            foreach (var directory in directories)
            {
                if (ShouldPreserveDirectory(directory))
                {
                    continue; // 跳过重要目录
                }
                
                try
                {
                    if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"⚠️ 无法删除目录 {directory}: {ex.Message}");
                }
            }
            
            _logger?.LogInformation("✅ Runtime 目录安全清理完成");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ 清理 Runtime 目录时出错: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 恢复保护的重要文件
    /// </summary>
    private async Task RestoreImportantFilesAsync()
    {
        try
        {
            _logger?.LogInformation("🔄 开始恢复重要文件...");
            
            var backupPath = GetBackupPath();
            
            if (!Directory.Exists(backupPath))
            {
                _logger?.LogInformation("ℹ️ 没有找到备份文件，跳过恢复");
                return;
            }
            
            // 恢复 App_Data 文件夹
            var backupAppDataPath = Path.Combine(backupPath, "App_Data");
            if (Directory.Exists(backupAppDataPath))
            {
                var appDataPath = Path.Combine(NcfRuntimePath, "App_Data");
                await CopyDirectoryAsync(backupAppDataPath, appDataPath);
                _logger?.LogInformation("✅ App_Data 文件夹已恢复");
            }
            
            // 🆕 恢复 logs 文件夹
            var backupLogsPath = Path.Combine(backupPath, "logs");
            if (Directory.Exists(backupLogsPath))
            {
                var logsPath = Path.Combine(NcfRuntimePath, "logs");
                await CopyDirectoryAsync(backupLogsPath, logsPath);
                _logger?.LogInformation("✅ logs 文件夹已恢复");
            }
            
            // 恢复 log 文件夹（向后兼容）
            var backupLogPath = Path.Combine(backupPath, "log");
            if (Directory.Exists(backupLogPath))
            {
                var logPath = Path.Combine(NcfRuntimePath, "log");
                await CopyDirectoryAsync(backupLogPath, logPath);
                _logger?.LogInformation("✅ log 文件夹已恢复");
            }
            
            // 🆕 智能恢复 appsettings 文件（带冲突检测）
            await RestoreAppSettingsFilesAsync(backupPath);
            
            // 清理临时备份
            try
            {
                Directory.Delete(backupPath, true);
                _logger?.LogInformation("🧹 临时备份已清理");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"⚠️ 清理临时备份时出错: {ex.Message}");
            }
            
            _logger?.LogInformation("✅ 重要文件恢复完成");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ 恢复重要文件时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 智能恢复 appsettings 配置文件（带冲突检测）
    /// </summary>
    private async Task RestoreAppSettingsFilesAsync(string backupPath)
    {
        try
        {
            var settingsBackupPath = Path.Combine(backupPath, "appsettings");
            
            if (!Directory.Exists(settingsBackupPath))
            {
                _logger?.LogInformation("ℹ️ 没有备份的配置文件，跳过");
                return;
            }
            
            var backupFiles = Directory.GetFiles(settingsBackupPath, "*", SearchOption.AllDirectories);
            
            foreach (var backupFile in backupFiles)
            {
                var fileName = Path.GetFileName(backupFile);
                
                // 还原文件名（移除路径分隔符替换）
                var originalFileName = fileName.Replace('_', Path.DirectorySeparatorChar);
                if (!originalFileName.EndsWith(".json"))
                {
                    // 如果不是 .json 结尾，可能是被替换的路径，尝试恢复
                    var parts = fileName.Split('_');
                    if (parts.Length > 1 && parts[^1].EndsWith(".json"))
                    {
                        originalFileName = parts[^1]; // 取最后一个部分作为文件名
                    }
                }
                
                var targetPath = Path.Combine(NcfRuntimePath, originalFileName);
                
                // 确保目标目录存在
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                
                // 🆕 检测冲突：如果新版本中也有这个文件，比较内容
                if (File.Exists(targetPath))
                {
                    var shouldOverwrite = await HandleAppSettingsConflictAsync(
                        originalFileName,
                        backupFile,  // 旧文件（备份）
                        targetPath   // 新文件（当前已解压的）
                    );
                    
                    if (shouldOverwrite)
                    {
                        // 用户选择覆盖：先备份新文件，然后用旧文件覆盖
                        var archiveFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}.backup-{DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(originalFileName)}";
                        var archivePath = Path.Combine(NcfRuntimePath, archiveFileName);
                        File.Copy(targetPath, archivePath, true);
                        _logger?.LogInformation($"📦 已存档新版本配置文件: {archiveFileName}");
                        
                        // 用旧配置覆盖
                        File.Copy(backupFile, targetPath, true);
                        _logger?.LogInformation($"✅ 已恢复旧配置文件: {originalFileName}");
                    }
                    else
                    {
                        // 用户选择保留新文件
                        _logger?.LogInformation($"⏭️ 保留新版本配置文件: {originalFileName}");
                        
                        // 将旧配置另存为 .old 文件供参考
                        var oldFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}.old-{DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(originalFileName)}";
                        var oldFilePath = Path.Combine(NcfRuntimePath, oldFileName);
                        File.Copy(backupFile, oldFilePath, true);
                        _logger?.LogInformation($"📋 旧配置已另存为: {oldFileName}");
                    }
                }
                else
                {
                    // 新版本中没有这个文件，直接恢复
                    File.Copy(backupFile, targetPath, true);
                    _logger?.LogInformation($"✅ 已恢复配置文件: {originalFileName}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ 恢复配置文件时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 处理 appsettings 配置文件冲突
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="oldFilePath">旧文件路径（备份）</param>
    /// <param name="newFilePath">新文件路径（当前）</param>
    /// <returns>true=使用旧文件覆盖，false=保留新文件</returns>
    private async Task<bool> HandleAppSettingsConflictAsync(string fileName, string oldFilePath, string newFilePath)
    {
        try
        {
            // 读取两个文件的内容
            var oldContent = await File.ReadAllTextAsync(oldFilePath);
            var newContent = await File.ReadAllTextAsync(newFilePath);
            
            // 比较内容
            if (oldContent.Trim() == newContent.Trim())
            {
                // 内容相同，直接使用新文件（不需要覆盖）
                _logger?.LogInformation($"ℹ️ 配置文件内容相同，无需处理: {fileName}");
                return false;
            }
            
            _logger?.LogWarning($"⚠️ 检测到配置文件冲突: {fileName}");
            _logger?.LogInformation($"   旧文件大小: {oldContent.Length} 字符");
            _logger?.LogInformation($"   新文件大小: {newContent.Length} 字符");
            
            // 如果设置了冲突处理回调，调用它
            if (OnAppSettingsConflict != null)
            {
                return await OnAppSettingsConflict(fileName, oldContent, newContent);
            }
            
            // 默认：保留新文件
            _logger?.LogInformation($"   默认行为：保留新版本文件");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"❌ 处理配置文件冲突时出错: {ex.Message}");
            // 出错时默认保留新文件
            return false;
        }
    }

    /// <summary>
    /// 递归复制目录
    /// </summary>
    private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        
        // 复制文件
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, true);
        }
        
        // 递归复制子目录
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            var targetSubDir = Path.Combine(targetDir, dirName);
            await CopyDirectoryAsync(subDir, targetSubDir);
        }
    }

    /// <summary>
    /// 判断是否应该保留文件
    /// </summary>
    private bool ShouldPreserveFile(string filePath)
    {
        var relativePath = Path.GetRelativePath(NcfRuntimePath, filePath);
        var fileName = Path.GetFileName(filePath);
        
        // 保留 App_Data 文件夹中的所有文件
        if (relativePath.StartsWith("App_Data", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 🆕 保留 logs/log 文件夹中的所有文件
        if (relativePath.StartsWith("logs", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("log", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 保留 appsettings*.json 文件
        if (fileName.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) && 
            fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 判断是否应该保留目录
    /// </summary>
    private bool ShouldPreserveDirectory(string directoryPath)
    {
        var relativePath = Path.GetRelativePath(NcfRuntimePath, directoryPath);
        
        // 保留 App_Data 文件夹
        if (relativePath.Equals("App_Data", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("App_Data" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 🆕 保留 logs/log 文件夹
        if (relativePath.Equals("logs", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals("log", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("logs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("log" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 获取备份路径
    /// </summary>
    private string GetBackupPath()
    {
        return Path.Combine(Path.GetDirectoryName(NcfRuntimePath) ?? AppDataPath, "backup");
    }
    
    #endregion

    private sealed class ReleaseSourceCandidate
    {
        public ReleaseSourceCandidate(string name, GitHubRelease release, TimeSpan metadataLatency)
        {
            Name = name;
            Release = release;
            MetadataLatency = metadataLatency;
        }

        public string Name { get; }
        public GitHubRelease Release { get; }
        public TimeSpan MetadataLatency { get; }
        public TimeSpan? PackageLatency { get; set; }
    }
}

// GitHub API 响应模型
public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("assets")]
    public GitHubAsset[]? Assets { get; set; }
}

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
}
