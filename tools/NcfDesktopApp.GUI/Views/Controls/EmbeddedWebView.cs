/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：EmbeddedWebView.cs
    文件功能描述：EmbeddedWebView.cs 相关实现
    
    
    创建标识：Senparc - 20250720
    
    修改标识：Senparc - 20260729
    修改描述：v0.3.3 修复 macOS WebView 编辑桥接并清理应用包构建残留

----------------------------------------------------------------*/

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using NcfDesktopApp.GUI.Services;
using System.Runtime.InteropServices;
using WebView = AvaloniaWebView.WebView;

namespace NcfDesktopApp.GUI.Views.Controls;

public partial class EmbeddedWebView : UserControl
{
    public static readonly StyledProperty<string> SourceProperty =
        AvaloniaProperty.Register<EmbeddedWebView, string>(nameof(Source), "");

    public string Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private Border _contentBorder = null!;
    private string _currentUrl = "";
    private bool _isWebViewReady = false;
    
    private TextBlock _statusText = null!;
    private Grid _webViewContainer = null!;
    private Border _webViewArea = null!;
    private WebView? _webView = null;
    
    /// <summary>
    /// 获取 WebView 是否已初始化完成
    /// </summary>
    public bool IsWebViewReady => _isWebViewReady;

    public EmbeddedWebView()
    {
        InitializeComponent();
        _ = InitializeWebViewAsync();
    }

    private void InitializeComponent()
    {
        // 状态显示（仅在需要时显示）
        var statusArea = new Border
        {
            Background = Brushes.LightBlue,
            BorderBrush = Brushes.SteelBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            MinHeight = 40,
            IsVisible = false,
            Margin = new Thickness(10, 10, 10, 0)
        };

        _statusText = new TextBlock
        {
            Text = "正在初始化嵌入式浏览器...",
            FontSize = 12,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkSlateBlue
        };

        statusArea.Child = _statusText;

        // WebView 区域
        _webViewContainer = new Grid
        {
            RowDefinitions = new RowDefinitions("*")
        };

        _webViewArea = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            MinHeight = 400,
            Margin = new Thickness(10),
            Child = _webViewContainer
        };

        // 初始化时的占位内容
        var placeholderContent = new StackPanel
        {
            Spacing = 15
        };

        var placeholderBorder = new Border
        {
            Padding = new Thickness(20),
            Child = placeholderContent
        };

        var welcomeText = new TextBlock
        {
            Text = "🌐 嵌入式浏览器",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkBlue
        };

