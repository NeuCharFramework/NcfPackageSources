using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NcfDesktopApp;

public class ApplicationOptions
{
    public bool AutoOpenBrowser { get; set; } = true;
    public bool CleanupDownloads { get; set; } = false;
    public bool CheckUpdateOnStartup { get; set; } = true;
}

class Program
{
    private static readonly HttpClient httpClient = new();
    private static ILogger<Program>? logger;
    private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NcfDesktopApp");
    private static readonly string NcfRuntimePath = Path.Combine(AppDataPath, "Runtime");
    private static readonly string DownloadsPath = Path.Combine(AppDataPath, "Downloads");
    
    // 支持的平台映射
    private static readonly Dictionary<(OSPlatform os, Architecture arch), string> PlatformMapping = new()
    {
        { (OSPlatform.Windows, Architecture.X64), "ncf-win-x64" },
        { (OSPlatform.Windows, Architecture.Arm64), "ncf-win-arm64" },
        { (OSPlatform.OSX, Architecture.X64), "ncf-osx-x64" },
        { (OSPlatform.OSX, Architecture.Arm64), "ncf-osx-arm64" },
        { (OSPlatform.Linux, Architecture.X64), "ncf-linux-x64" },
        { (OSPlatform.Linux, Architecture.Arm64), "ncf-linux-arm64" }
    };

    private static void ShowHelp()
    {
        Console.WriteLine("🚀 NCF 桌面应用程序");
        Console.WriteLine();
        Console.WriteLine("用法: dotnet run [选项]");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  --test         测试模式，仅验证API连接和平台检测");
        Console.WriteLine("  --auto-clean   强制启用自动清理下载文件功能");
        Console.WriteLine("  --help, -h     显示此帮助信息");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  dotnet run                    # 正常启动");
        Console.WriteLine("  dotnet run --auto-clean       # 启动并强制自动清理下载文件");
        Console.WriteLine("  dotnet run --test             # 测试模式");
        Console.WriteLine();
        Console.WriteLine("配置说明:");
        Console.WriteLine("  - 默认行为可通过 appsettings.json 中的 Application 节配置");
        Console.WriteLine("  - CleanupDownloads: 是否自动删除下载文件（默认: false）");
        Console.WriteLine("  - AutoOpenBrowser: 是否自动打开浏览器（默认: true）");
        Console.WriteLine("  - 命令行参数 --auto-clean 会覆盖配置文件设置");
        Console.WriteLine();
        Console.WriteLine("路径信息:");
        Console.WriteLine("  - 下载文件保存在用户数据目录的Downloads文件夹中");
        Console.WriteLine("  - NCF站点将启动在 http://localhost:5000");
        Console.WriteLine();
    }

    static async Task<int> Main(string[] args)
    {
        // 解析命令行参数
        bool testMode = args.Contains("--test");
        bool autoClean = args.Contains("--auto-clean");
        bool showHelp = args.Contains("--help") || args.Contains("-h");
        
        // 显示帮助信息
        if (showHelp)
        {
            ShowHelp();
            return 0;
        }
        
        // 创建主机和依赖注入
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(builder =>
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                
                // 绑定应用程序配置
                services.Configure<ApplicationOptions>(
                    context.Configuration.GetSection("Application"));
            })
            .Build();

        logger = host.Services.GetRequiredService<ILogger<Program>>();
        var appOptions = host.Services.GetRequiredService<IOptions<ApplicationOptions>>().Value;
        
