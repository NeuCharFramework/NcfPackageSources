using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NcfDesktopApp.GUI.Services;

public class NcfService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NcfService>? _logger;
    
    // 路径配置
    public static string AppDataPath { get; private set; } = string.Empty;
    public static string DownloadsPath { get; private set; } = string.Empty;
    public static string NcfRuntimePath { get; private set; } = string.Empty;
    
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
        try
        {
            _logger?.LogInformation("获取最新版本信息...");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NCF-Desktop-App");
            
            var response = await _httpClient.GetStringAsync("https://api.github.com/repos/NeuCharFramework/NCF/releases/latest", cancellationToken);
            
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);
            _logger?.LogInformation($"获取到最新版本: {release?.TagName}");
            
            return release;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取最新版本失败");
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
        
        _logger?.LogInformation($"开始下载: {fileName}");
        
        using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        
        var buffer = new byte[8192];
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            downloadedBytes += bytesRead;
            
            if (totalBytes > 0)
            {
                var progressPercent = (double)downloadedBytes / totalBytes * 100;
                progress?.Report(progressPercent);
            }
        }
        
        _logger?.LogInformation($"下载完成: {fileName}");
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
        
        // 清理旧文件
        if (Directory.Exists(NcfRuntimePath))
        {
            Directory.Delete(NcfRuntimePath, true);
        }
        Directory.CreateDirectory(NcfRuntimePath);
        
        await ExtractZipWithCorrectPathsAsync(zipPath, NcfRuntimePath, progress, cancellationToken);
        
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
    
    public async Task<Process> StartNcfProcessAsync(int port, CancellationToken cancellationToken = default)
    {
        var senparcWebDll = Path.Combine(NcfRuntimePath, "Senparc.Web.dll");
        
        if (!File.Exists(senparcWebDll))
        {
            throw new FileNotFoundException($"未找到Senparc.Web.dll文件: {senparcWebDll}");
        }
        
        _logger?.LogInformation($"启动NCF站点，端口: {port}");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"Senparc.Web.dll --urls=http://localhost:{port}",
            WorkingDirectory = NcfRuntimePath,
            UseShellExecute = false, // 必须设置为false才能使用环境变量
            CreateNoWindow = false
        };
        
        startInfo.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            startInfo.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
        }
        
        var process = Process.Start(startInfo);
        
        if (process == null)
        {
            throw new InvalidOperationException("无法启动NCF进程");
        }
        
        return process;
    }
    
    public async Task<bool> WaitForSiteReadyAsync(string siteUrl, Process? process, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;
                
            if (process?.HasExited == true)
                return false;
            
            try
            {
                using var response = await _httpClient.GetAsync(siteUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                // 忽略连接错误，继续等待
            }
            
            await Task.Delay(2000, cancellationToken);
        }
        
        return false;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("https://api.github.com/repos/NeuCharFramework/NCF/releases/latest");
            return response.IsSuccessStatusCode;
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

        var needsDownload = await CheckIfDownloadNeededAsync(targetAsset.Name!, targetAsset.Size);
        
        if (needsDownload)
        {
            progress.Report(($"正在下载 {targetAsset.Name}...", -1));
            
            var downloadProgress = new Progress<double>(value =>
            {
                progress.Report(($"下载中... {value:F1}%", value * 0.6));
            });

            await DownloadFileAsync(targetAsset.BrowserDownloadUrl!, targetAsset.Name!, downloadProgress, cancellationToken);
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

    public async Task CleanupDownloadsAsync()
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
    }
    
    #region 私有方法
    
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
    
    private async Task SaveVersionAsync(string version)
    {
        var versionFile = Path.Combine(NcfRuntimePath, "version.txt");
        await File.WriteAllTextAsync(versionFile, version);
    }
    
    #endregion
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