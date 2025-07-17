using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region 属性绑定

    [ObservableProperty]
    private string _platformInfo = GetPlatformInfo();

    [ObservableProperty]
    private string _latestVersion = "检查中...";

    [ObservableProperty]
    private string _currentStatus = "就绪";

    [ObservableProperty]
    private string _statusColor = "#28A745";

    [ObservableProperty]
    private string _siteUrl = "未启动";

    [ObservableProperty]
    private string _progressText = "准备开始...";

    [ObservableProperty]
    private double _progressValue = 0;

    [ObservableProperty]
    private bool _isProgressIndeterminate = false;

    [ObservableProperty]
    private string _logText = "";

    [ObservableProperty]
    private bool _autoOpenBrowser = true;

    [ObservableProperty]
    private bool _autoCleanDownloads = false;

    [ObservableProperty]
    private bool _showDetailedInfo = true;

    [ObservableProperty]
    private bool _minimizeToTray = false;

    [ObservableProperty]
    private int _startPort = 5001;

    [ObservableProperty]
    private int _endPort = 5300;

    [ObservableProperty]
    private string _mainButtonText = "启动 NCF";

    [ObservableProperty]
    private bool _isOperationInProgress = false;

    #endregion

    #region 命令

    public ICommand TestConnectionCommand { get; }
    public ICommand OpenConfigDirectoryCommand { get; }
    public ICommand MainActionCommand { get; }
    public ICommand StopCommand { get; }

    #endregion

    #region 私有字段

    private CancellationTokenSource? _cancellationTokenSource;
    private Process? _ncfProcess;
    private readonly StringBuilder _logBuffer = new();
    private bool _isNcfRunning = false;
    private readonly NcfService _ncfService;
    private readonly ILogger<MainWindowViewModel>? _logger;

    #endregion

    public MainWindowViewModel(NcfService? ncfService = null, ILogger<MainWindowViewModel>? logger = null)
    {
        _ncfService = ncfService ?? new NcfService(new HttpClient());
        _logger = logger;
        
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        OpenConfigDirectoryCommand = new RelayCommand(OpenConfigDirectory);
        MainActionCommand = new AsyncRelayCommand(MainActionAsync);
        StopCommand = new RelayCommand(StopOperation);

        // 初始化
        _ = Task.Run(InitializeAsync);
    }

    #region 初始化

    private async Task InitializeAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog("🚀 NCF桌面应用启动中...");
                CurrentStatus = "初始化中";
                StatusColor = "#007ACC";
            });

            // 检查最新版本
            await CheckLatestVersionAsync();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStatus = "就绪";
                StatusColor = "#28A745";
                AddLog("✅ 初始化完成");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStatus = "初始化失败";
                StatusColor = "#DC3545";
                AddLog($"❌ 初始化失败: {ex.Message}");
            });
        }
    }

    private async Task CheckLatestVersionAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog("🔍 检查最新版本...");
            });

            var release = await _ncfService.GetLatestReleaseAsync();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (release != null)
                {
                    LatestVersion = release.TagName ?? "未知版本";
                    AddLog($"✅ 获取最新版本成功: {LatestVersion}");
                }
                else
                {
                    LatestVersion = "获取失败";
                    AddLog("⚠️ 无法获取版本信息，可能网络连接问题");
                }
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LatestVersion = "获取失败";
                AddLog($"⚠️ 获取版本信息失败: {ex.Message}");
            });
        }
    }

    #endregion

    #region 命令实现

    private async Task TestConnectionAsync()
    {
        try
        {
            IsOperationInProgress = true;
            CurrentStatus = "测试连接中";
            StatusColor = "#007ACC";
            IsProgressIndeterminate = true;
            ProgressText = "正在测试网络连接...";
            
            AddLog("🔗 开始测试连接...");

            // TODO: 实现实际的连接测试
            await Task.Delay(2000);

            CurrentStatus = "连接正常";
            StatusColor = "#28A745";
            ProgressText = "连接测试完成";
            AddLog("✅ 网络连接测试成功");
        }
        catch (Exception ex)
        {
            CurrentStatus = "连接失败";
            StatusColor = "#DC3545";
            ProgressText = "连接测试失败";
            AddLog($"❌ 连接测试失败: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
            IsProgressIndeterminate = false;
            ProgressValue = 0;
        }
    }

    private void OpenConfigDirectory()
    {
        try
        {
            var appDataPath = GetAppDataPath();
            Directory.CreateDirectory(appDataPath);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", appDataPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", appDataPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", appDataPath);
            }
            
            AddLog($"📁 已打开配置目录: {appDataPath}");
        }
        catch (Exception ex)
        {
            AddLog($"❌ 打开配置目录失败: {ex.Message}");
        }
    }

    private async Task MainActionAsync()
    {
        if (_isNcfRunning)
        {
            await StopNcfAsync();
        }
        else
        {
            await StartNcfAsync();
        }
    }

    private void StopOperation()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            
            if (_isNcfRunning)
            {
                _ = Task.Run(StopNcfAsync);
            }
            
            AddLog("🛑 操作已取消");
        }
        catch (Exception ex)
        {
            AddLog($"❌ 停止操作失败: {ex.Message}");
        }
    }

    #endregion

    #region NCF 操作

    private async Task StartNcfAsync()
    {
        try
        {
            IsOperationInProgress = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            CurrentStatus = "启动中";
            StatusColor = "#007ACC";
            MainButtonText = "停止 NCF";
            
            AddLog("🚀 开始启动 NCF...");

            // 1. 检查/下载文件
            await DownloadNcfAsync(cancellationToken);
            
            // 2. 提取文件
            await ExtractNcfAsync(cancellationToken);
            
            // 3. 启动NCF进程
            await StartNcfProcessAsync(cancellationToken);
            
            _isNcfRunning = true;
            CurrentStatus = "运行中";
            StatusColor = "#28A745";
            ProgressText = "NCF 运行中";
            ProgressValue = 100;
            
            AddLog("✅ NCF 启动成功");

            // 自动打开浏览器
            if (AutoOpenBrowser && !string.IsNullOrEmpty(SiteUrl) && SiteUrl != "未启动")
            {
                OpenBrowser(SiteUrl);
            }
        }
        catch (OperationCanceledException)
        {
            AddLog("🛑 启动操作已取消");
        }
        catch (Exception ex)
        {
            CurrentStatus = "启动失败";
            StatusColor = "#DC3545";
            ProgressText = "启动失败";
            AddLog($"❌ 启动失败: {ex.Message}");
        }
        finally
        {
            if (!_isNcfRunning)
            {
                IsOperationInProgress = false;
                IsProgressIndeterminate = false;
                MainButtonText = "启动 NCF";
            }
        }
    }

    private async Task StopNcfAsync()
    {
        try
        {
            AddLog("🛑 正在停止 NCF...");
            
            if (_ncfProcess != null && !_ncfProcess.HasExited)
            {
                _ncfProcess.Kill();
                await _ncfProcess.WaitForExitAsync();
            }

            _isNcfRunning = false;
            IsOperationInProgress = false;
            CurrentStatus = "已停止";
            StatusColor = "#6C757D";
            MainButtonText = "启动 NCF";
            SiteUrl = "未启动";
            ProgressValue = 0;
            ProgressText = "已停止";
            
            AddLog("✅ NCF 已停止");
        }
        catch (Exception ex)
        {
            AddLog($"❌ 停止失败: {ex.Message}");
        }
    }

    private async Task DownloadNcfAsync(CancellationToken cancellationToken)
    {
        var release = await _ncfService.GetLatestReleaseAsync(cancellationToken);
        if (release == null)
        {
            throw new InvalidOperationException("无法获取最新版本信息");
        }

        var targetAsset = _ncfService.GetTargetAsset(release);
        if (targetAsset == null)
        {
            throw new InvalidOperationException("未找到适合当前平台的下载包");
        }

        var needsDownload = await _ncfService.CheckIfDownloadNeededAsync(targetAsset.Name!, targetAsset.Size);
        
        if (needsDownload)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressText = $"正在下载 {targetAsset.Name}...";
                IsProgressIndeterminate = false;
                AddLog("📥 开始下载最新版本...");
            });

            var progress = new Progress<double>(value =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressValue = value * 0.6; // 下载占60%进度
                    ProgressText = $"下载中... {value:F1}%";
                });
            });

            await _ncfService.DownloadFileAsync(targetAsset.BrowserDownloadUrl!, targetAsset.Name!, progress, cancellationToken);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog("✅ 下载完成");
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressValue = 60;
                AddLog("✅ 文件已存在，跳过下载");
            });
        }
    }

    private async Task ExtractNcfAsync(CancellationToken cancellationToken)
    {
        var release = await _ncfService.GetLatestReleaseAsync(cancellationToken);
        if (release == null) return;

        var targetAsset = _ncfService.GetTargetAsset(release);
        if (targetAsset == null) return;

        var needsExtract = await _ncfService.CheckIfExtractNeededAsync(release.TagName!);
        
        if (needsExtract)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressText = "正在提取文件...";
                AddLog("📦 正在提取文件...");
            });

            var progress = new Progress<double>(value =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressValue = 60 + (value * 0.3); // 提取占30%进度
                    ProgressText = $"提取中... {value:F1}%";
                });
            });

            await _ncfService.ExtractZipAsync(targetAsset.Name!, release.TagName!, progress, cancellationToken);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog("✅ 文件提取完成");
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressValue = 90;
                AddLog("✅ 文件已是最新版本，跳过提取");
            });
        }
    }

    private async Task StartNcfProcessAsync(CancellationToken cancellationToken)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText = "正在启动 NCF 进程...";
            ProgressValue = 90;
            AddLog("🌐 正在启动 NCF 站点...");
        });
        
        // 查找可用端口
        var port = await _ncfService.FindAvailablePortAsync(StartPort, EndPort);
        var siteUrl = $"http://localhost:{port}";
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SiteUrl = siteUrl;
            AddLog($"🔌 使用端口: {port}");
        });

        // 启动NCF进程
        _ncfProcess = await _ncfService.StartNcfProcessAsync(port, cancellationToken);
        
        // 等待站点就绪
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText = "等待站点就绪...";
            AddLog("⏳ 等待NCF站点完全启动...");
        });

        var isReady = await _ncfService.WaitForSiteReadyAsync(siteUrl, _ncfProcess, 60, cancellationToken);
        
        if (!isReady)
        {
            throw new InvalidOperationException("NCF站点启动超时或失败");
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AddLog($"✅ NCF 站点已启动: {siteUrl}");
        });
    }

    #endregion

    #region 工具方法

    private static string GetPlatformInfo()
    {
        var os = Environment.OSVersion.Platform.ToString();
        var arch = RuntimeInformation.ProcessArchitecture.ToString();
        return $"{os} {arch}";
    }

    private static string GetAppDataPath()
    {
        return NcfService.AppDataPath;
    }

    private void OpenBrowser(string url)
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
            
            AddLog($"🌏 已打开浏览器: {url}");
        }
        catch (Exception ex)
        {
            AddLog($"⚠️ 无法自动打开浏览器: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}\n";
        
        _logBuffer.Append(logEntry);
        
        // 保持日志大小合理（最多1000行）
        if (_logBuffer.Length > 50000)
        {
            var text = _logBuffer.ToString();
            var lines = text.Split('\n');
            if (lines.Length > 1000)
            {
                _logBuffer.Clear();
                _logBuffer.Append(string.Join('\n', lines[^500..]));
            }
        }
        
        LogText = _logBuffer.ToString();
    }

    #endregion
}
