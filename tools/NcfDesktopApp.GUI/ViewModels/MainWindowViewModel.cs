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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NcfDesktopApp.GUI.Services;
using System.Linq;

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
    private int _startPort = 5000;

    [ObservableProperty]
    private int _endPort = 5300;

    [ObservableProperty]
    private string _mainButtonText = "启动 NCF";

    [ObservableProperty]
    private bool _isOperationInProgress = false;
    
    // 新增浏览器相关属性
    [ObservableProperty]
    private bool _isBrowserReady = false;
    
    [ObservableProperty]
    private bool _hasBrowserError = false;
    
    [ObservableProperty]
    private string _browserErrorMessage = "";
    
    [ObservableProperty]
    private bool _isInitializing = true;
    
    [ObservableProperty]
    private int _currentTabIndex = 0; // 0=设置页面, 1=浏览器页面
    
    // 控制浏览器标签页的可见性
    [ObservableProperty]
    private bool _isBrowserTabVisible = false;

    public object? BrowserViewReference { get; set; }

    #endregion

    #region 私有字段
    
    private readonly NcfService _ncfService;
    private readonly WebView2Service _webView2Service;
    private readonly StringBuilder _logBuffer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Process? _ncfProcess;
    private bool _isNcfRunning = false;

    #endregion

    #region 构造函数

    public MainWindowViewModel()
    {
        var httpClient = new HttpClient();
        _ncfService = new NcfService(httpClient);
        _webView2Service = new WebView2Service(httpClient);
        _logBuffer = new StringBuilder();
        
        // 🆕 注册配置文件冲突处理回调
        _ncfService.OnAppSettingsConflict = HandleAppSettingsConflictAsync;
        
        // 初始化应用程序
        _ = Task.Run(InitializeApplicationAsync);
    }

    #endregion

    #region 命令

    [RelayCommand]
    private async Task TestConnection()
    {
        try
        {
            AddLog("🔍 测试网络连接...");
            var isConnected = await _ncfService.TestConnectionAsync();
            
            if (isConnected)
            {
                AddLog("✅ 网络连接正常");
            }
            else
            {
                AddLog("❌ 网络连接失败，请检查网络设置");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ 连接测试失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenConfigDirectory()
    {
        try
        {
            var path = GetAppDataPath();
            OpenBrowser(path);
            AddLog($"📁 已打开配置目录: {path}");
        }
        catch (Exception ex)
        {
            AddLog($"❌ 无法打开配置目录: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteMainOperation))]
    private async Task MainOperation()
    {
        if (_isNcfRunning)
        {
            StopOperation();
        }
        else
        {
            await StartNcfAsync();
        }
    }

    private bool CanExecuteMainOperation() => !IsOperationInProgress;

    [RelayCommand]
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
    
    // 新增页面切换命令
    [RelayCommand(CanExecute = nameof(CanSwitchToBrowser))]
    private void SwitchToBrowser()
    {
        CurrentTabIndex = 1;
        AddLog("🌐 切换到浏览器页面");
    }
    
    private bool CanSwitchToBrowser() => IsBrowserReady;
    
    [RelayCommand]
    private void SwitchToSettings()
    {
        CurrentTabIndex = 0;
        AddLog("⚙️ 切换到设置页面");
    }
    
    [RelayCommand]
    private async Task RetryBrowser()
    {
        HasBrowserError = false;
        BrowserErrorMessage = "";
        await InitializeBrowserAsync();
    }
    
    [RelayCommand(CanExecute = nameof(CanOpenInExternalBrowser))]
    private void OpenInExternalBrowser()
    {
        if (!string.IsNullOrEmpty(SiteUrl) && SiteUrl != "未启动")
        {
            OpenBrowser(SiteUrl);
        }
    }
    
    private bool CanOpenInExternalBrowser() => !string.IsNullOrEmpty(SiteUrl) && SiteUrl != "未启动";
    
    [RelayCommand(CanExecute = nameof(CanCloseBrowserTab))]
    private async Task CloseBrowserTab()
    {
        try
        {
            // 显示确认对话框
            var result = await ShowConfirmDialogAsync(
                "确认关闭",
                "关闭标签页将停止 NCF 应用程序，\n是否继续？",
                "关闭",
                "取消"
            );
            
            if (!result)
            {
                AddLog("ℹ️ 取消关闭标签页");
                return;
            }
            
            AddLog("🗙 关闭浏览器标签页...");
            
            // 关闭浏览器标签页
            IsBrowserTabVisible = false;
            CurrentTabIndex = 0; // 切换回设置页面
            
            // 停止NCF进程
            if (_isNcfRunning)
            {
                await StopNcfAsync();
            }
            
            AddLog("✅ 浏览器标签页已关闭");
        }
        catch (Exception ex)
        {
            AddLog($"❌ 关闭浏览器标签页失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 显示确认对话框
    /// </summary>
    private async Task<bool> ShowConfirmDialogAsync(string title, string message, string okButtonText = "确定", string cancelButtonText = "取消")
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                var okButton = new Button
                {
                    Content = okButtonText,
                    Width = 100,
                    Height = 35,
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                
                var cancelButton = new Button
                {
                    Content = cancelButtonText,
                    Width = 100,
                    Height = 35,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                
                var dialog = new Window
                {
                    Title = title,
                    Width = 500,
                    MinHeight = 200,
                    MaxHeight = 600,
                    SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    ShowInTaskbar = false,
                    Content = new ScrollViewer
                    {
                        MaxHeight = 550,
                        Content = new StackPanel
                        {
                            Margin = new Thickness(20),
                            Spacing = 20,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = message,
                                    FontSize = 14,
                                    TextWrapping = TextWrapping.Wrap,
                                    TextAlignment = TextAlignment.Left,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Top
                                },
                                new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Spacing = 15,
                                    Margin = new Thickness(0, 10, 0, 0),
                                    Children = { okButton, cancelButton }
                                }
                            }
                        }
                    }
                };
                
                okButton.Click += (s, e) => dialog.Close(true);
                cancelButton.Click += (s, e) => dialog.Close(false);
                
                var result = await dialog.ShowDialog<bool>(mainWindow);
                return result;
            }
        }
        
        // 如果无法显示对话框，默认返回 false（不关闭）
        return false;
    }
    
    private bool CanCloseBrowserTab() => IsBrowserTabVisible;

    #endregion

    #region 初始化方法

    private async Task InitializeApplicationAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog("🚀 正在初始化 NCF 桌面应用程序...");
                IsInitializing = true;
            });

            // 检查最新版本
            await CheckLatestVersionAsync();
            
            // 立即关闭初始化遮罩，让用户看到 WebView2 安装日志
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsInitializing = false;
            });
            
            // 初始化浏览器
            await InitializeBrowserAsync();
            
            // 完成初始化
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog("✅ 应用程序初始化完成");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsInitializing = false;
                AddLog($"❌ 初始化失败: {ex.Message}");
            });
        }
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog("🌐 正在初始化内置浏览器...");
            });
            
            // 仅在 Windows 上检查和安装 WebView2
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("🔍 检查 WebView2 Runtime...");
                });
                
                // 检查并安装 WebView2
                var progress = new Progress<(string message, double percentage)>(update =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        AddLog($"   {update.message}");
                        if (update.percentage >= 0)
                        {
                            ProgressValue = update.percentage;
                            IsProgressIndeterminate = false;
                        }
                        else
                        {
                            IsProgressIndeterminate = true;
                        }
                    });
                });
                
                var installed = await _webView2Service.EnsureWebView2InstalledAsync(progress);
                
                if (!installed)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        AddLog("⚠️ WebView2 Runtime 安装失败");
                        AddLog("   内置浏览器可能无法正常工作");
                        AddLog("   请访问 https://go.microsoft.com/fwlink/p/?LinkId=2124703 手动下载安装");
                        HasBrowserError = true;
                        BrowserErrorMessage = "WebView2 Runtime 安装失败\n\n" +
                                             "内置浏览器需要 Microsoft Edge WebView2 Runtime 才能运行。\n" +
                                             "您可以手动下载并安装：\n" +
                                             "https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" +
                                             "或者使用外部浏览器打开 NCF 应用。";
                    });
                    
                    // 即使失败也标记为就绪，让用户可以使用外部浏览器
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        OnBrowserReady();
                    });
                    return;
                }
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("✅ WebView2 Runtime 已就绪");
                    ProgressValue = 0;
                    IsProgressIndeterminate = false;
                });
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("ℹ️ 非 Windows 平台，使用系统 WebView");
                });
            }
            
            // 等待浏览器组件初始化
            await Task.Delay(500);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnBrowserReady();
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnBrowserError($"浏览器初始化失败: {ex.Message}");
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

            var latestVersion = await _ncfService.GetLatestVersionAsync();
            var installedVersion = await _ncfService.GetInstalledVersionAsync();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LatestVersion = latestVersion;
                AddLog($"📋 最新版本: {latestVersion}");
                
                if (!string.IsNullOrEmpty(installedVersion))
                {
                    AddLog($"💾 当前已安装版本: {installedVersion}");
                    
                    // 比较版本
                    if (installedVersion != latestVersion)
                    {
                        AddLog($"🆕 发现新版本可用！");
                    }
                    else
                    {
                        AddLog($"✅ 当前已是最新版本");
                    }
                }
                else
                {
                    AddLog($"ℹ️ 未检测到已安装的 NeuCharFramework");
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

            // 检查版本更新
            var (shouldContinue, shouldUpdate) = await CheckAndConfirmUpdateAsync();
            if (!shouldContinue)
            {
                // 用户取消启动，恢复状态
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsOperationInProgress = false;
                    CurrentStatus = "已取消";
                    StatusColor = "#6C757D";
                    MainButtonText = "启动 NCF";
                    AddLog("ℹ️ 用户取消了启动操作");
                });
                return;
            }

            // 1-2. 如果需要更新，则下载和提取文件
            if (shouldUpdate)
            {
                // 1. 检查/下载文件
                await DownloadNcfAsync(cancellationToken);
                
                // 2. 提取文件
                await ExtractNcfAsync(cancellationToken);
            }
            else
            {
                AddLog("⏭️ 跳过下载和提取，使用现有版本");
            }
            
            // 3. 启动NCF进程
            await StartNcfProcessAsync(cancellationToken);
            
            _isNcfRunning = true;
            CurrentStatus = "运行中";
            StatusColor = "#28A745";
            ProgressText = "NCF 运行中";
            ProgressValue = 100;
            
            AddLog("✅ NCF 启动成功");
            
            // 显示浏览器标签页
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBrowserTabVisible = true;
            });

            // 自动在内置浏览器中打开
            if (AutoOpenBrowser && !string.IsNullOrEmpty(SiteUrl) && SiteUrl != "未启动")
            {
                await NavigateToBrowserAsync(SiteUrl);
            }
        }
        catch (OperationCanceledException)
        {
            AddLog("🛑 操作已取消");
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStatus = "错误";
                StatusColor = "#DC3545";
                AddLog($"❌ 启动失败: {ex.Message}");
            });
        }
        finally
        {
            IsOperationInProgress = false;
            if (!_isNcfRunning)
            {
                MainButtonText = "启动 NCF";
                CurrentStatus = "就绪";
                StatusColor = "#28A745";
            }
        }
    }

    private async Task DownloadNcfAsync(CancellationToken cancellationToken)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText = "检查本地文件...";
            IsProgressIndeterminate = true;
        });

        var progress = new Progress<(string message, double percentage)>(p =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressText = p.message;
                ProgressValue = p.percentage;
                IsProgressIndeterminate = p.percentage < 0;
            });
        });

        await _ncfService.DownloadLatestReleaseAsync(progress, ShowDetailedInfo, cancellationToken);
    }

    private async Task ExtractNcfAsync(CancellationToken cancellationToken)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText = "提取文件...";
            IsProgressIndeterminate = true;
        });

        var progress = new Progress<(string message, double percentage)>(p =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressText = p.message;
                ProgressValue = p.percentage;
                IsProgressIndeterminate = p.percentage < 0;
                
                if (ShowDetailedInfo)
                {
                    AddLog(p.message);
                }
            });
        });

        await _ncfService.ExtractFilesAsync(progress, cancellationToken);
        
        if (AutoCleanDownloads)
        {
            await _ncfService.CleanupDownloadsAsync();
            AddLog("🧹 已清理下载文件");
        }
    }

    private async Task StartNcfProcessAsync(CancellationToken cancellationToken)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText = "启动 NCF 进程...";
            IsProgressIndeterminate = true;
        });

        var availablePort = await _ncfService.FindAvailablePortAsync(StartPort, EndPort);
        var siteUrl = $"http://localhost:{availablePort}";
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SiteUrl = siteUrl;
            AddLog($"🌐 使用端口: {availablePort}");
            ProgressText = "启动进程...";
        });

        _ncfProcess = await _ncfService.StartNcfProcessAsync(availablePort, cancellationToken);
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AddLog($"🚀 NCF 进程已启动 (PID: {_ncfProcess.Id})");
            ProgressText = "等待站点就绪...";
        });

        // 等待站点就绪
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

    private async Task StopNcfAsync()
    {
        try
        {
            if (_ncfProcess != null && !_ncfProcess.HasExited)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("🛑 正在停止 NCF 进程...");
                });

                // 在 Windows 上，使用 taskkill 杀死整个进程树
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        var killProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = $"/PID {_ncfProcess.Id} /T /F",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        });
                        
                        if (killProcess != null)
                        {
                            await killProcess.WaitForExitAsync();
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                AddLog($"🔪 已使用 taskkill 终止进程树 (PID: {_ncfProcess.Id})");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            AddLog($"⚠️ taskkill 失败，尝试常规 Kill: {ex.Message}");
                        });
                        _ncfProcess.Kill();
                    }
                }
                else
                {
                    // macOS/Linux 使用常规 Kill
                    _ncfProcess.Kill(entireProcessTree: true);
                }
                
                // 等待进程退出，最多等待 5 秒
                var exitTask = _ncfProcess.WaitForExitAsync();
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(exitTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        AddLog("⚠️ 进程未在 5 秒内退出，强制终止");
                    });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        AddLog("✅ NCF 进程已停止");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog($"⚠️ 停止进程时出错: {ex.Message}");
            });
        }
        finally
        {
            _ncfProcess?.Dispose();
            _ncfProcess = null;
            _isNcfRunning = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainButtonText = "启动 NCF";
                CurrentStatus = "已停止";
                StatusColor = "#6C757D";
                SiteUrl = "未启动";
                ProgressText = "已停止";
                ProgressValue = 0;
                IsBrowserTabVisible = false; // 隐藏浏览器标签页
                CurrentTabIndex = 0; // 切换回设置页面
            });
        }
    }

    #endregion

    #region 浏览器控制方法

    public void OnBrowserReady()
    {
        IsBrowserReady = true;
        HasBrowserError = false;
        AddLog("✅ 内置浏览器已准备就绪");
    }

    public void OnBrowserError(string errorMessage)
    {
        HasBrowserError = true;
        BrowserErrorMessage = errorMessage;
        IsBrowserReady = false;
        AddLog($"❌ 浏览器错误: {errorMessage}");
    }

    public void OnNavigationStarted(string url)
    {
        AddLog($"🌐 开始加载: {url}");
    }

    public void OnNavigationCompleted(string url)
    {
        AddLog($"✅ 加载完成: {url}");
    }

    private async Task NavigateToBrowserAsync(string url)
    {
        try
        {
            // 直接切换到浏览器标签页，内置WebView会自动更新URL
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentTabIndex = 1; // 切换到浏览器标签页
                AddLog($"🌐 在内置浏览器中显示: {url}");
            });
            
            // 如果BrowserView可用，尝试导航
            if (BrowserViewReference is NcfDesktopApp.GUI.Views.BrowserView browserView)
            {
                await browserView.NavigateToUrl(url);
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ 浏览器导航失败: {ex.Message}");
        }
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
            
            AddLog($"🌏 已在外部浏览器中打开: {url}");
        }
        catch (Exception ex)
        {
            AddLog($"⚠️ 无法自动打开浏览器: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理 appsettings 配置文件冲突
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="oldContent">旧文件内容</param>
    /// <param name="newContent">新文件内容</param>
    /// <returns>true=使用旧配置覆盖，false=保留新配置</returns>
    private async Task<bool> HandleAppSettingsConflictAsync(string fileName, string oldContent, string newContent)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AddLog($"⚠️ 配置文件冲突: {fileName}");
            AddLog($"   需要用户决策...");
        });
        
        var message = $"检测到配置文件冲突：\n\n" +
                     $"文件名: {fileName}\n\n" +
                     $"旧配置大小: {oldContent.Length} 字符\n" +
                     $"新配置大小: {newContent.Length} 字符\n\n" +
                     $"选择\"使用旧配置\"将保留您的自定义设置\n" +
                     $"选择\"使用新配置\"将使用新版本的默认设置\n\n" +
                     $"注意：\n" +
                     $"• 使用旧配置：新版本配置将备份为 {fileName}.backup-[日期].json\n" +
                     $"• 使用新配置：旧配置将另存为 {fileName}.old-[日期].json";
        
        var result = await ShowConfirmDialogAsync(
            "配置文件冲突",
            message,
            "使用旧配置",
            "使用新配置"
        );
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (result)
            {
                AddLog($"✅ 用户选择：使用旧配置覆盖");
            }
            else
            {
                AddLog($"✅ 用户选择：保留新配置");
            }
        });
        
        return result;
    }
    
    /// <summary>
    /// 检查版本更新并确认
    /// </summary>
    /// <returns>(shouldContinue, shouldUpdate): shouldContinue=是否继续启动, shouldUpdate=是否需要更新</returns>
    private async Task<(bool shouldContinue, bool shouldUpdate)> CheckAndConfirmUpdateAsync()
    {
        try
        {
            // 获取当前已安装版本
            var installedVersion = await _ncfService.GetInstalledVersionAsync();
            
            // 如果没有安装过，直接继续（首次安装）
            if (string.IsNullOrEmpty(installedVersion))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("ℹ️ 首次安装，将下载最新版本");
                });
                return (true, true); // 继续且需要下载
            }
            
            // 获取最新版本
            var latestVersion = await _ncfService.GetLatestVersionAsync();
            
            // 如果版本相同，直接继续
            if (installedVersion == latestVersion)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog($"✅ 当前版本 {installedVersion} 已是最新版本");
                });
                return (true, false); // 继续但不需要下载
            }
            
            // 发现新版本，显示确认对话框
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog($"🆕 发现新版本可用");
                AddLog($"   当前版本: {installedVersion}");
                AddLog($"   最新版本: {latestVersion}");
            });
            
            var message = $"检测到 NeuCharFramework 有新版本可用：\n\n" +
                         $"当前版本: {installedVersion}\n" +
                         $"最新版本: {latestVersion}\n\n" +
                         $"是否更新到最新版本？\n\n" +
                         $"注意：\n" +
                         $"• 更新将保留您的数据库和配置文件\n" +
                         $"• 选择\"继续使用当前版本\"将跳过更新";
            
            var result = await ShowConfirmDialogAsync(
                "版本更新提示",
                message,
                "更新",
                "继续使用当前版本"
            );
            
            if (result)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("✅ 用户选择更新到最新版本");
                });
                return (true, true); // 继续且需要下载
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("ℹ️ 用户选择继续使用当前版本");
                });
                return (true, false); // 继续但不下载
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddLog($"⚠️ 版本检查失败: {ex.Message}");
                AddLog($"   将继续使用当前版本");
            });
            // 出错时继续，但不下载
            return (true, false);
        }
    }
    
    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        
        _logBuffer.AppendLine(logEntry);
        
        // 限制日志大小，保留最后1000行
        var lines = _logBuffer.ToString().Split('\n');
        if (lines.Length > 1000)
        {
            _logBuffer.Clear();
            _logBuffer.AppendLine(string.Join('\n', lines.Skip(lines.Length - 1000)));
        }
        
        LogText = _logBuffer.ToString();
    }

    #endregion
}