        var descText = new TextBlock
        {
            Text = "正在初始化浏览器控件...",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 20)
        };

        placeholderContent.Children.Add(welcomeText);
        placeholderContent.Children.Add(descText);
        _webViewContainer.Children.Add(placeholderBorder);
        Grid.SetRow(placeholderBorder, 0);

        // 主容器
        var mainContainer = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        mainContainer.Children.Add(statusArea);
        Grid.SetRow(statusArea, 0);
        
        mainContainer.Children.Add(_webViewArea);
        Grid.SetRow(_webViewArea, 1);

        _contentBorder = new Border
        {
            Background = Brushes.White,
            Child = mainContainer
        };

        Content = _contentBorder;
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    await InitializeWebViewHostAsync();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"浏览器初始化失败: {ex.Message}", Brushes.Red);
                    ShowFallbackView();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"浏览器初始化异常: {ex.Message}");
            ShowFallbackView();
        }
    }

    private async Task InitializeWebViewHostAsync()
    {
        try
        {
            UpdateStatus("正在初始化浏览器控件...", Brushes.Blue);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    Debug.WriteLine("🔧 创建 WebView.Avalonia 控件");
                    Debug.WriteLine($"   平台: {RuntimeInformation.OSDescription}");
                    Debug.WriteLine($"   架构: {RuntimeInformation.ProcessArchitecture}");
                    
                    // 直接创建 WebView.Avalonia 控件
                    _webView = new WebView();
                    _webView.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                    _webView.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                    _webView.NavigationCompleted += OnWebViewNavigationCompleted;

                    _webViewContainer.Children.Clear();
                    _webViewContainer.Children.Add(_webView);
                    Grid.SetRow(_webView, 0);

                    _isWebViewReady = true;
                    Debug.WriteLine("✅ WebView 创建成功");
                    UpdateStatus("嵌入式浏览器已就绪", Brushes.Green);

                    // 🔧 方案1优化：如果有初始 URL，则导航到它
                    // 注意：这里直接调用 NavigateToUrlAsync，而不是 UpdateSource()，
                    // 因为 UpdateSource() 会检查 _currentUrl，但此时还没有设置
                    if (WebNavigationPolicy.TryGetNavigableUri(Source, out _))
                    {
                        Debug.WriteLine($"🎯 准备导航到初始 URL: {Source}");
                        _ = NavigateToUrlAsync(Source);
                    }
                    else
                    {
                        // 如果没有初始 URL，确保 _currentUrl 为空，这样后续设置 Source 时会导航
                        _currentUrl = "";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ 创建 WebView 失败: {ex.Message}");
                    Debug.WriteLine($"   异常类型: {ex.GetType().Name}");
                    Debug.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"浏览器初始化失败: {ex.Message}", Brushes.Red);
            throw;
        }
    }


    private void UpdateStatus(string message, IBrush color)
    {
        _statusText.Text = message;
        _statusText.Foreground = color;
        
        // 显示状态区域
        var statusArea = _statusText.Parent as Border;
        if (statusArea != null)
        {
            statusArea.IsVisible = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == SourceProperty)
        {
            UpdateSource();
        }
    }

    private void UpdateSource()
    {
        // 🔧 方案1优化：避免在标签切换时重新导航
        // 如果 URL 没有变化，不执行导航（保持当前页面状态）
        if (_isWebViewReady && WebNavigationPolicy.TryGetNavigableUri(Source, out _))
        {
            // 比较新 URL 和当前 URL，如果相同则跳过导航
            if (string.Equals(_currentUrl, Source, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"ℹ️ Source 属性变化但 URL 相同 ({Source})，跳过导航以保持页面状态");
                return;
            }
            
            Debug.WriteLine($"🔄 Source 属性变化，从 {_currentUrl} 导航到 {Source}");
            _ = NavigateToUrlAsync(Source);
        }
    }

    private async Task NavigateToUrlAsync(string url)
    {
        if (!_isWebViewReady || !WebNavigationPolicy.TryGetNavigableUri(url, out var targetUri))
        {
            Debug.WriteLine($"⚠️ 跳过导航: Ready={_isWebViewReady}, URL={url}");
            return;
        }

        try
        {
            OnNavigationStarted(url);
            UpdateStatus("正在导航到页面...", Brushes.Blue);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_webView != null)
                {
                    try
                    {
                        Debug.WriteLine($"🚀 WebView.Url 设置为: {url}");
                        _webView.Url = targetUri;
                        _currentUrl = url;
                        Debug.WriteLine($"✅ WebView.Url 设置成功");
                        UpdateStatus("页面加载完成", Brushes.Green);
                        // 真正注入补丁以 NavigationCompleted 为准；此处先通知外层 UI。
                        OnNavigationCompleted(url);
                    }
                    catch (Exception navEx)
                    {
                        Debug.WriteLine($"❌ WebView.Url 设置失败: {navEx.Message}");
                        Debug.WriteLine($"   堆栈跟踪: {navEx.StackTrace}");
                        throw;
                    }
                }
                else
                {
                    Debug.WriteLine("❌ WebView 为 null，无法导航");
                    throw new InvalidOperationException("WebView is not initialized");
                }
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"导航失败: {ex.Message}", Brushes.Red);
            OnNavigationFailed($"导航失败: {ex.Message}");
        }
    }

    public async Task NavigateTo(string url)
    {
        await NavigateToUrlAsync(url);
    }

    // 刷新功能，供外部调用
    public void Refresh()
    {
        if (!_isWebViewReady) return;
        try
        {
            _webView?.Reload();
        }
        catch { }
    }

    /// <summary>供主窗口 Edit 菜单 / 快捷键调用：全选。</summary>
    public Task<bool> SelectAllAsync() => WebViewEditBridge.TrySelectAllAsync(_webView);

    /// <summary>供主窗口 Edit 菜单 / 快捷键调用：复制。</summary>
    public Task<bool> CopyAsync() => WebViewEditBridge.TryCopyAsync(_webView, WebViewEditBridge.GetClipboard(this));

    /// <summary>供主窗口 Edit 菜单 / 快捷键调用：剪切。</summary>
    public Task<bool> CutAsync() => WebViewEditBridge.TryCutAsync(_webView, WebViewEditBridge.GetClipboard(this));

    /// <summary>供主窗口 Edit 菜单 / 快捷键调用：粘贴。</summary>
    public Task<bool> PasteAsync() => WebViewEditBridge.TryPasteAsync(_webView, WebViewEditBridge.GetClipboard(this));

    private void OnWebViewNavigationCompleted(object? sender, WebViewCore.Events.WebViewUrlLoadedEventArg e)
    {
        Debug.WriteLine($"[EmbeddedWebView] NavigationCompleted IsSuccess={e.IsSuccess}");
        if (!e.IsSuccess)
        {
            return;
        }

        if (WebViewEditBridge.IsScriptBridgeSupported)
        {
            _ = WebViewEditBridge.EnsureKeyboardPatchAsync(_webView);
        }
    }

    // 后退功能，供外部调用  
    public void GoBack()
    {
        // WebView.Avalonia 的 WebView 类可能不支持导航历史
        Debug.WriteLine("⚠️ GoBack 功能在 WebView.Avalonia 中可能不可用");
    }

    // 前进功能，供外部调用
    public void GoForward()
    {
        // WebView.Avalonia 的 WebView 类可能不支持导航历史
        Debug.WriteLine("⚠️ GoForward 功能在 WebView.Avalonia 中可能不可用");
    }

    // 检查是否可以后退
    public bool CanGoBack => false;

    // 检查是否可以前进
    public bool CanGoForward => false;

    private void OpenInExternalBrowser(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开外部浏览器失败: {ex.Message}");
        }
    }

    private void ShowFallbackView()
    {
        _webViewContainer.Children.Clear();
        
        var fallbackContent = new StackPanel
        {
            Spacing = 15,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var fallbackBorder = new Border
        {
            Padding = new Thickness(40),
            Child = fallbackContent,
            MaxWidth = 600
        };

        var errorText = new TextBlock
        {
            Text = "❌ 内置浏览器初始化失败",
            FontSize = 20,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Red,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var descText = new TextBlock
        {
            Text = "无法加载内置浏览器组件。这可能是因为：",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // 原因列表
        var reasonsList = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(20, 0, 20, 20)
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            reasonsList.Children.Add(CreateReasonItem("• WebView2 Runtime 未安装或安装失败"));
            reasonsList.Children.Add(CreateReasonItem("• 系统权限不足"));
        }
        else
        {
            reasonsList.Children.Add(CreateReasonItem("• 系统 WebView 组件不可用"));
        }
        reasonsList.Children.Add(CreateReasonItem("• 组件版本不兼容"));

        // 解决方案文本
        var solutionText = new TextBlock
        {
            Text = "您可以尝试以下解决方案：",
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 15)
        };

        // 按钮容器
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        // 在外部浏览器中打开按钮
        var openExternalButton = new Button
        {
            Content = "🌍 在外部浏览器中打开",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(25, 12),
            Background = Brushes.DodgerBlue,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(6),
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        openExternalButton.Click += (s, e) =>
        {
            if (WebNavigationPolicy.TryGetNavigableUri(Source, out _))
            {
                OpenInExternalBrowser(Source);
            }
        };

        buttonPanel.Children.Add(openExternalButton);

        // 仅在 Windows 上显示下载 WebView2 的按钮
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var downloadWebView2Button = new Button
            {
                Content = "⬇️ 下载 WebView2 Runtime",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Padding = new Thickness(25, 12),
                Background = Brushes.Orange,
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(6),
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            downloadWebView2Button.Click += (s, e) =>
            {
                OpenInExternalBrowser("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            };

            buttonPanel.Children.Add(downloadWebView2Button);

            // 添加提示文本
            var hintText = new TextBlock
            {
                Text = "💡 下载并安装 WebView2 后，重启应用即可使用内置浏览器",
                FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 15, 0, 0),
                MaxWidth = 500
            };
            buttonPanel.Children.Add(hintText);
        }

        fallbackContent.Children.Add(errorText);
        fallbackContent.Children.Add(descText);
        fallbackContent.Children.Add(reasonsList);
        fallbackContent.Children.Add(solutionText);
        fallbackContent.Children.Add(buttonPanel);
        
        _webViewContainer.Children.Add(fallbackBorder);
    }

    private TextBlock CreateReasonItem(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = Brushes.DarkGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
    }

    public event EventHandler<string>? NavigationStarted;
    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? NavigationFailed;

    protected virtual void OnNavigationStarted(string url)
    {
        NavigationStarted?.Invoke(this, url);
    }

    protected virtual void OnNavigationCompleted(string url)
    {
        NavigationCompleted?.Invoke(this, url);
    }

    protected virtual void OnNavigationFailed(string error)
    {
        NavigationFailed?.Invoke(this, error);
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Debug.WriteLine($"🔍 [OnLoaded] _isWebViewReady={_isWebViewReady}, _webView={(_webView != null ? "存在" : "null")}, _currentUrl={_currentUrl}");
        
        // 🔧 方案1：只在首次加载时初始化，避免标签切换时重新初始化
        // Avalonia 的 TabControl 默认保持标签内容在内存中，不会完全卸载
        if (!_isWebViewReady)
        {
            Debug.WriteLine("🔄 首次加载，初始化 WebView...");
            _ = InitializeWebViewAsync();
        }
        else
        {
            Debug.WriteLine("✅ WebView 已就绪，跳过重新初始化（保持状态）");
            
            // 🔧 检查 WebView 是否仍然存在且有效
            if (_webView != null)
            {
                try
                {
                    var currentWebViewUrl = _webView.Url?.ToString() ?? "null";
                    Debug.WriteLine($"   WebView.Url = {currentWebViewUrl}");
                    
                    // 如果 WebView.Url 为空但 _currentUrl 不为空，尝试恢复
                    if (string.IsNullOrEmpty(currentWebViewUrl) && !string.IsNullOrEmpty(_currentUrl))
                    {
                        Debug.WriteLine($"⚠️ WebView.Url 丢失，尝试恢复导航到: {_currentUrl}");
                        _ = NavigateToUrlAsync(_currentUrl);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ 检查 WebView 状态时出错: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("⚠️ WebView 为 null，需要重新初始化");
                _ = InitializeWebViewAsync();
            }
        }
    }
    
    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        // 🔧 方案1：禁用清理逻辑，防止标签切换时丢失 Session/Cookie
        // Avalonia 的 TabControl 在标签切换时可能触发 OnUnloaded，但不会完全销毁控件
        // 因此我们不清理 WebView，以保持登录状态和浏览历史
        Debug.WriteLine("ℹ️ OnUnloaded 触发，保持 WebView 状态（不清理）");
        
        // ❌ 已禁用：防止标签切换时清理 WebView（会丢失登录状态）
        // CleanupWebView();
    }
    
    /// <summary>
    /// 清理 WebView 资源（修复 Windows ARM64 重新初始化问题）
    /// </summary>
    private void CleanupWebView()
    {
        try
        {
            Debug.WriteLine("🧹 开始清理 WebView 资源...");
            
            if (_webView != null)
            {
                try
                {
                    // 1. 导航到空白页，释放网页资源
                    try
                    {
                        _webView.Url = new Uri("about:blank");
                        Debug.WriteLine("   ✓ WebView 已导航到空白页");
                    }
                    catch { /* 忽略导航失败 */ }
                    
                    // 2. 从容器中移除
                    _webViewContainer?.Children.Remove(_webView);
                    Debug.WriteLine("   ✓ WebView 已从容器移除");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"   ⚠️ WebView 清理警告: {ex.Message}");
                }
                finally
                {
                    _webView = null;
                }
            }
            
            // 3. 重置初始化标志（关键！）
            _isWebViewReady = false;
            _currentUrl = "";
            
            Debug.WriteLine("✅ WebView 资源清理完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ WebView 清理失败: {ex.Message}");
        }
    }
}
