using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
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
                    
                    _webViewContainer.Children.Clear();
                    _webViewContainer.Children.Add(_webView);
                    Grid.SetRow(_webView, 0);

                    _isWebViewReady = true;
                    Debug.WriteLine("✅ WebView 创建成功");
                    UpdateStatus("嵌入式浏览器已就绪", Brushes.Green);

                    // 如果有初始 URL，则导航到它
                    if (!string.IsNullOrEmpty(Source))
                    {
                        Debug.WriteLine($"🎯 准备导航到初始 URL: {Source}");
                        _ = NavigateToUrlAsync(Source);
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
        if (_isWebViewReady && !string.IsNullOrEmpty(Source))
        {
            _ = NavigateToUrlAsync(Source);
        }
    }

    private async Task NavigateToUrlAsync(string url)
    {
        if (!_isWebViewReady || string.IsNullOrEmpty(url))
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
                        _webView.Url = new Uri(url);
                        _currentUrl = url;
                        Debug.WriteLine($"✅ WebView.Url 设置成功");
                        UpdateStatus("页面加载完成", Brushes.Green);
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
            Spacing = 15
        };

        var fallbackBorder = new Border
        {
            Padding = new Thickness(20),
            Child = fallbackContent
        };

        var errorText = new TextBlock
        {
            Text = "❌ 嵌入式浏览器初始化失败",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Red
        };

        var descText = new TextBlock
        {
            Text = "无法加载嵌入式浏览器组件。\n请使用外部浏览器打开 NCF 应用。",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var openExternalButton = new Button
        {
            Content = "🌍 在外部浏览器中打开",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
            Background = Brushes.Orange,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4)
        };
        openExternalButton.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(Source))
            {
                OpenInExternalBrowser(Source);
            }
        };

        fallbackContent.Children.Add(errorText);
        fallbackContent.Children.Add(descText);
        fallbackContent.Children.Add(openExternalButton);
        
        _webViewContainer.Children.Add(fallbackBorder);
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

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        // 清理资源
        _webView = null;
    }
} 