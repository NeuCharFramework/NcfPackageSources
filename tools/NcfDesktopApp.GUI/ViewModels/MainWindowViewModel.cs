/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MainWindowViewModel.cs
    文件功能描述：桌面应用主窗口状态与命令逻辑
    
    
    创建标识：Senparc - 20250718
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强更新源选择、下载反馈与桌面窗口兼容性

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;
using NcfDesktopApp.GUI.Views;
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

    /// <summary>备用更新源站点根地址，持久化至 AppData/desktop-user-settings.json</summary>
    [ObservableProperty]
    private string _mirrorServerBaseUrl = DesktopUserSettings.DefaultMirrorServerBaseUrl;

    [ObservableProperty]
    private NcfLaunchTargetKind _launchTargetKind = NcfLaunchTargetKind.ManagedPublished;

    [ObservableProperty]
    private string _externalNcfPath = string.Empty;

    [ObservableProperty]
    private string _aspNetCoreEnvironment = "Production";

    [ObservableProperty]
    private string? _selectedRecentNcfPath;

    [ObservableProperty]
    private string _targetKindText = "内置托管版本";

    [ObservableProperty]
    private string _targetVersionText = "等待检测";

    [ObservableProperty]
    private string _targetFrameworkText = "等待检测";

    [ObservableProperty]
    private string _targetEntryText = "等待检测";

    [ObservableProperty]
    private string _targetValidationMessage = "桌面端将管理此 Runtime 的安装与更新。";

    [ObservableProperty]
    private string _targetStatusColor = "#6C757D";

    [ObservableProperty]
    private string _mainButtonText = "启动目标";

    [ObservableProperty]
    private bool _isOperationInProgress = false;

    [ObservableProperty]
    private string _desktopBridgeStatusText = "等待 NCF 启动";

    [ObservableProperty]
    private string _desktopBridgeStatusColor = "#6C757D";

    [ObservableProperty]
    private string _desktopBridgeNoticeMessage = "";

    [ObservableProperty]
    private bool _isDesktopBridgeNoticeVisible;

    [ObservableProperty]
    private bool _isDesktopBridgeInstallActionVisible;

    public Action? ShowDesktopRobotRequested { get; set; }

    public DesktopRobotViewModel Robot { get; } = new();

    public ObservableCollection<string> RecentNcfPaths { get; } = new();

    public IReadOnlyList<string> EnvironmentOptions { get; } = new[] { "Production", "Development" };

    public string ManagedRuntimePath => NcfService.NcfRuntimePath;

    public bool IsManagedTargetMode => LaunchTargetKind == NcfLaunchTargetKind.ManagedPublished;

    public bool IsExternalTargetMode => !IsManagedTargetMode;

    public bool IsTargetSelectionEnabled => !IsOperationInProgress && !_isNcfRunning;

    public string ManagedModeButtonBackground => IsManagedTargetMode ? "#2563EB" : "Transparent";

    public string ManagedModeButtonForeground => IsManagedTargetMode ? "White" : "#6C757D";

    public string ExternalModeButtonBackground => IsExternalTargetMode ? "#7C3AED" : "Transparent";

    public string ExternalModeButtonForeground => IsExternalTargetMode ? "White" : "#6C757D";
    
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
    
    // 控制浏览器标签页的可见性（标签按钮）
    [ObservableProperty]
    private bool _isBrowserTabVisible = false;
    
    // 🔧 方案1最终版：控制各标签页内容的显示（Grid 叠加层方案）
    // 设置页面是否可见
    public bool IsSettingsTabActive => CurrentTabIndex == 0;
    
    // 浏览器页面是否可见（且已就绪且标签可见）
    public bool IsBrowserTabActive => CurrentTabIndex == 1 && IsBrowserTabVisible;
    
    // 按钮背景色（表示选中状态）
    public string SettingsTabButtonBackground => CurrentTabIndex == 0 ? "#007ACC" : "Transparent";
    public string BrowserTabButtonBackground => CurrentTabIndex == 1 ? "#007ACC" : "Transparent";
    
    // 按钮前景色（表示选中状态）
    public string SettingsTabButtonForeground => CurrentTabIndex == 0 ? "White" : "Black";
    public string BrowserTabButtonForeground => CurrentTabIndex == 1 ? "White" : "Black";

    public object? BrowserViewReference { get; set; }

    #endregion
    
    #region 属性变更通知
    
    // 🔧 当 CurrentTabIndex 变化时，通知依赖属性
    partial void OnCurrentTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsSettingsTabActive));
        OnPropertyChanged(nameof(IsBrowserTabActive));
        OnPropertyChanged(nameof(SettingsTabButtonBackground));
        OnPropertyChanged(nameof(BrowserTabButtonBackground));
        OnPropertyChanged(nameof(SettingsTabButtonForeground));
        OnPropertyChanged(nameof(BrowserTabButtonForeground));
    }
    
    // 🔧 当 IsBrowserTabVisible 变化时，通知 IsBrowserTabActive
    partial void OnIsBrowserTabVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBrowserTabActive));
    }
    
    // 🔧 当 IsBrowserReady 变化时，通知命令刷新
    partial void OnIsBrowserReadyChanged(bool value)
    {
        SwitchToBrowserCommand.NotifyCanExecuteChanged();
    }
    
    // 🔧 当 SiteUrl 变化时，通知外部浏览器打开命令刷新
    partial void OnSiteUrlChanged(string value)
    {
        OpenInExternalBrowserCommand.NotifyCanExecuteChanged();
    }

    partial void OnMirrorServerBaseUrlChanged(string value)
    {
        if (_suppressMirrorSettingsSave)
        {
            return;
        }

        var normalized = DesktopSettingsStore.NormalizeMirrorServerBase(value);
        _ncfService.MirrorServerBaseUrl = normalized;
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            _suppressMirrorSettingsSave = true;
            MirrorServerBaseUrl = normalized;
            _suppressMirrorSettingsSave = false;
        }

        SaveDesktopSettings();
    }

    partial void OnLaunchTargetKindChanged(NcfLaunchTargetKind value)
    {
        OnPropertyChanged(nameof(IsManagedTargetMode));
        OnPropertyChanged(nameof(IsExternalTargetMode));
        OnPropertyChanged(nameof(ManagedModeButtonBackground));
        OnPropertyChanged(nameof(ManagedModeButtonForeground));
        OnPropertyChanged(nameof(ExternalModeButtonBackground));
        OnPropertyChanged(nameof(ExternalModeButtonForeground));

        if (_suppressDesktopSettingsSave)
        {
            return;
        }

        RefreshSelectedLaunchTarget();
        SaveDesktopSettings();
    }

    partial void OnExternalNcfPathChanged(string value)
    {
        if (_suppressDesktopSettingsSave)
        {
            return;
        }

        _resolvedLaunchTarget = null;
        TargetKindText = "等待检测";
        TargetVersionText = "—";
        TargetFrameworkText = "—";
        TargetEntryText = string.IsNullOrWhiteSpace(value) ? "尚未选择外部目标" : value;
        TargetValidationMessage = "路径已变化，请点击“检测目标”确认入口和运行环境。";
        TargetStatusColor = "#D97706";
        SaveDesktopSettings();
    }

    partial void OnAspNetCoreEnvironmentChanged(string value)
    {
        if (!_suppressDesktopSettingsSave)
        {
            SaveDesktopSettings();
        }
    }

    partial void OnIsOperationInProgressChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTargetSelectionEnabled));
        NotifyLaunchTargetCommandsCanExecuteChanged();
    }

    partial void OnSelectedRecentNcfPathChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || _suppressDesktopSettingsSave)
        {
            return;
        }

        // macOS renders ComboBox entries in a native menu. Do not change the
        // bound path or ItemsSource while that menu is still processing the
        // selection event; Avalonia can otherwise re-enter native selection
        // handling and terminate the process with an unhandled exception.
        var selectedPath = value;
        Dispatcher.UIThread.Post(
            () => ApplyRecentNcfPath(selectedPath),
            DispatcherPriority.Background);
    }
    
    #endregion

    #region 私有字段
    
    private readonly NcfService _ncfService;
    private readonly DesktopBridgeClient _desktopBridgeClient;
    private readonly AdminChatClient _adminChatClient;
    private bool _suppressMirrorSettingsSave;
    private bool _suppressDesktopSettingsSave;
    private readonly WebView2Service _webView2Service;
    private readonly StringBuilder _logBuffer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Process? _ncfProcess;
    private string? _desktopBridgeSessionToken;
    private NcfLaunchTarget? _resolvedLaunchTarget;
    private NcfLaunchTarget? _activeLaunchTarget;
    private bool _isNcfRunning = false;
    
    // 🚀 性能优化：批量日志处理
    private readonly Queue<string> _pendingCliLogs = new Queue<string>();
    private readonly System.Timers.Timer _logUpdateTimer;
    private int _currentLineCount = 0;
    private ScrollViewer? _cachedScrollViewer;
    private const int MaxLogLines = 1000;
    private const int LogUpdateIntervalMs = 100;  // 每100ms批量更新一次
    private const int InitialDisplayLines = 200;  // 初始只显示最后200行
    private bool _isApplicationReady = false;  // 应用是否已就绪（启动完成后才显示完整日志）
    private DateTime _lastLogUpdateTime = DateTime.MinValue;  // 上次日志更新时间
    private const int MaxLogUpdateIntervalMs = 500;  // 当日志量大时的最大更新间隔（500ms）

    #endregion

    #region 构造函数

    public MainWindowViewModel()
    {
        var httpClient = new HttpClient(CreateDesktopHttpHandler(), disposeHandler: true);
        _ncfService = new NcfService(httpClient);
        _webView2Service = new WebView2Service(httpClient);
        var bridgeHttpClient = new HttpClient(CreateDesktopHttpHandler(), disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        _desktopBridgeClient = new DesktopBridgeClient(bridgeHttpClient);
        _desktopBridgeClient.AvailabilityChanged += OnDesktopBridgeAvailabilityChanged;
        _desktopBridgeClient.ActivityReceived += OnDesktopActivityReceived;
        _desktopBridgeClient.AuthorizedSyncReceived += OnDesktopAuthorizedSyncReceived;
        _desktopBridgeClient.AuthorizedSyncAuthorizationFailed += OnDesktopAuthorizedSyncAuthorizationFailed;
        _adminChatClient = new AdminChatClient(new HttpClient(CreateDesktopHttpHandler(), disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
        _logBuffer = new StringBuilder();
        _ncfService.OnDownloadLog = AddLog;
        
        // 🚀 初始化日志批量更新定时器（性能优化）
        _logUpdateTimer = new System.Timers.Timer(LogUpdateIntervalMs);
        _logUpdateTimer.Elapsed += OnLogUpdateTimerElapsed;
        _logUpdateTimer.AutoReset = true;
        _logUpdateTimer.Start();
        
        // 🆕 注册配置文件冲突处理回调
        _ncfService.OnAppSettingsConflict = HandleAppSettingsConflictAsync;
        
        // 初始化应用程序
        _ = Task.Run(InitializeApplicationAsync);
    }

    /// <summary>
    /// 本机 HTTPS（如 ASP.NET Core 开发证书）校验失败会导致镜像元数据拉取失败并误回退 GitHub；对回环地址放宽证书校验。
    /// </summary>
    private static HttpMessageHandler CreateDesktopHttpHandler()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = static (request, _, _, sslPolicyErrors) =>
        {
            if (request.RequestUri?.IsLoopback == true)
            {
                return true;
            }

            var host = request.RequestUri?.Host;
            if (!string.IsNullOrEmpty(host) &&
                string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return sslPolicyErrors == SslPolicyErrors.None;
        };
        return handler;
    }

    private void ApplyMirrorUrlFromViewModelToService()
    {
        _ncfService.MirrorServerBaseUrl = DesktopSettingsStore.NormalizeMirrorServerBase(MirrorServerBaseUrl);
    }

    private async Task ApplyMirrorUrlFromViewModelToServiceOnUiAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(ApplyMirrorUrlFromViewModelToService);
    }

    private void SaveDesktopSettings()
    {
        if (_suppressDesktopSettingsSave)
        {
            return;
        }

        DesktopSettingsStore.Save(new DesktopUserSettings
        {
            MirrorServerBaseUrl = MirrorServerBaseUrl,
            LaunchTargetKind = LaunchTargetKind,
            ExternalNcfPath = ExternalNcfPath,
            RecentNcfPaths = RecentNcfPaths.ToList(),
            AspNetCoreEnvironment = AspNetCoreEnvironment
        });
    }

    private void RefreshSelectedLaunchTarget()
    {
        var resolution = IsManagedTargetMode
            ? NcfLaunchTargetResolver.ResolveManagedRuntime(NcfService.NcfRuntimePath)
            : NcfLaunchTargetResolver.ResolveExternal(ExternalNcfPath);

        if (resolution.IsValid)
        {
            ApplyResolvedLaunchTarget(resolution.Target!);
            return;
        }

        _resolvedLaunchTarget = null;
        TargetKindText = IsManagedTargetMode ? "内置托管版本" : "外部工作区";
        TargetVersionText = IsManagedTargetMode ? "待安装" : "—";
        TargetFrameworkText = "—";
        TargetEntryText = IsManagedTargetMode ? NcfService.NcfRuntimePath : ExternalNcfPath;
        TargetValidationMessage = IsManagedTargetMode
            ? "内置 Runtime 尚未安装；首次启动时将下载最新兼容版本。"
            : resolution.ErrorMessage;
        TargetStatusColor = IsManagedTargetMode ? "#D97706" : "#DC3545";
    }

    private void ApplyResolvedLaunchTarget(NcfLaunchTarget target)
    {
        _resolvedLaunchTarget = target;
        TargetKindText = target.KindDisplayName;
        TargetVersionText = target.Version;
        TargetFrameworkText = target.TargetFramework;
        TargetEntryText = target.EntryPath;
        TargetValidationMessage = target.IsManaged
            ? "目标有效。此目录由桌面端负责下载、更新和配置保护。"
            : target.IsSourceProject
                ? "目标有效。将忽略 launchSettings 并使用 dotnet run --no-restore；不会自动还原包，但构建可能更新 bin/obj。"
                : "目标有效。外部发布目录只启动不更新；NCF 进程本身仍可能写入日志、数据库和配置。";
        TargetStatusColor = "#16A34A";

        if (!target.IsManaged)
        {
            LatestVersion = target.Version;
        }

        if (!target.IsManaged)
        {
            _suppressDesktopSettingsSave = true;
            LaunchTargetKind = target.Kind;
            _suppressDesktopSettingsSave = false;
        }
    }

    private void AddRecentNcfPath(string path)
    {
        var existingIndex = RecentNcfPaths
            .Select((item, index) => new { item, index })
            .FirstOrDefault(entry =>
                string.Equals(entry.item, path, StringComparison.OrdinalIgnoreCase))?.index ?? -1;
        if (existingIndex > 0)
        {
            RecentNcfPaths.Move(existingIndex, 0);
        }
        else if (existingIndex < 0)
        {
            RecentNcfPaths.Insert(0, path);
        }

        while (RecentNcfPaths.Count > 8)
        {
            RecentNcfPaths.RemoveAt(RecentNcfPaths.Count - 1);
        }
    }

    private void ApplyRecentNcfPath(string path)
    {
        try
        {
            _suppressDesktopSettingsSave = true;
            ExternalNcfPath = path;
            LaunchTargetKind = NcfLaunchTargetKind.ExternalPublished;
            _suppressDesktopSettingsSave = false;
            ValidateExternalTargetCore(recordRecentPath: false);
        }
        catch (Exception ex)
        {
            _suppressDesktopSettingsSave = false;
            AddLog($"❌ 选择最近工作区失败: {ex.Message}");
        }
    }

    #endregion

    #region 命令

    [RelayCommand]
    private async Task TestConnection()
    {
        try
        {
            await ApplyMirrorUrlFromViewModelToServiceOnUiAsync().ConfigureAwait(true);
            AddLog("🔍 测试网络连接...");
            var isConnected = await _ncfService.TestConnectionAsync();
            
            if (isConnected)
            {
                AddLog("✅ 网络连接正常");
                if (!string.IsNullOrWhiteSpace(_ncfService.LastSourceSelectionSummary))
                {
                    AddLog($"🌐 {_ncfService.LastSourceSelectionSummary}");
                }
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

    [RelayCommand(CanExecute = nameof(CanChangeLaunchTarget))]
    private void UseManagedRuntime()
    {
        LaunchTargetKind = NcfLaunchTargetKind.ManagedPublished;
        RefreshSelectedLaunchTarget();
        AddLog("🧭 已切换到内置托管版本");
    }

    [RelayCommand(CanExecute = nameof(CanChangeLaunchTarget))]
    private async Task UseExternalTarget()
    {
        if (IsManagedTargetMode)
        {
            LaunchTargetKind = NcfLaunchTargetKind.ExternalPublished;
        }

        if (string.IsNullOrWhiteSpace(ExternalNcfPath))
        {
            await SelectExternalNcfTarget();
            return;
        }

        RefreshSelectedLaunchTarget();
        AddLog("🧭 已切换到外部 NCF 工作区");
    }

    [RelayCommand(CanExecute = nameof(CanChangeLaunchTarget))]
    private async Task SelectExternalNcfTarget()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow?.StorageProvider is not { CanPickFolder: true } storageProvider)
            {
                AddLog("❌ 当前平台无法打开目录选择器，请直接粘贴目标路径。");
                return;
            }

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择 NCF 发布目录或源码工作区",
                AllowMultiple = false
            });
            var selectedPath = folders.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            _suppressDesktopSettingsSave = true;
            ExternalNcfPath = selectedPath;
            LaunchTargetKind = NcfLaunchTargetKind.ExternalPublished;
            _suppressDesktopSettingsSave = false;
            ValidateExternalTarget();
        }
        catch (Exception ex)
        {
            AddLog($"❌ 选择外部目标失败: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanChangeLaunchTarget))]
    private void ValidateExternalTarget()
    {
        ValidateExternalTargetCore(recordRecentPath: true);
    }

    private void ValidateExternalTargetCore(bool recordRecentPath)
    {
        var resolution = NcfLaunchTargetResolver.ResolveExternal(ExternalNcfPath);
        if (!resolution.IsValid)
        {
            _resolvedLaunchTarget = null;
            TargetKindText = "外部工作区";
            TargetVersionText = "—";
            TargetFrameworkText = "—";
            TargetEntryText = ExternalNcfPath;
            TargetValidationMessage = resolution.ErrorMessage;
            TargetStatusColor = "#DC3545";
            AddLog($"❌ 外部目标检测失败: {resolution.ErrorMessage}");
            SaveDesktopSettings();
            return;
        }

        var target = resolution.Target!;
        ApplyResolvedLaunchTarget(target);
        if (recordRecentPath)
        {
            var wasSuppressed = _suppressDesktopSettingsSave;
            try
            {
                _suppressDesktopSettingsSave = true;
                ExternalNcfPath = target.SelectedPath;
                SelectedRecentNcfPath = target.SelectedPath;
            }
            finally
            {
                _suppressDesktopSettingsSave = wasSuppressed;
            }

            AddRecentNcfPath(target.SelectedPath);
        }
        SaveDesktopSettings();
        AddLog($"✅ 已识别 {target.KindDisplayName}: {target.EntryPath}");
    }

    [RelayCommand]
    private void OpenSelectedTargetDirectory()
    {
        var path = IsManagedTargetMode
            ? NcfService.NcfRuntimePath
            : _resolvedLaunchTarget?.WorkingDirectory ?? ExternalNcfPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            AddLog("ℹ️ 当前目标目录不存在。");
            return;
        }

        OpenBrowser(path);
        AddLog($"📁 已打开目标目录: {path}");
    }

    private bool CanChangeLaunchTarget() => !IsOperationInProgress && !_isNcfRunning;

    private void NotifyLaunchTargetCommandsCanExecuteChanged()
    {
        OnPropertyChanged(nameof(IsTargetSelectionEnabled));
        UseManagedRuntimeCommand.NotifyCanExecuteChanged();
        UseExternalTargetCommand.NotifyCanExecuteChanged();
        SelectExternalNcfTargetCommand.NotifyCanExecuteChanged();
        ValidateExternalTargetCommand.NotifyCanExecuteChanged();
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

    [RelayCommand]
    private async Task OpenDesktopBridgeInstallPage()
    {
        if (string.IsNullOrWhiteSpace(SiteUrl) || SiteUrl == "未启动")
        {
            AddLog("ℹ️ 请先启动 NCF，再打开 XNCF 模块管理页面安装 DesktopBridge。");
            return;
        }

        var moduleManagerUrl = $"{SiteUrl.TrimEnd('/')}/Admin/XncfModule/Index";
        IsBrowserTabVisible = true;
        await NavigateToBrowserAsync(moduleManagerUrl);
        AddLog("🧩 已打开 XNCF 模块管理，请安装或更新 Senparc.Xncf.DesktopBridge。");
    }

    [RelayCommand]
    private void ShowDesktopRobot()
    {
        ShowDesktopRobotRequested?.Invoke();
    }
    
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

    /// <summary>NCF 站点进程是否处于运行中（主窗口关闭前判断）。</summary>
    public bool IsNcfRunning => _isNcfRunning;

    /// <summary>
    /// 主窗口即将关闭：若 NCF 在运行则弹框确认并停止进程；返回 <c>true</c> 表示可以关闭窗口。
    /// </summary>
    public async Task<bool> TryPrepareShutdownForWindowCloseAsync()
    {
        if (!_isNcfRunning)
        {
            return true;
        }

        var confirm = await ShowConfirmDialogAsync(
            "关闭应用",
            "NCF 正在运行。关闭窗口将停止 NCF 进程。\n是否继续？",
            "停止并关闭",
            "取消"
        ).ConfigureAwait(true);

        if (!confirm)
        {
            return false;
        }

        await StopNcfAsync().ConfigureAwait(true);
        return true;
    }

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

            var desktopSettings = DesktopSettingsStore.Load();
            _ncfService.MirrorServerBaseUrl = DesktopSettingsStore.NormalizeMirrorServerBase(desktopSettings.MirrorServerBaseUrl);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _suppressMirrorSettingsSave = true;
                _suppressDesktopSettingsSave = true;
                MirrorServerBaseUrl = _ncfService.MirrorServerBaseUrl;
                ExternalNcfPath = desktopSettings.ExternalNcfPath ?? string.Empty;
                AspNetCoreEnvironment = string.Equals(
                    desktopSettings.AspNetCoreEnvironment,
                    "Development",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Development"
                    : "Production";
                LaunchTargetKind = desktopSettings.LaunchTargetKind;
                RecentNcfPaths.Clear();
                foreach (var path in desktopSettings.RecentNcfPaths ?? new List<string>())
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        RecentNcfPaths.Add(path);
                    }
                }
                _suppressMirrorSettingsSave = false;
                _suppressDesktopSettingsSave = false;
                RefreshSelectedLaunchTarget();
            });

            SaveDesktopSettings();

            if (IsManagedTargetMode)
            {
                // 只有托管模式参与线上版本比较和自动更新；网络检查放到后台，不能阻塞界面和浏览器初始化。
                _ = Task.Run(CheckLatestVersionAsync);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LatestVersion = _resolvedLaunchTarget?.Version ?? "目标无效";
                    AddLog(_resolvedLaunchTarget == null
                        ? $"⚠️ 外部目标需要重新检测: {TargetValidationMessage}"
                        : $"🧭 已恢复外部目标: {_resolvedLaunchTarget.EntryPath}");
                });
            }
            
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
                // 🚀 标记应用已就绪，现在可以显示完整日志了
                _isApplicationReady = true;
                // 立即刷新一次日志显示
                FlushPendingLogs();
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

            if (IsVersionCheckUnavailable(latestVersion))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LatestVersion = "暂不可用";
                    AddLog("⚠️ 在线版本检查暂不可用，已跳过本次检查，不影响程序继续运行");
                });
                return;
            }
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LatestVersion = latestVersion;
                AddLog($"📋 最新版本: {latestVersion}");
                if (!string.IsNullOrWhiteSpace(_ncfService.LastSourceSelectionSummary))
                {
                    AddLog($"🌐 {_ncfService.LastSourceSelectionSummary}");
                }
                
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
                AddLog($"⚠️ 获取版本信息失败: {GetRoutineUpdateError(ex)}；不影响程序继续运行");
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
            Robot.SetProcessState("启动中", "正在准备 NCF 运行环境");

            CurrentStatus = "启动中";
            StatusColor = "#007ACC";
            MainButtonText = "停止目标";
            
            AddLog("🚀 开始启动 NCF 目标...");

            NcfLaunchTarget launchTarget;
            if (IsManagedTargetMode)
            {
                // 只有内置托管版本允许进入下载、更新和解压链路。
                var (shouldContinue, shouldUpdate) = await CheckAndConfirmUpdateAsync();
                if (!shouldContinue)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsOperationInProgress = false;
                        CurrentStatus = "已取消";
                        StatusColor = "#6C757D";
                        MainButtonText = "启动目标";
                        AddLog("ℹ️ 用户取消了启动操作");
                    });
                    return;
                }

                if (shouldUpdate)
                {
                    try
                    {
                        await DownloadNcfAsync(cancellationToken);
                        await ExtractNcfAsync(cancellationToken);
                    }
                    catch (Exception ex) when (
                        !cancellationToken.IsCancellationRequested &&
                        IsUpdateConnectivityError(ex))
                    {
                        var fallbackResolution = NcfLaunchTargetResolver.ResolveManagedRuntime(NcfService.NcfRuntimePath);
                        if (!fallbackResolution.IsValid)
                        {
                            throw;
                        }

                        AddLog($"⚠️ 在线更新暂不可用（{GetRoutineUpdateError(ex)}），继续使用本地托管版本");
                    }
                }
                else
                {
                    AddLog("⏭️ 跳过下载和提取，使用现有托管版本");
                }

                var managedResolution = NcfLaunchTargetResolver.ResolveManagedRuntime(NcfService.NcfRuntimePath);
                if (!managedResolution.IsValid)
                {
                    throw new InvalidOperationException(managedResolution.ErrorMessage);
                }
                launchTarget = managedResolution.Target!;
                await Dispatcher.UIThread.InvokeAsync(() => ApplyResolvedLaunchTarget(launchTarget));
            }
            else
            {
                var externalResolution = NcfLaunchTargetResolver.ResolveExternal(ExternalNcfPath);
                if (!externalResolution.IsValid)
                {
                    throw new InvalidOperationException(externalResolution.ErrorMessage);
                }

                launchTarget = externalResolution.Target!;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ApplyResolvedLaunchTarget(launchTarget);
                    AddRecentNcfPath(launchTarget.SelectedPath);
                    SaveDesktopSettings();
                });
                AddLog("🛡️ 外部工作区模式：已跳过下载、解压和自动更新");
            }

            _activeLaunchTarget = launchTarget;
            AddLog($"🧭 启动目标: {launchTarget.KindDisplayName} / {launchTarget.Version} / {launchTarget.TargetFramework}");
            await StartNcfProcessAsync(launchTarget, cancellationToken);
            
            _isNcfRunning = true;
            NotifyLaunchTargetCommandsCanExecuteChanged();
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
            Robot.SetProcessState("已停止", "启动操作已取消");
        }
        catch (Exception ex)
        {
            Robot.SetProcessState("错误", ex.Message, isError: true);
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
                MainButtonText = "启动目标";
                CurrentStatus = "就绪";
                StatusColor = "#28A745";
                _activeLaunchTarget = null;
            }
        }
    }

    private async Task DownloadNcfAsync(CancellationToken cancellationToken)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyMirrorUrlFromViewModelToService();
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
        if (!string.IsNullOrWhiteSpace(_ncfService.LastSourceSelectionSummary))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                AddLog($"🌐 {_ncfService.LastSourceSelectionSummary}"));
        }
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

    private async Task StartNcfProcessAsync(NcfLaunchTarget launchTarget, CancellationToken cancellationToken)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText = "启动 NCF 进程...";
            IsProgressIndeterminate = true;
        });

        var availablePort = await _ncfService.FindAvailablePortAsync(StartPort, EndPort);
        var siteUrl = $"http://localhost:{availablePort}";
        _desktopBridgeSessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AddLog($"🌐 使用端口: {availablePort}");
            ProgressText = "启动进程...";
        });

        // 注册 CLI 输出回调
        _ncfService.OnProcessOutput = (output, isError) =>
        {
            AddCliLog(output, isError);
            Robot.ApplyCompatibilityLog(output, isError);
        };

        _ncfProcess = await _ncfService.StartNcfProcessAsync(
            launchTarget,
            availablePort,
            _desktopBridgeSessionToken,
            AspNetCoreEnvironment,
            cancellationToken);
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AddLog($"🚀 NCF 进程已启动 (PID: {_ncfProcess.Id})");
            ProgressText = "等待站点就绪...";
        });

        // 等待站点就绪
        var timeoutSeconds = launchTarget.IsSourceProject ? 180 : 60;
        var isReady = await _ncfService.WaitForSiteReadyAsync(
            siteUrl,
            _ncfProcess,
            timeoutSeconds,
            requireNcfBranding: launchTarget.IsManaged,
            cancellationToken: cancellationToken);
        
        if (!isReady)
        {
            throw new InvalidOperationException("NCF站点启动超时或失败");
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // 站点实际就绪后再更新绑定地址，避免 WebView 把导航完成误报为 NCF 已启动。
            SiteUrl = siteUrl;
            AddLog($"✅ NCF 站点已启动: {siteUrl}");
        });

        Robot.SetProcessState("运行中", $"{launchTarget.DisplayName} 已启动，正在等待 Agent 任务");
        await ConnectDesktopBridgeAsync(siteUrl, _desktopBridgeSessionToken, cancellationToken);
    }

    private async Task ConnectDesktopBridgeAsync(
        string siteUrl,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        try
        {
            await _desktopBridgeClient.ConnectAsync(siteUrl, sessionToken, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            // Bridge 是可选能力，任何异常都只触发兼容模式，绝不阻断 NCF 启动。
            ApplyDesktopBridgeAvailability(new DesktopBridgeProbeResult(
                DesktopBridgeAvailability.Unavailable,
                $"DesktopBridge 初始化失败（{ex.Message}），已安全切换到兼容模式。"));
        }
    }

    private void OnDesktopBridgeAvailabilityChanged(DesktopBridgeProbeResult result)
    {
        ApplyDesktopBridgeAvailability(result);
    }

    private void ApplyDesktopBridgeAvailability(DesktopBridgeProbeResult result)
    {
        Robot.SetBridgeAvailability(result);
        Dispatcher.UIThread.Post(() =>
        {
            DesktopBridgeStatusText = result.Message;
            UpdateAdminChatBridgeState(result);
            DesktopBridgeStatusColor = result.Availability switch
            {
                DesktopBridgeAvailability.Available => "#28A745",
                DesktopBridgeAvailability.NotInstalled => "#FFC107",
                DesktopBridgeAvailability.Incompatible => "#FD7E14",
                DesktopBridgeAvailability.Unauthorized => "#DC3545",
                DesktopBridgeAvailability.Inactive => "#FD7E14",
                _ => "#6C757D"
            };

            IsDesktopBridgeNoticeVisible = result.Availability != DesktopBridgeAvailability.Available;
            IsDesktopBridgeInstallActionVisible = result.Availability is
                DesktopBridgeAvailability.NotInstalled or DesktopBridgeAvailability.Incompatible;
            DesktopBridgeNoticeMessage = result.Availability switch
            {
                DesktopBridgeAvailability.NotInstalled =>
                    "当前内部站点未安装 Senparc.Xncf.DesktopBridge。机器人仍会使用进程和日志兼容模式，不影响 NCF 运行。请进入 XNCF 模块管理安装；若列表中没有该模块，请先升级站点或补充模块包，然后重启站点。",
                DesktopBridgeAvailability.Incompatible =>
                    "DesktopBridge 版本与当前 GUI 不兼容。机器人已安全降级，请在 XNCF 模块管理中更新模块后重启站点。",
                DesktopBridgeAvailability.Unauthorized =>
                    "DesktopBridge 已检测到，但会话认证失败。NCF 会继续运行；请从桌面应用重新启动站点。",
                DesktopBridgeAvailability.Inactive =>
                    "DesktopBridge 已安装但未启用桌面会话。NCF 会继续运行；请从桌面应用重新启动站点。",
                DesktopBridgeAvailability.Unavailable =>
                    "暂时无法连接 DesktopBridge，机器人正在使用兼容模式并会在后台重连。NCF 运行不受影响。",
                _ => string.Empty
            };

            AddLog(result.Availability == DesktopBridgeAvailability.Available
                ? $"🤖 {result.Message}"
                : $"ℹ️ {result.Message}");
        });
    }

    private void OnDesktopActivityReceived(DesktopActivityMessage activity)
    {
        Robot.ApplyActivity(activity);
        if (ShowDetailedInfo)
        {
            AddLog($"🤖 [{activity.Source}] {activity.Title}: {activity.State}");
        }
    }

    private async Task StopNcfAsync()
    {
        try
        {
            await _desktopBridgeClient.StopAsync().ConfigureAwait(false);

            // 清理 CLI 输出回调
            if (_ncfService != null)
            {
                _ncfService.OnProcessOutput = null;
            }
            
            // 🚀 停止定时器前先处理剩余的日志
            FlushPendingLogs();
            
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
            _desktopBridgeSessionToken = null;
            ResetAdminChatState();
            _activeLaunchTarget = null;
            _isNcfRunning = false;
            NotifyLaunchTargetCommandsCanExecuteChanged();
            Robot.SetProcessState("已停止", "NCF 站点已停止");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainButtonText = "启动目标";
                CurrentStatus = "已停止";
                StatusColor = "#6C757D";
                SiteUrl = "未启动";
                ProgressText = "已停止";
                ProgressValue = 0;
                IsBrowserTabVisible = false; // 隐藏浏览器标签页
                CurrentTabIndex = 0; // 切换回设置页面
                DesktopBridgeStatusText = "等待 NCF 启动";
                DesktopBridgeStatusColor = "#6C757D";
                DesktopBridgeNoticeMessage = string.Empty;
                IsDesktopBridgeNoticeVisible = false;
                IsDesktopBridgeInstallActionVisible = false;
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
            await ApplyMirrorUrlFromViewModelToServiceOnUiAsync().ConfigureAwait(true);

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

            if (IsVersionCheckUnavailable(latestVersion))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddLog("⚠️ 在线版本检查暂不可用，继续使用当前托管版本");
                });
                return (true, false);
            }
            
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
                AddLog($"⚠️ 版本检查失败: {GetRoutineUpdateError(ex)}");
                AddLog($"   将继续使用当前版本");
            });
            // 出错时继续，但不下载
            return (true, false);
        }
    }

    private static bool IsVersionCheckUnavailable(string? version) =>
        string.IsNullOrWhiteSpace(version) ||
        string.Equals(version, "获取失败", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(version, "暂不可用", StringComparison.OrdinalIgnoreCase);

    private static bool IsUpdateConnectivityError(Exception exception)
    {
        if (exception is HttpRequestException or TaskCanceledException)
        {
            return true;
        }

        return exception is InvalidOperationException invalidOperation &&
               (invalidOperation.Message.Contains("无法获取最新版本", StringComparison.OrdinalIgnoreCase) ||
                invalidOperation.Message.Contains("未找到适合当前平台", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRoutineUpdateError(Exception exception) => exception switch
    {
        TaskCanceledException => "请求超时",
        HttpRequestException httpException when httpException.StatusCode.HasValue =>
            $"HTTP {(int)httpException.StatusCode.Value}",
        HttpRequestException => "网络不可达",
        InvalidOperationException => "更新源没有可用安装包",
        _ => "请求失败"
    };
    
    /// <summary>
    /// 添加应用日志（高性能版本：批量处理）
    /// </summary>
    private void AddLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        
        // 🚀 性能优化：使用相同的批量更新机制
        lock (_pendingCliLogs)
        {
            _pendingCliLogs.Enqueue(logEntry);
        }
    }

    /// <summary>
    /// 添加 CLI 日志（高性能版本：批量处理）
    /// </summary>
    private void AddCliLog(string message, bool isError)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var prefix = isError ? "[CLI:ERROR]" : "[CLI]";
        var logEntry = $"[{timestamp}] {prefix} {message}";
        
        // 🚀 性能优化：只将日志加入队列，不立即更新 UI
        // 由定时器每 100ms 批量更新，减少 95%+ 的性能开销
        lock (_pendingCliLogs)
        {
            _pendingCliLogs.Enqueue(logEntry);
        }
    }
    
    /// <summary>
    /// 定时器回调：批量更新日志到 UI（每 100ms 一次）
    /// </summary>
    private void OnLogUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        List<string> logsToAdd;
        
        lock (_pendingCliLogs)
        {
            if (_pendingCliLogs.Count == 0) return;
            
            logsToAdd = new List<string>(_pendingCliLogs);
            _pendingCliLogs.Clear();
        }
        
        // 🚀 性能优化：如果应用还未就绪，只累积日志不立即显示
        // 这样可以避免启动时阻塞UI
        if (!_isApplicationReady)
        {
            // 应用启动阶段：只累积日志到缓冲区，不更新UI
            // 注意：_logBuffer 只在定时器回调中使用，不需要锁
            foreach (var log in logsToAdd)
            {
                _logBuffer.AppendLine(log);
                _currentLineCount++;
            }
            
            // 限制缓冲区大小，避免内存占用过大
            if (_currentLineCount > MaxLogLines * 2)
            {
                var lines = _logBuffer.ToString().Split('\n');
                if (lines.Length > MaxLogLines)
                {
                    var keptLines = lines.Skip(lines.Length - MaxLogLines);
                    _logBuffer.Clear();
                    _logBuffer.AppendLine(string.Join(Environment.NewLine, keptLines));
                    _currentLineCount = MaxLogLines;
                }
            }
            return;
        }
        
        // 🚀 应用已就绪：正常更新UI，但根据日志量动态调整更新频率
        var now = DateTime.Now;
        var timeSinceLastUpdate = (now - _lastLogUpdateTime).TotalMilliseconds;
        var pendingCount = logsToAdd.Count;
        
        // 如果日志量很大且距离上次更新时间很短，跳过本次更新（降低更新频率）
        if (pendingCount > 50 && timeSinceLastUpdate < MaxLogUpdateIntervalMs)
        {
            // 将日志重新放回队列，等待下次更新
            lock (_pendingCliLogs)
            {
                foreach (var log in logsToAdd)
                {
                    _pendingCliLogs.Enqueue(log);
                }
            }
            return;
        }
        
        Dispatcher.UIThread.Post(() =>
        {
            // 🚀 性能优化：一次性构建完整字符串块，然后一次性追加
            // 这样可以避免逐条操作 StringBuilder，减少 UI 渲染次数
            if (logsToAdd.Count > 0)
            {
                // 方法1：使用 string.Join 一次性构建（最快）
                var newLogsBlock = string.Join(Environment.NewLine, logsToAdd) + Environment.NewLine;
                
                // 一次性追加到缓冲区
                _logBuffer.Append(newLogsBlock);
                _currentLineCount += logsToAdd.Count;
                
                // 限制日志行数（只在超出阈值时执行，避免频繁字符串分割）
                if (_currentLineCount > MaxLogLines + 100)  // 留一些缓冲
                {
                    var lines = _logBuffer.ToString().Split('\n');
                    if (lines.Length > MaxLogLines)
                    {
                        // 一次性构建保留的日志块
                        var keptLines = lines.Skip(lines.Length - MaxLogLines);
                        var keptLogsBlock = string.Join(Environment.NewLine, keptLines);
                        
                        _logBuffer.Clear();
                        _logBuffer.Append(keptLogsBlock);
                        _currentLineCount = MaxLogLines;
                    }
                }
                
                // 🚀 关键：一次性更新 UI，确保同步显示
                LogText = _logBuffer.ToString();
                _lastLogUpdateTime = DateTime.Now;
                
                // 🔧 延迟滚动，确保 UI 内容已完全渲染
                // 使用 LayoutUpdated 事件或小延迟确保滚动在内容渲染后执行
                ScrollToBottomIfNeededDelayed();
            }
        });
    }
    
    /// <summary>
    /// 如果需要，滚动到日志底部（优化版本：缓存控件引用）
    /// </summary>
    private void ScrollToBottomIfNeeded()
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                // 🚀 性能优化：缓存 ScrollViewer 引用，避免每次都查找控件
                if (_cachedScrollViewer == null)
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow as MainWindow;
                        if (mainWindow?.Content is Grid mainContent)
                        {
                            // LogScrollViewer 在 SettingsView 中，需要通过 FindControl 递归查找
                            _cachedScrollViewer = mainContent.FindControl<ScrollViewer>("LogScrollViewer");
                        }
                    }
                }
                
                if (_cachedScrollViewer != null)
                {
                    // 🔍 查找 SettingsView：向上遍历父级，找到 SettingsView
                    Views.SettingsView? settingsView = null;
                    var parent = _cachedScrollViewer.Parent;
                    while (parent != null)
                    {
                        if (parent is Views.SettingsView sv)
                        {
                            settingsView = sv;
                            break;
                        }
                        parent = parent.Parent;
                    }
                    
                    // 检查是否应该自动滚动（默认应该自动滚动）
                    // 如果用户手动滚动到历史位置（距离底部 > 20px），则不应该自动滚动
                    if (settingsView?.ShouldAutoScroll ?? true)
                    {
                        // 🚀 直接滚动到底部，显示最新日志
                        _cachedScrollViewer.ScrollToEnd();
                    }
                    // 如果 ShouldAutoScroll 为 false，说明用户在查看历史日志，不自动滚动
                }
            });
        }
        catch
        {
            // 忽略滚动错误，不影响日志功能
        }
    }
    
    /// <summary>
    /// 延迟滚动到底部，确保 UI 内容已完全渲染
    /// </summary>
    private void ScrollToBottomIfNeededDelayed()
    {
        try
        {
            // 🔧 使用 InvokeAsync 支持 async/await
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                ScrollViewer? scrollViewer = _cachedScrollViewer;
                
                // 🚀 如果缓存为 null，尝试查找 ScrollViewer
                if (scrollViewer == null)
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow as MainWindow;
                        if (mainWindow?.Content is Grid mainContent)
                        {
                            // LogScrollViewer 在 SettingsView 中，需要通过 FindControl 递归查找
                            scrollViewer = mainContent.FindControl<ScrollViewer>("LogScrollViewer");
                            if (scrollViewer != null)
                            {
                                _cachedScrollViewer = scrollViewer; // 缓存引用
                            }
                        }
                    }
                }
                
                // 🔧 如果还是找不到，等待一下再试（可能 UI 还没完全加载）
                if (scrollViewer == null)
                {
                    await Task.Delay(50);
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow as MainWindow;
                        if (mainWindow?.Content is Grid mainContent)
                        {
                            scrollViewer = mainContent.FindControl<ScrollViewer>("LogScrollViewer");
                            if (scrollViewer != null)
                            {
                                _cachedScrollViewer = scrollViewer; // 缓存引用
                            }
                        }
                    }
                }
                
                if (scrollViewer != null)
                {
                    // 🔧 等待一小段时间，确保 UI 内容已完全渲染
                    await Task.Delay(20);
                    
                    // 🔍 查找 SettingsView：向上遍历父级，找到 SettingsView
                    Views.SettingsView? settingsView = null;
                    var parent = scrollViewer.Parent;
                    while (parent != null)
                    {
                        if (parent is Views.SettingsView sv)
                        {
                            settingsView = sv;
                            break;
                        }
                        parent = parent.Parent;
                    }
                    
                    // 检查是否应该自动滚动（默认应该自动滚动）
                    // 如果用户手动滚动到历史位置（距离底部 > 20px），则不应该自动滚动
                    if (settingsView?.ShouldAutoScroll ?? true)
                    {
                        // 🚀 滚动到底部，显示最新日志
                        scrollViewer.ScrollToEnd();
                    }
                    // 如果 ShouldAutoScroll 为 false，说明用户在查看历史日志，不自动滚动
                }
            });
        }
        catch
        {
            // 忽略滚动错误，不影响日志功能
        }
    }
    
    /// <summary>
    /// 立即刷新所有待处理的日志（用于停止或清理时，或应用就绪时）
    /// </summary>
    private void FlushPendingLogs()
    {
        try
        {
            List<string> logsToAdd;
            
            lock (_pendingCliLogs)
            {
                logsToAdd = new List<string>(_pendingCliLogs);
                _pendingCliLogs.Clear();
            }
            
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // 先处理队列中的新日志
                if (logsToAdd.Count > 0)
                {
                    foreach (var log in logsToAdd)
                    {
                        _logBuffer.AppendLine(log);
                        _currentLineCount++;
                    }
                }
                
                // 🚀 如果应用刚就绪且日志很多，只显示最后N行，避免一次性渲染太多日志
                if (_isApplicationReady && _currentLineCount > InitialDisplayLines && _lastLogUpdateTime == DateTime.MinValue)
                {
                    var allLogs = _logBuffer.ToString();
                    var lines = allLogs.Split('\n');
                    
                    if (lines.Length > InitialDisplayLines)
                    {
                        var displayLines = lines.Skip(lines.Length - InitialDisplayLines);
                        var displayText = string.Join(Environment.NewLine, displayLines);
                        
                        var skippedCount = lines.Length - InitialDisplayLines;
                        LogText = $"[已跳过 {skippedCount} 行启动日志，仅显示最后 {InitialDisplayLines} 行]{Environment.NewLine}{displayText}";
                        
                        _logBuffer.Clear();
                        _logBuffer.Append(displayText);
                        _currentLineCount = InitialDisplayLines;
                    }
                    else
                    {
                        LogText = allLogs;
                    }
                }
                else if (_logBuffer.Length > 0)
                {
                    // 正常显示所有日志
                    LogText = _logBuffer.ToString();
                }
                
                _lastLogUpdateTime = DateTime.Now;
                // 🔧 延迟滚动，确保 UI 内容已完全渲染
                ScrollToBottomIfNeededDelayed();
            });
        }
        catch
        {
            // 忽略错误
        }
    }

    #endregion
}
