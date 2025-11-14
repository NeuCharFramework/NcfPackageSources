using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// WebView2 Runtime 检测和安装服务
/// </summary>
public class WebView2Service
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebView2Service>? _logger;
    
    // WebView2 Runtime 注册表路径
    private const string WebView2RegistryKey = @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
    private const string WebView2RegistryKey64 = @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
    
    // WebView2 Bootstrapper 下载链接（自动检测架构）
    private const string WebView2BootstrapperUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
    
    public WebView2Service(HttpClient httpClient, ILogger<WebView2Service>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    /// <summary>
    /// 检查 WebView2 Runtime 是否已安装
    /// </summary>
    public bool IsWebView2Installed()
    {
        // 仅在 Windows 上检查
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return true; // 非 Windows 平台，WebView.Avalonia 会使用其他 WebView
        }
        
        try
        {
            // 检查 32 位注册表路径
            using (var key = Registry.LocalMachine.OpenSubKey(WebView2RegistryKey))
            {
                if (key != null)
                {
                    var version = key.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version))
                    {
                        _logger?.LogInformation($"✅ WebView2 Runtime 已安装，版本: {version}");
                        return true;
                    }
                }
            }
            
            // 检查 64 位注册表路径
            using (var key = Registry.LocalMachine.OpenSubKey(WebView2RegistryKey64))
            {
                if (key != null)
                {
                    var version = key.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version))
                    {
                        _logger?.LogInformation($"✅ WebView2 Runtime 已安装，版本: {version}");
                        return true;
                    }
                }
            }
            
            _logger?.LogWarning("❌ WebView2 Runtime 未安装");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "检查 WebView2 Runtime 时出错");
            return false;
        }
    }
    
    /// <summary>
    /// 获取已安装的 WebView2 版本
    /// </summary>
    public string? GetInstalledVersion()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }
        
        try
        {
            // 检查 32 位注册表
            using (var key = Registry.LocalMachine.OpenSubKey(WebView2RegistryKey))
            {
                if (key != null)
                {
                    var version = key.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version))
                    {
                        return version;
                    }
                }
            }
            
            // 检查 64 位注册表
            using (var key = Registry.LocalMachine.OpenSubKey(WebView2RegistryKey64))
            {
                if (key != null)
                {
                    var version = key.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version))
                    {
                        return version;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取 WebView2 版本时出错");
        }
        
        return null;
    }
    
    /// <summary>
    /// 自动下载并安装 WebView2 Runtime
    /// </summary>
    /// <param name="progress">进度报告</param>
    public async Task<bool> InstallWebView2RuntimeAsync(IProgress<(string message, double percentage)>? progress = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger?.LogInformation("非 Windows 平台，跳过 WebView2 安装");
            return true;
        }
        
        try
        {
            _logger?.LogInformation("🚀 开始安装 WebView2 Runtime...");
            progress?.Report(("正在下载 WebView2 Runtime...", 0));
            
            // 下载 Bootstrapper
            var tempPath = Path.Combine(Path.GetTempPath(), "WebView2Bootstrapper.exe");
            
            _logger?.LogInformation($"下载 WebView2 Bootstrapper: {WebView2BootstrapperUrl}");
            progress?.Report(("下载中...", 10));
            
            using (var response = await _httpClient.GetAsync(WebView2BootstrapperUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
            
            _logger?.LogInformation("✅ WebView2 Bootstrapper 下载完成");
            progress?.Report(("下载完成，正在安装...", 50));
            
            // 运行安装程序
            var processStartInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = "/silent /install",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            _logger?.LogInformation("🔧 运行 WebView2 安装程序...");
            
            using (var process = Process.Start(processStartInfo))
            {
                if (process == null)
                {
                    _logger?.LogError("❌ 无法启动 WebView2 安装程序");
                    return false;
                }
                
                // 等待安装完成，最多等待 5 分钟
                var timeout = TimeSpan.FromMinutes(5);
                var startTime = DateTime.Now;
                
                while (!process.HasExited)
                {
                    if (DateTime.Now - startTime > timeout)
                    {
                        _logger?.LogError("❌ WebView2 安装超时");
                        process.Kill();
                        return false;
                    }
                    
                    await Task.Delay(1000);
                    
                    // 更新进度
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    var progressPercent = Math.Min(50 + (elapsed / 300 * 50), 100);
                    progress?.Report(("正在安装...", progressPercent));
                }
                
                await process.WaitForExitAsync();
                
                _logger?.LogInformation($"WebView2 安装程序退出，退出码: {process.ExitCode}");
                
                // 退出码 0 表示成功
                if (process.ExitCode == 0)
                {
                    progress?.Report(("安装完成，正在验证...", 90));
                    
                    _logger?.LogInformation("WebView2 安装程序退出成功，开始验证...");
                    
                    // 等待注册表更新，最多重试 10 次
                    bool installed = false;
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(1000); // 每次等待 1 秒
                        installed = IsWebView2Installed();
                        
                        if (installed)
                        {
                            _logger?.LogInformation($"✅ 验证成功（第 {i + 1} 次尝试）");
                            break;
                        }
                        
                        _logger?.LogInformation($"⏳ 等待注册表更新... ({i + 1}/10)");
                    }
                    
                    if (installed)
                    {
                        _logger?.LogInformation("✅ WebView2 Runtime 安装成功");
                        progress?.Report(("WebView2 Runtime 安装成功", 100));
                        
                        // 清理临时文件
                        try
                        {
                            File.Delete(tempPath);
                        }
                        catch { }
                        
                        return true;
                    }
                    else
                    {
                        _logger?.LogWarning("⚠️ WebView2 安装程序退出成功，但验证超时");
                        _logger?.LogWarning("   注意：WebView2 可能已安装，但注册表尚未更新");
                        _logger?.LogWarning("   建议：重启应用或手动验证");
                        
                        // 即使验证失败，也返回 true（因为退出码为 0）
                        // 让应用继续运行，用户可以手动重启
                        return true;
                    }
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger?.LogError($"❌ WebView2 安装失败，退出码: {process.ExitCode}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger?.LogError($"错误信息: {error}");
                    }
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "安装 WebView2 Runtime 时出错");
            progress?.Report(($"安装失败: {ex.Message}", -1));
            return false;
        }
    }
    
    /// <summary>
    /// 检测并在需要时安装 WebView2
    /// </summary>
    public async Task<bool> EnsureWebView2InstalledAsync(IProgress<(string message, double percentage)>? progress = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return true; // 非 Windows 平台无需安装
        }
        
        // 检查是否已安装
        if (IsWebView2Installed())
        {
            var version = GetInstalledVersion();
            _logger?.LogInformation($"✅ WebView2 Runtime 已安装，版本: {version}");
            progress?.Report(($"WebView2 Runtime 已就绪 (版本: {version})", 100));
            return true;
        }
        
        // 未安装，尝试自动安装
        _logger?.LogWarning("⚠️ WebView2 Runtime 未安装，尝试自动安装...");
        progress?.Report(("检测到 WebView2 未安装，正在自动安装...", 0));
        
        return await InstallWebView2RuntimeAsync(progress);
    }
}