        try
        {
            logger.LogInformation("🚀 NCF桌面应用启动中...");
            
            // 确保目录存在
            EnsureDirectoriesExist();
            
            // 检测当前平台
            var (currentOs, currentArch) = DetectCurrentPlatform();
            logger.LogInformation($"🖥️  检测到平台: {currentOs} {currentArch}");
            
            // 获取平台对应的包名
            if (!PlatformMapping.TryGetValue((currentOs, currentArch), out var platformKey))
            {
                logger.LogError($"❌ 不支持的平台: {currentOs} {currentArch}");
                return 1;
            }
            
            // 检查是否需要下载或更新
            var latestRelease = await GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                logger.LogWarning("⚠️  无法获取最新版本信息，检查是否有现有版本可用...");
                
                // 检查是否已经有可用的NCF版本
                var senparcWebDll = Path.Combine(NcfRuntimePath, "Senparc.Web.dll");
                if (File.Exists(senparcWebDll))
                {
                    logger.LogInformation("✅ 找到现有的NCF版本，直接启动...");
                    await StartNcfSiteAsync();
                    
                    // 根据配置决定是否打开浏览器
                    if (appOptions.AutoOpenBrowser)
                    {
                        var portFile = Path.Combine(AppDataPath, "port.txt");
                        if (File.Exists(portFile))
                        {
                            var portText = await File.ReadAllTextAsync(portFile);
                            if (int.TryParse(portText, out var port))
                            {
                                OpenBrowser($"http://localhost:{port}");
                            }
                        }
                    }
                    
                    logger.LogInformation("✨ NCF桌面应用启动完成！");
                    return 0;
                }
                else
                {
                    logger.LogError("❌ 无法获取最新版本信息且未找到现有版本");
                    return 1;
                }
            }
            
            var targetAsset = latestRelease.Assets?.FirstOrDefault(a => 
                a.Name?.StartsWith(platformKey) == true);
            
            if (targetAsset == null)
            {
                logger.LogError($"❌ 未找到适用于 {platformKey} 的发布包");
                return 1;
            }
            
            logger.LogInformation($"🎯 目标包: {targetAsset.Name}");
            logger.LogInformation($"📝 下载URL: {targetAsset.BrowserDownloadUrl}");
            logger.LogInformation($"📦 文件大小: {targetAsset.Size / 1024 / 1024}MB");
            
            // 测试模式：只验证API调用和URL解析
            if (testMode)
            {
                logger.LogInformation("🧪 测试模式：API调用和URL解析验证成功！");
                logger.LogInformation("💡 要实际运行应用程序，请使用: dotnet run");
                return 0;
            }
            
            // 检查是否需要下载
            var needsDownload = await CheckIfDownloadNeededAsync(targetAsset.Name!, targetAsset.Size);
            
            if (needsDownload)
            {
                logger.LogInformation("📥 开始下载最新版本...");
                await DownloadFileAsync(targetAsset.BrowserDownloadUrl!, targetAsset.Name!);
            }
            else
            {
                logger.LogInformation("✅ 文件已存在，无需重复下载");
            }
            
            // 检查是否需要解压
            var needsExtract = await CheckIfExtractNeededAsync(latestRelease.TagName!);
            
            if (needsExtract)
            {
                logger.LogInformation("📦 开始解压文件...");
                var zipPath = Path.Combine(DownloadsPath, targetAsset.Name!);
                await ExtractArchiveAsync(zipPath);
                
                // 命令行参数优先于配置文件设置
                bool shouldClean = autoClean || appOptions.CleanupDownloads;
                await SaveVersionAsync(latestRelease.TagName!, shouldClean);
            }
            else
            {
                logger.LogInformation("✅ 已是最新版本，无需重新解压");
            }
            
            // 启动NCF站点
            await StartNcfSiteAsync();
            
            // 根据配置决定是否打开浏览器
            if (appOptions.AutoOpenBrowser)
            {
                var portFile = Path.Combine(AppDataPath, "port.txt");
                var port = File.Exists(portFile) ? await File.ReadAllTextAsync(portFile) : "5001";
                var siteUrl = $"http://localhost:{port.Trim()}";
                OpenBrowser(siteUrl);
                logger.LogInformation($"🌏 已打开浏览器: {siteUrl}");
            }
            else
            {
                var portFile = Path.Combine(AppDataPath, "port.txt");
                var port = File.Exists(portFile) ? await File.ReadAllTextAsync(portFile) : "5001";
                logger.LogInformation($"🌐 站点地址: http://localhost:{port.Trim()}");
            }
            
