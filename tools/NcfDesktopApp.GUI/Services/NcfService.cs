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
        var process = Process.Start(startInfo);
        if (process != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var so = await process.StandardOutput.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(so)) _logger?.LogInformation("NCF 输出:\n" + so);
                }
                catch { }
            });
            _ = Task.Run(async () =>
            {
                try
                {
                    var se = await process.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(se)) _logger?.LogWarning("NCF 错误:\n" + se);
                }
                catch { }
            });
        }
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
                RedirectStandardError = true
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
                        RedirectStandardError = true
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