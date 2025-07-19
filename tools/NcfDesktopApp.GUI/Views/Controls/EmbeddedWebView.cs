using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System.Net.Http;
using System.Text;
using System.IO;
using Avalonia.Platform;
using System.Runtime.InteropServices;

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
    private static readonly HttpClient _httpClient = new();
    
    private TextBlock _statusText = null!;
    private Button _refreshButton = null!;
    private Button _openExternalButton = null!;
    private Button _backButton = null!;
    private Button _forwardButton = null!;
    private TextBox _urlTextBox = null!;
    private StackPanel _webViewContainer = null!;
    private Border _webViewArea = null!;
    private NativeWebViewHost _nativeWebViewHost = null!;

    public EmbeddedWebView()
    {
        InitializeComponent();
        _ = InitializeWebViewAsync();
    }

    private void InitializeComponent()
    {
        var content = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(10)
        };

        // 地址栏
        var urlPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 5,
            Margin = new Thickness(0, 0, 0, 10)
        };

        _backButton = new Button
        {
            Content = "←",
            Width = 35,
            Height = 30,
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Background = Brushes.LightGray,
            Foreground = Brushes.Black,
            CornerRadius = new CornerRadius(4),
            IsEnabled = false
        };
        _backButton.Click += OnBackClick;

        _forwardButton = new Button
        {
            Content = "→",
            Width = 35,
            Height = 30,
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Background = Brushes.LightGray,
            Foreground = Brushes.Black,
            CornerRadius = new CornerRadius(4),
            IsEnabled = false
        };
        _forwardButton.Click += OnForwardClick;

        _refreshButton = new Button
        {
            Content = "🔄",
            Width = 35,
            Height = 30,
            FontSize = 14,
            Background = Brushes.LightBlue,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            IsEnabled = false
        };
        _refreshButton.Click += OnRefreshClick;

        _urlTextBox = new TextBox
        {
            Height = 30,
            FontSize = 12,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            IsReadOnly = true
        };

        _openExternalButton = new Button
        {
            Content = "🌍",
            Width = 35,
            Height = 30,
            FontSize = 14,
            Background = Brushes.Orange,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            IsEnabled = false
        };
        _openExternalButton.Click += OnOpenExternalClick;

        urlPanel.Children.Add(_backButton);
        urlPanel.Children.Add(_forwardButton);
        urlPanel.Children.Add(_refreshButton);
        urlPanel.Children.Add(_urlTextBox);
        urlPanel.Children.Add(_openExternalButton);

        // 状态显示
        var statusArea = new Border
        {
            Background = Brushes.LightBlue,
            BorderBrush = Brushes.SteelBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            MinHeight = 40,
            IsVisible = false
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
        _webViewContainer = new StackPanel
        {
            Spacing = 15
        };

        _webViewArea = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            MinHeight = 400,
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
            Text = "正在初始化原生浏览器控件...",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 20)
        };

        placeholderContent.Children.Add(welcomeText);
        placeholderContent.Children.Add(descText);
        _webViewContainer.Children.Add(placeholderBorder);

        content.Children.Add(urlPanel);
        content.Children.Add(statusArea);
        content.Children.Add(_webViewArea);

        _contentBorder = new Border
        {
            Background = Brushes.White,
            Child = content
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
                    await InitializeNativeBrowserAsync();
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

    private async Task InitializeNativeBrowserAsync()
    {
        try
        {
            UpdateStatus("正在初始化原生浏览器控件...", Brushes.Blue);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // 创建原生 WebView 主机
                    _nativeWebViewHost = new NativeWebViewHost();
                    
                    // 清除占位内容并添加原生 WebView
                    _webViewContainer.Children.Clear();
                    _webViewContainer.Children.Add(_nativeWebViewHost);

                    _isWebViewReady = true;
                    UpdateStatus("嵌入式浏览器已就绪", Brushes.Green);
                    
                    // 启用控制按钮
                    _refreshButton.IsEnabled = true;
                    _openExternalButton.IsEnabled = true;
                    _backButton.IsEnabled = true;
                    _forwardButton.IsEnabled = true;

                    // 如果有初始 URL，则导航到它
                    if (!string.IsNullOrEmpty(Source))
                    {
                        _ = NavigateToUrlAsync(Source);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"创建原生 WebView 失败: {ex.Message}");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"原生浏览器初始化失败: {ex.Message}", Brushes.Red);
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
            return;

        try
        {
            OnNavigationStarted(url);
            UpdateStatus("正在导航到页面...", Brushes.Blue);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _nativeWebViewHost?.NavigateTo(url);
                _currentUrl = url;
                _urlTextBox.Text = url;
            });
        }
        catch (Exception ex)
        {
            OnNavigationFailed($"导航失败: {ex.Message}");
        }
    }

    public async Task NavigateTo(string url)
    {
        await NavigateToUrlAsync(url);
    }

    private void OnBackClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isWebViewReady)
        {
            _nativeWebViewHost?.GoBack();
        }
    }

    private void OnForwardClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isWebViewReady)
        {
            _nativeWebViewHost?.GoForward();
        }
    }

    private void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isWebViewReady)
        {
            _nativeWebViewHost?.Refresh();
        }
    }

    private void OnOpenExternalClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            OpenInExternalBrowser(_currentUrl);
        }
    }

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
        _nativeWebViewHost = null;
    }
}