            logger.LogInformation("🎉 NCF桌面应用启动完成！");
            logger.LogInformation("📝 按任意键退出应用...");
            
            Console.ReadKey();
            
            return 0;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "💥 应用程序发生错误");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
            return 1;
        }
    }
    
    private static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(NcfRuntimePath);
        Directory.CreateDirectory(DownloadsPath);
        logger?.LogInformation($"📁 应用数据目录: {AppDataPath}");
    }
    
    private static (OSPlatform os, Architecture arch) DetectCurrentPlatform()
    {
        var arch = RuntimeInformation.OSArchitecture;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return (OSPlatform.Windows, arch);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return (OSPlatform.OSX, arch);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return (OSPlatform.Linux, arch);
            
        throw new PlatformNotSupportedException("不支持的操作系统");
    }
    
    private static async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        try
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NcfDesktopApp/1.0");
            
            var response = await httpClient.GetStringAsync(
                "https://api.github.com/repos/NeuCharFramework/NCF/releases/latest");
            
            var release = JsonSerializer.Deserialize<GitHubRelease>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            logger?.LogInformation($"📦 最新版本: {release?.TagName}");
            return release;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ 获取最新版本失败");
            return null;
        }
    }
    
    private static Task<bool> CheckIfDownloadNeededAsync(string assetName, long expectedSize)
    {
        var downloadPath = Path.Combine(DownloadsPath, assetName);
        
        // 检查文件是否存在
        if (!File.Exists(downloadPath))
        {
            return Task.FromResult(true);
        }
        
        try
        {
            // 检查文件大小是否匹配
            var fileInfo = new FileInfo(downloadPath);
            return Task.FromResult(fileInfo.Length != expectedSize);
        }
        catch
        {
            return Task.FromResult(true);
        }
    }
    
    private static async Task<bool> CheckIfExtractNeededAsync(string tagName)
    {
        var versionFile = Path.Combine(AppDataPath, "version.txt");
        var senparcWebDll = Path.Combine(NcfRuntimePath, "Senparc.Web.dll");
        
        // 检查版本文件和提取的文件是否存在
        if (!File.Exists(versionFile) || !File.Exists(senparcWebDll))
        {
            return true;
        }
        
        try
        {
            var currentVersion = await File.ReadAllTextAsync(versionFile);
            return currentVersion.Trim() != tagName.Trim();
        }
        catch
        {
            return true;
        }
    }
    
    private static async Task SaveVersionAsync(string tagName, bool autoClean = false)
    {
        var versionFile = Path.Combine(AppDataPath, "version.txt");
        await File.WriteAllTextAsync(versionFile, tagName);
        logger?.LogInformation($"📦 最新版本: {tagName}");
        logger?.LogInformation("✅ 安装完成");
        
        // 根据参数决定是否删除下载的ZIP文件
        if (autoClean)
        {
            try
            {
                var zipFiles = Directory.GetFiles(DownloadsPath, "*.zip");
                foreach (var zipFile in zipFiles)
                {
                    File.Delete(zipFile);
                    logger?.LogInformation($"🗑️  已清理下载文件: {Path.GetFileName(zipFile)}");
                }
                logger?.LogInformation("💾 已自动清理下载文件以节省空间");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "⚠️  清理下载文件时出现问题，可手动删除Downloads目录中的文件");
            }
        }
        else
        {
            var zipFiles = Directory.GetFiles(DownloadsPath, "*.zip");
            if (zipFiles.Length > 0)
            {
                logger?.LogInformation($"📦 下载文件已保存在: {DownloadsPath}");
                logger?.LogInformation("💡 提示: 使用 --auto-clean 参数可自动清理下载文件");
            }
        }
    }
    

    
    private static async Task<string> DownloadFileAsync(string downloadUrl, string fileName)
    {
        var zipPath = Path.Combine(DownloadsPath, fileName);
        
        logger?.LogInformation($"📥 开始下载: {fileName}");
        
        using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        
        using (var contentStream = await response.Content.ReadAsStreamAsync())
        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, false))
        {
            var buffer = new byte[8192];
            var totalRead = 0L;
            int read;
            
            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;
                
                if (totalBytes > 0)
                {
                    var progress = (double)totalRead / totalBytes * 100;
                    Console.Write($"\r📥 下载进度: {progress:F1}%");
                }
            }
            
            // 确保数据写入磁盘
            await fileStream.FlushAsync();
        } // using语句确保文件流在这里被完全释放
        
        Console.WriteLine();
        logger?.LogInformation("✅ 下载完成");
        
        return zipPath;
    }
    
    private static async Task ExtractArchiveAsync(string zipPath)
    {
        // 清理旧的运行时文件
        if (Directory.Exists(NcfRuntimePath))
        {
            Directory.Delete(NcfRuntimePath, true);
            Directory.CreateDirectory(NcfRuntimePath);
        }
        
        logger?.LogInformation("📦 正在解压文件...");
        
        // 添加小延迟确保文件句柄完全释放
        await Task.Delay(100);
        
        try
        {
            // 使用自定义解压逻辑处理跨平台路径分隔符问题
            await ExtractZipWithCorrectPathsAsync(zipPath, NcfRuntimePath);
            logger?.LogInformation("✅ 解压完成");
        }
        catch (IOException ex) when (ex.Message.Contains("being used by another process"))
        {
            // 如果还是被占用，等待更长时间再试
            logger?.LogWarning("⚠️  文件被占用，等待释放...");
            await Task.Delay(2000);
            
            await ExtractZipWithCorrectPathsAsync(zipPath, NcfRuntimePath);
            logger?.LogInformation("✅ 解压完成");
        }
    }
    
    private static async Task ExtractZipWithCorrectPathsAsync(string zipPath, string extractPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        
        foreach (var entry in archive.Entries)
        {
            // 跳过目录条目
            if (string.IsNullOrEmpty(entry.Name))
                continue;
                
            // 将Windows路径分隔符转换为当前平台的路径分隔符
            var relativePath = entry.FullName.Replace('\\', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(extractPath, relativePath);
            
            // 确保目标目录存在
            var directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // 解压文件
            using var entryStream = entry.Open();
            using var fileStream = File.Create(fullPath);
            await entryStream.CopyToAsync(fileStream);
        }
    }
    
    private static async Task StartNcfSiteAsync()
    {
        var senparcWebDll = Path.Combine(NcfRuntimePath, "Senparc.Web.dll");
        
        if (!File.Exists(senparcWebDll))
        {
            throw new FileNotFoundException($"未找到Senparc.Web.dll文件: {senparcWebDll}");
        }
        
        // 找一个可用的端口
        var availablePort = await FindAvailablePortAsync(5000);
        
        logger?.LogInformation("🌐 启动NCF站点...");
        logger?.LogInformation($"🔌 使用端口: {availablePort}");
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"Senparc.Web.dll --urls=http://localhost:{availablePort}",
                WorkingDirectory = NcfRuntimePath,
                UseShellExecute = true, // 使用shell启动，更接近手动运行
                CreateNoWindow = false
            };
            
            // 设置环境变量指定端口（双重保险）
            startInfo.Environment["ASPNETCORE_URLS"] = $"http://localhost:{availablePort}";
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
            
            var process = Process.Start(startInfo);
            
            if (process == null)
            {
                throw new InvalidOperationException("无法启动NCF进程");
            }
            
            // 使用shell启动时不能重定向输出，NCF进程会在独立控制台中显示输出
            logger?.LogInformation("📝 NCF进程在独立控制台中运行，输出将在那里显示");
            
            // 等待站点完全启动并可用
            var siteUrl = $"http://localhost:{availablePort}";
            var isReady = await WaitForSiteReadyAsync(siteUrl, process, logger);
            
            if (!isReady)
            {
                throw new InvalidOperationException("NCF站点启动超时或失败");
            }
            
            logger?.LogInformation("✅ NCF站点启动成功");
            logger?.LogInformation($"🌐 站点地址: {siteUrl}");
            
            // 保存端口信息供浏览器使用
            await File.WriteAllTextAsync(Path.Combine(AppDataPath, "port.txt"), availablePort.ToString());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ 启动NCF站点失败");
            throw;
        }
    }
    
    private static async Task<int> FindAvailablePortAsync(int startPort = 5000)
    {
        const int maxPort = 5300;
        
        for (int port = startPort; port <= maxPort; port++)
        {
            // 先用lsof检查端口是否被占用
            if (await IsPortInUseAsync(port))
            {
                continue;
            }
            
            // 再用TcpListener确认端口可用
            try
            {
                using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch
            {
                // 端口被占用，继续尝试下一个
                continue;
            }
        }
        
        throw new InvalidOperationException($"无法找到可用端口（范围: {startPort} - {maxPort}）");
    }
    
    private static async Task<bool> IsPortInUseAsync(int port)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "lsof",
                Arguments = $"-i :{port}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(startInfo);
            await process.WaitForExitAsync();
            
            // lsof返回0表示找到了占用该端口的进程
            return process.ExitCode == 0;
        }
        catch
        {
            // 如果lsof命令失败，假设端口未被占用
            return false;
        }
    }
    
    private static async Task<bool> WaitForSiteReadyAsync(string siteUrl, Process process, ILogger? logger)
    {
        const int maxWaitTimeSeconds = 60; // 最大等待60秒
        const int checkIntervalMs = 2000; // 每2秒检查一次
        
        var httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5) // HTTP请求超时5秒
        };
        
        var stopwatch = Stopwatch.StartNew();
        int attemptCount = 0;
        
        logger?.LogInformation("⏳ 等待NCF站点完全启动...");
        
        while (stopwatch.ElapsedMilliseconds < maxWaitTimeSeconds * 1000)
        {
            attemptCount++;
            
            try
            {
                // 检查进程是否还在运行
                if (process.HasExited)
                {
                    logger?.LogError($"❌ NCF进程已退出，退出代码: {process.ExitCode}");
                    return false;
                }
                
                // 尝试访问站点
                logger?.LogInformation($"🔍 检查站点状态 (第{attemptCount}次)...");
                
                var response = await httpClient.GetAsync(siteUrl);
                
                // 检查响应状态
                if (response.IsSuccessStatusCode)
                {
                    logger?.LogInformation($"✅ 站点响应正常 (状态码: {(int)response.StatusCode})");
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                         response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    // 403或503表示服务正在启动中，继续等待
                    logger?.LogInformation($"⚠️  站点正在初始化中 (状态码: {(int)response.StatusCode})，继续等待...");
                }
                else
                {
                    logger?.LogWarning($"⚠️  收到意外响应 (状态码: {(int)response.StatusCode})，继续等待...");
                }
            }
            catch (HttpRequestException)
            {
                // 连接失败，通常表示服务还没启动
                logger?.LogInformation("🔄 站点还未准备就绪，继续等待...");
            }
            catch (TaskCanceledException)
            {
                // 请求超时
                logger?.LogInformation("⏰ 请求超时，站点可能还在启动中...");
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"⚠️  健康检查异常: {ex.Message}");
            }
            
            // 等待一段时间后再次检查
            await Task.Delay(checkIntervalMs);
        }
        
        logger?.LogError($"❌ 等待超时 ({maxWaitTimeSeconds}秒)，站点可能启动失败");
        return false;
    }
    
    private static void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            
            logger?.LogInformation($"🌏 已打开浏览器: {url}");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, $"⚠️  无法自动打开浏览器，请手动访问: {url}");
        }
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
