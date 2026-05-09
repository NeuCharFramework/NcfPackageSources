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

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// CLI 进程输出处理委托
/// </summary>
/// <param name="output">输出内容</param>
/// <param name="isError">是否为错误输出</param>
public delegate void ProcessOutputHandler(string output, bool isError);

public class NcfService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NcfService>? _logger;
    
    // 路径配置
    public static string AppDataPath { get; private set; } = string.Empty;
    public static string DownloadsPath { get; private set; } = string.Empty;
    public static string NcfRuntimePath { get; private set; } = string.Empty;

    /// <summary>
    /// 备用更新源站点根地址（默认 https://www.ncf.pub）。元数据地址为 {此属性}/NcfPackages/latest-release.json。
    /// 可由用户在设置中修改，并通过 desktop-user-settings.json 持久化。
    /// </summary>
    public string MirrorServerBaseUrl { get; set; } = DesktopUserSettings.DefaultMirrorServerBaseUrl;

    /// <summary>
    /// 备用元数据 latest-release.json 的完整 URL。
    /// </summary>
    public string GetMirrorMetadataUrl()
    {
        var b = DesktopSettingsStore.NormalizeMirrorServerBase(MirrorServerBaseUrl);
        return $"{b}/NcfPackages/latest-release.json";
    }

    /// <summary>
    /// 用户将「备用更新源」设为非默认根地址（如本机 https://localhost:xxx）时，应优先从该地址拉取 latest-release.json 与其中给出的下载链接。
    /// </summary>
    private bool PreferMirrorMetadataFirst =>
        !string.Equals(
            DesktopSettingsStore.NormalizeMirrorServerBase(MirrorServerBaseUrl),
            DesktopSettingsStore.NormalizeMirrorServerBase(DesktopUserSettings.DefaultMirrorServerBaseUrl),
            StringComparison.OrdinalIgnoreCase);
    
    // 🆕 配置文件冲突处理回调
    // 参数: fileName, oldContent, newContent
    // 返回: true=使用新文件（覆盖），false=保留旧文件
    public Func<string, string, string, Task<bool>>? OnAppSettingsConflict { get; set; }
    
    /// <summary>
    /// CLI 进程输出回调（参数：输出内容, 是否为错误输出）
    /// </summary>
    public ProcessOutputHandler? OnProcessOutput { get; set; }
    
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

        if (PreferMirrorMetadataFirst)
        {
            var mirrorUrl = GetMirrorMetadataUrl();
            _logger?.LogInformation("已配置自定义镜像根地址，优先从元数据地址获取版本: {Url}", mirrorUrl);
            var fromMirror = await TryGetLatestReleaseFromMirrorAsync(cancellationToken).ConfigureAwait(false);
            if (fromMirror != null)
            {
                return fromMirror;
            }

            _logger?.LogWarning("自定义镜像元数据不可用，回退到 GitHub API");
            var fromGitHub = await TryGetLatestReleaseFromGitHubAsync(cancellationToken).ConfigureAwait(false);
            if (fromGitHub != null)
            {
                return fromGitHub;
            }

            return null;
        }

        var fromGitHubDefault = await TryGetLatestReleaseFromGitHubAsync(cancellationToken).ConfigureAwait(false);
        if (fromGitHubDefault != null)
        {
            return fromGitHubDefault;
        }

        var fallbackMirrorUrl = GetMirrorMetadataUrl();
        _logger?.LogWarning("GitHub API 不可用，尝试备用元数据地址: {Mirror}", fallbackMirrorUrl);
        return await TryGetLatestReleaseFromMirrorAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<GitHubRelease?> TryGetLatestReleaseFromGitHubAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogInformation("从 GitHub 获取最新版本信息...");
            var response = await _httpClient.GetStringAsync("https://api.github.com/repos/NeuCharFramework/NCF/releases/latest", cancellationToken).ConfigureAwait(false);
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
            }
        }
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
    
    public async Task<Process> StartNcfProcessAsync(int port, CancellationToken cancellationToken = default)
    {
        // 确定 NCF 应用所在目录（兼容压缩包内嵌套目录）
        var ncfAppDir = FindNcfAppDirectory() ?? NcfRuntimePath;

        // 路径定义（基于实际 NCF 目录）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // 尝试去除隔离属性，避免 Gatekeeper 阻止启动
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
            // 确保可执行权限
            TryMakeExecutable(exePathUnix);
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
                throw new FileNotFoundException($"未找到 NCF 启动文件（既没有自包含可执行，也没有 dll）: {NcfRuntimePath}");
            }

            string dotnetPath;
            if (IsDotnetInstalled())
            {
                dotnetPath = "dotnet";
            }
            else
            {
                // 自动安装用户级 .NET 8 ASP.NET Core 运行时
                dotnetPath = await EnsureDotnetAvailableAsync(cancellationToken);
            }

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
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            startInfo.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
        }

        // 如果使用本地 dotnet ，补充 DOTNET_ROOT 和 PATH 以保证宿主可定位到运行时
        var localDotnet = GetLocalDotnetPath();
        if (File.Exists(localDotnet))
        {
            var localRoot = GetLocalDotnetInstallDir();
            startInfo.Environment["DOTNET_ROOT"] = localRoot;
            var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            startInfo.Environment["PATH"] = string.IsNullOrEmpty(existingPath)
                ? localRoot
                : localRoot + Path.PathSeparator + existingPath;
        }

        // 捕获进程输出，便于诊断
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
        
        var process = Process.Start(startInfo);
        
        // 附加输出捕获事件处理
        AttachProcessOutputHandlers(process);
        // 若自包含可执行在 macOS 被 Gatekeeper 杀死或依赖缺失导致瞬退，尝试回退到 dotnet 方式
        if ((process == null || process.HasExited) && File.Exists(Path.Combine(ncfAppDir, "Senparc.Web.dll")))
        {
            _logger?.LogWarning("检测到自包含启动失败，回退到 dotnet 方式...");
            string dotnetPath;
            if (IsDotnetInstalled())
            {
                dotnetPath = "dotnet";
            }
            else
            {
                dotnetPath = await EnsureDotnetAvailableAsync(cancellationToken);
            }

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
            fb.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                fb.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
            }
            var local2 = GetLocalDotnetPath();
            if (File.Exists(local2))
            {
                var root2 = GetLocalDotnetInstallDir();
                fb.Environment["DOTNET_ROOT"] = root2;
                var path2 = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                fb.Environment["PATH"] = string.IsNullOrEmpty(path2) ? root2 : root2 + Path.PathSeparator + path2;
            }
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
                    string dotnetPath2 = IsDotnetInstalled() ? "dotnet" : await EnsureDotnetAvailableAsync(cancellationToken);
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
                    fb2.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        fb2.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
                    }
                    var local3 = GetLocalDotnetPath();
                    if (File.Exists(local3))
                    {
                        var root3 = GetLocalDotnetInstallDir();
                        fb2.Environment["DOTNET_ROOT"] = root3;
                        var path3 = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                        fb2.Environment["PATH"] = string.IsNullOrEmpty(path3) ? root3 : root3 + Path.PathSeparator + path3;
                    }
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

    private static string? FindNcfAppDirectory()
    {
        try
        {
            // 优先检查根目录
            var rootDll = Path.Combine(NcfRuntimePath, "Senparc.Web.dll");
            var rootWinExe = Path.Combine(NcfRuntimePath, "Senparc.Web.exe");
            var rootUnixExe = Path.Combine(NcfRuntimePath, "Senparc.Web");
            if (File.Exists(rootDll) || File.Exists(rootWinExe) || File.Exists(rootUnixExe))
            {
                return NcfRuntimePath;
            }

            // 递归查找（只搜索两层以降低成本）
            foreach (var dir in Directory.GetDirectories(NcfRuntimePath))
            {
                if (ContainsNcfApp(dir)) return dir;
                foreach (var sub in Directory.GetDirectories(dir))
                {
                    if (ContainsNcfApp(sub)) return sub;
                }
            }
        }
        catch
        {
            // ignore scanning errors
        }
        return null;
    }

    private static bool ContainsNcfApp(string directory)
    {
        var dll = Path.Combine(directory, "Senparc.Web.dll");
        var winExe = Path.Combine(directory, "Senparc.Web.exe");
        var unixExe = Path.Combine(directory, "Senparc.Web");
        return File.Exists(dll) || File.Exists(winExe) || File.Exists(unixExe);
    }

    private static bool IsDotnetInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(3000);
            return p.ExitCode == 0;
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

    private async Task<string> EnsureDotnetAvailableAsync(CancellationToken cancellationToken)
    {
        var localDotnet = GetLocalDotnetPath();
        if (File.Exists(localDotnet))
        {
            return localDotnet;
        }

        await InstallLocalDotnetRuntimeAsync(cancellationToken);
        // 为 Unix 平台确保可执行权限
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TryMakeExecutable(localDotnet);
        }

        if (!File.Exists(localDotnet))
        {
            throw new InvalidOperationException("自动安装 .NET Runtime 失败，请手动安装 .NET 8 运行时或使用自包含的 NCF 包。");
        }
        return localDotnet;
    }

    private async Task InstallLocalDotnetRuntimeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var installDir = GetLocalDotnetInstallDir();
            Directory.CreateDirectory(installDir);

            var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            _logger?.LogInformation($"准备安装 .NET 运行时到: {installDir} (架构: {arch})");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // 使用官方 dotnet-install.ps1 安装到用户目录，无需管理员权限
                var scriptUrl = "https://dot.net/v1/dotnet-install.ps1";
                var scriptPath = Path.Combine(installDir, "dotnet-install.ps1");
                var scriptBytes = await _httpClient.GetByteArrayAsync(scriptUrl, cancellationToken);
                await File.WriteAllBytesAsync(scriptPath, scriptBytes, cancellationToken);
                _logger?.LogInformation("下载 dotnet-install.ps1 完成，开始安装 .NET Runtime...");

                // 先安装 .NET Runtime（包含 dotnet 主机）
                var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Runtime dotnet -Channel 8.0 -Architecture {arch} -InstallDir \"{installDir}\"";
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
                var argsAsp = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Runtime aspnetcore -Channel 8.0 -Architecture {arch} -InstallDir \"{installDir}\"";
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
                var args = $"\"{scriptPath}\" --runtime dotnet --channel 8.0 --architecture {arch} --install-dir \"{installDir}\"";
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
                var argsAsp = $"\"{scriptPath}\" --runtime aspnetcore --channel 8.0 --architecture {arch} --install-dir \"{installDir}\"";
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
    
    public async Task<bool> WaitForSiteReadyAsync(string siteUrl, Process? process, int timeoutSeconds, CancellationToken cancellationToken = default)
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
                    // 内容校验：尽量确认是 NCF 页而不是其它服务
                    var looksLikeNcf = content.IndexOf("Senparc", StringComparison.OrdinalIgnoreCase) >= 0
                                        || content.IndexOf("NCF", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (looksLikeNcf)
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
        if (PreferMirrorMetadataFirst)
        {
            try
            {
                using var response = await _httpClient.GetAsync(GetMirrorMetadataUrl());
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // 继续尝试 GitHub
            }

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

        try
        {
            using var response = await _httpClient.GetAsync("https://api.github.com/repos/NeuCharFramework/NCF/releases/latest");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch
        {
            // 继续尝试备用源
        }

        try
        {
            using var response = await _httpClient.GetAsync(GetMirrorMetadataUrl());
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