// 原生 WebView 主机类
public class NativeWebViewHost : NativeControlHost
{
    private IPlatformHandle? _nativeHandle;
    private bool _isInitialized = false;
    private string _currentUrl = "";

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (_isInitialized)
            return _nativeHandle!;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: 尝试使用 WebView2
                _nativeHandle = CreateWebView2Control(parent);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: 尝试使用 WKWebView
                _nativeHandle = CreateWKWebViewControl(parent);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: 尝试使用 WebKitGTK
                _nativeHandle = CreateWebKitGTKControl(parent);
            }
            else
            {
                // 其他平台：创建占位控件
                _nativeHandle = CreatePlaceholderControl(parent);
            }

            _isInitialized = true;
            return _nativeHandle;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"创建原生控件失败: {ex.Message}");
            return CreatePlaceholderControl(parent);
        }
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        // 清理原生控件资源
        _nativeHandle = null;
        _isInitialized = false;
    }

    private IPlatformHandle CreateWebView2Control(IPlatformHandle parent)
    {
        // Windows WebView2 实现
        // 这里需要调用 Windows API 创建 WebView2 控件
        // 由于复杂性，这里提供一个占位实现
        Debug.WriteLine("Windows WebView2 控件创建中...");
        return CreatePlaceholderControl(parent);
    }

    private IPlatformHandle CreateWKWebViewControl(IPlatformHandle parent)
    {
        // macOS WKWebView 实现
        // 这里需要调用 macOS API 创建 WKWebView 控件
        // 由于复杂性，这里提供一个占位实现
        Debug.WriteLine("macOS WKWebView 控件创建中...");
        return CreatePlaceholderControl(parent);
    }

    private IPlatformHandle CreateWebKitGTKControl(IPlatformHandle parent)
    {
        // Linux WebKitGTK 实现
        // 这里需要调用 GTK API 创建 WebKit 控件
        // 由于复杂性，这里提供一个占位实现
        Debug.WriteLine("Linux WebKitGTK 控件创建中...");
        return CreatePlaceholderControl(parent);
    }

    private IPlatformHandle CreatePlaceholderControl(IPlatformHandle parent)
    {
        // 创建一个占位控件，显示提示信息
        var placeholder = new Border
        {
            Background = Brushes.LightGray,
            Child = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "🌐",
                        FontSize = 48,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "原生浏览器控件",
                        FontSize = 16,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "正在开发中...",
                        FontSize = 12,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Foreground = Brushes.Gray
                    },
                    new TextBlock
                    {
                        Text = "当前 URL: " + (_currentUrl ?? "未设置"),
                        FontSize = 10,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Foreground = Brushes.DarkGray,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    }
                }
            }
        };

        // 返回一个占位句柄
        return new PlatformHandle(IntPtr.Zero, "PLACEHOLDER");
    }

    public void NavigateTo(string url)
    {
        _currentUrl = url;
        Debug.WriteLine($"导航到: {url}");
        
        // 这里应该调用原生控件的导航方法
        // 由于当前是占位实现，只是记录 URL
    }

    public void GoBack()
    {
        Debug.WriteLine("后退");
        // 这里应该调用原生控件的后退方法
    }

    public void GoForward()
    {
        Debug.WriteLine("前进");
        // 这里应该调用原生控件的前进方法
    }

    public void Refresh()
    {
        Debug.WriteLine("刷新");
        // 这里应该调用原生控件的刷新方法
    }
} 