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
using Avalonia.Layout;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;

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
    private Grid _webViewContainer = null!;
    private Border _webViewArea = null!;
    private WebViewHost? _webViewHost = null;
    private Control? _nativeWebView = null;
    private Type? _nativeWebViewType = null;

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
                    // 优先尝试使用 WebView.Avalonia 的原生控件
                    _nativeWebView = TryCreateNativeWebView(out _nativeWebViewType);
                    _webViewContainer.Children.Clear();

                    if (_nativeWebView != null)
                    {
                        _nativeWebView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        _nativeWebView.VerticalAlignment = VerticalAlignment.Stretch;
                        _nativeWebView.Width = double.NaN; // Auto
                        _nativeWebView.Height = double.NaN; // Auto
                        _webViewContainer.Children.Clear();
                        _webViewContainer.Children.Add(_nativeWebView);
                        Grid.SetRow(_nativeWebView, 0);
                    }
                    else
                    {
                        // 回退到占位实现
                        _webViewHost = new WebViewHost
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Width = double.NaN,
                            Height = double.NaN
                        };
                        _webViewContainer.Children.Clear();
                        _webViewContainer.Children.Add(_webViewHost);
                        Grid.SetRow(_webViewHost, 0);
                    }

                    _isWebViewReady = true;
                    UpdateStatus("嵌入式浏览器已就绪", Brushes.Green);

                    // 如果有初始 URL，则导航到它
                    if (!string.IsNullOrEmpty(Source))
                    {
                        _ = NavigateToUrlAsync(Source);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"创建 WebView 失败: {ex.Message}");
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

    private Control? TryCreateNativeWebView(out Type? controlType)
    {
        controlType = null;
        
        // 🔥 Windows 平台：优先使用原生 WebView2 控件
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Debug.WriteLine("🪟 检测到 Windows 平台，尝试使用 WindowsWebView2Control");
                
                var webView2Control = new WindowsWebView2Control();
                controlType = typeof(WindowsWebView2Control);
                
                // 订阅导航事件
                webView2Control.NavigationStarted += (s, url) =>
                {
                    Debug.WriteLine($"🚢 [WindowsWebView2] 导航开始: {url}");
                    OnNavigationStarted(url);
                };
                
                webView2Control.NavigationCompleted += (s, url) =>
                {
                    Debug.WriteLine($"✅ [WindowsWebView2] 导航完成: {url}");
                    OnNavigationCompleted(url);
                };
                
                webView2Control.NavigationFailed += (s, error) =>
                {
                    Debug.WriteLine($"❌ [WindowsWebView2] 导航失败: {error}");
                    OnNavigationFailed(error);
                };
                
                Debug.WriteLine("✅ WindowsWebView2Control 创建成功");
                return webView2Control;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ WindowsWebView2Control 创建失败: {ex.Message}");
                Debug.WriteLine($"   异常类型: {ex.GetType().Name}");
                Debug.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
                Debug.WriteLine("   将回退到 WebView.Avalonia");
                // 继续尝试 WebView.Avalonia
            }
        }
        
        // 回退方案：尝试使用 WebView.Avalonia (跨平台)
        try
        {
            Debug.WriteLine("🔍 尝试查找 WebView.Avalonia 控件");
            
            // 优先匹配包名包含 "Avalonia.WebView" 的程序集中的类型名 "WebView"
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var candidateTypes = assemblies
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(Control).IsAssignableFrom(t)
                            && string.Equals(t.Name, "WebView", StringComparison.Ordinal)
                            && (t.Namespace?.Contains("Avalonia.WebView", StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            // 兼容可能的命名空间变化，兜底匹配类型名为 WebView 的控件
            if (candidateTypes.Count == 0)
            {
                candidateTypes = assemblies
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                    })
                    .Where(t => typeof(Control).IsAssignableFrom(t)
                                && string.Equals(t.Name, "WebView", StringComparison.Ordinal))
                    .ToList();
            }

            var type = candidateTypes.FirstOrDefault();
            if (type == null)
            {
                Debug.WriteLine("❌ 未找到 WebView.Avalonia 控件类型，使用占位实现");
                return null;
            }

            controlType = type;
            var instance = Activator.CreateInstance(type) as Control;
            Debug.WriteLine($"✅ 创建了 WebView.Avalonia 控件: {type.FullName}");
            return instance;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 创建原生 WebView 控件失败: {ex.Message}");
            controlType = null;
            return null;
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
            
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_nativeWebView != null && _nativeWebViewType != null)
                {
                    // 🔥 优先处理 WindowsWebView2Control
                    if (_nativeWebView is WindowsWebView2Control webView2)
                    {
                        Debug.WriteLine($"🚀 使用 WindowsWebView2Control 导航到: {url}");
                        await webView2.NavigateAsync(url);
                    }
                    else
                    {
                        // WebView.Avalonia 或其他控件
                        // 优先设置 Source 属性
                        var sourceProp = _nativeWebViewType.GetProperty("Source", BindingFlags.Public | BindingFlags.Instance);
                        if (sourceProp != null && sourceProp.CanWrite)
                        {
                            try
                            {
                                if (sourceProp.PropertyType == typeof(string))
                                {
                                    sourceProp.SetValue(_nativeWebView, url);
                                }
                                else if (sourceProp.PropertyType == typeof(Uri))
                                {
                                    sourceProp.SetValue(_nativeWebView, new Uri(url));
                                }
                                else
                                {
                                    // 其他类型，尝试直接赋值
                                    sourceProp.SetValue(_nativeWebView, url);
                                }
                            }
                            catch (Exception setEx)
                            {
                                Debug.WriteLine($"设置 WebView.Source 失败: {setEx.Message}");
                            }
                        }
                        else
                        {
                            // 尝试调用 Navigate 方法
                            var navigateMethod = _nativeWebViewType.GetMethod("Navigate", BindingFlags.Public | BindingFlags.Instance);
                            if (navigateMethod != null)
                            {
                                try
                                {
                                    navigateMethod.Invoke(_nativeWebView, new object?[] { url });
                                }
                                catch (Exception navEx)
                                {
                                    Debug.WriteLine($"调用 WebView.Navigate 失败: {navEx.Message}");
                                }
                            }
                        }
                    }
                }
                else if (_webViewHost != null)
                {
                    _webViewHost.NavigateTo(url);
                }
                _currentUrl = url;
            });
            
            // 导航完成后更新状态
            UpdateStatus("页面加载完成", Brushes.Green);
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
            if (_nativeWebView is WindowsWebView2Control webView2)
            {
                webView2.Refresh();
            }
            else if (_nativeWebView != null && _nativeWebViewType != null)
            {
                var method = _nativeWebViewType.GetMethod("Reload", BindingFlags.Public | BindingFlags.Instance)
                             ?? _nativeWebViewType.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance);
                method?.Invoke(_nativeWebView, null);
            }
            else if (_webViewHost != null)
            {
                _webViewHost.Refresh();
            }
        }
        catch { }
    }

    // 后退功能，供外部调用
    public void GoBack()
    {
        if (!_isWebViewReady) return;
        try
        {
            if (_nativeWebView is WindowsWebView2Control webView2)
            {
                webView2.GoBack();
            }
            else if (_nativeWebView != null && _nativeWebViewType != null)
            {
                var canGoBackProp = _nativeWebViewType.GetProperty("CanGoBack", BindingFlags.Public | BindingFlags.Instance);
                var goBackMethod = _nativeWebViewType.GetMethod("GoBack", BindingFlags.Public | BindingFlags.Instance);
                var canGoBack = canGoBackProp?.GetValue(_nativeWebView) as bool?;
                if (canGoBack == true)
                {
                    goBackMethod?.Invoke(_nativeWebView, null);
                }
            }
            else if (_webViewHost?.CanGoBack == true)
            {
                _webViewHost.GoBack();
            }
        }
        catch { }
    }

    // 前进功能，供外部调用
    public void GoForward()
    {
        if (!_isWebViewReady) return;
        try
        {
            if (_nativeWebView is WindowsWebView2Control webView2)
            {
                webView2.GoForward();
            }
            else if (_nativeWebView != null && _nativeWebViewType != null)
            {
                var canGoForwardProp = _nativeWebViewType.GetProperty("CanGoForward", BindingFlags.Public | BindingFlags.Instance);
                var goForwardMethod = _nativeWebViewType.GetMethod("GoForward", BindingFlags.Public | BindingFlags.Instance);
                var canGoForward = canGoForwardProp?.GetValue(_nativeWebView) as bool?;
                if (canGoForward == true)
                {
                    goForwardMethod?.Invoke(_nativeWebView, null);
                }
            }
            else if (_webViewHost?.CanGoForward == true)
            {
                _webViewHost.GoForward();
            }
        }
        catch { }
    }

    // 检查是否可以后退
    public bool CanGoBack
        => _isWebViewReady && (
            (_nativeWebView is WindowsWebView2Control webView2 && webView2.CanGoBack)
            || (_nativeWebView != null && _nativeWebViewType?.GetProperty("CanGoBack")?.GetValue(_nativeWebView) as bool? == true)
            || (_webViewHost?.CanGoBack == true)
        );

    // 检查是否可以前进
    public bool CanGoForward
        => _isWebViewReady && (
            (_nativeWebView is WindowsWebView2Control webView2 && webView2.CanGoForward)
            || (_nativeWebView != null && _nativeWebViewType?.GetProperty("CanGoForward")?.GetValue(_nativeWebView) as bool? == true)
            || (_webViewHost?.CanGoForward == true)
        );

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
        _webViewHost = null;
        _nativeWebView = null;
        _nativeWebViewType = null;
    }
}

// WebView 主机类
public class WebViewHost : UserControl
{
    private string _currentUrl = "";
    private StackPanel _contentContainer = null!;
    private Border _webContentArea = null!;
    private TextBlock _urlDisplay = null!;
    private TextBlock _statusDisplay = null!;

    public WebViewHost()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _contentContainer = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Spacing = 10
        };

        // 创建网页内容区域
        _webContentArea = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            MinHeight = 350
        };

        // 创建内容显示区域
        var contentDisplay = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 15
        };

        var webIcon = new TextBlock
        {
            Text = "🌐",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var webTitle = new TextBlock
        {
            Text = "嵌入式网页内容",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkBlue
        };

        _urlDisplay = new TextBlock
        {
            Text = "等待加载...",
            FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        _statusDisplay = new TextBlock
        {
            Text = "准备就绪",
            FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Green
        };

        var openButton = new Button
        {
            Content = "🌍 在外部浏览器中打开",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
            Background = Brushes.Blue,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4)
        };
        openButton.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(_currentUrl))
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = _currentUrl,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"打开外部浏览器失败: {ex.Message}");
                }
            }
        };

        contentDisplay.Children.Add(webIcon);
        contentDisplay.Children.Add(webTitle);
        contentDisplay.Children.Add(_urlDisplay);
        contentDisplay.Children.Add(_statusDisplay);
        contentDisplay.Children.Add(openButton);

        _webContentArea.Child = contentDisplay;
        _contentContainer.Children.Add(_webContentArea);

        Content = _contentContainer;
    }

    public void NavigateTo(string url)
    {
        _currentUrl = url;
        _urlDisplay.Text = url;
        _statusDisplay.Text = "页面已加载";
        _statusDisplay.Foreground = Brushes.Green;
        
        Debug.WriteLine($"导航到: {url}");
    }

    public bool CanGoBack => false;
    public bool CanGoForward => false;

    public void GoBack()
    {
        Debug.WriteLine("后退");
        _statusDisplay.Text = "后退功能暂不可用";
        _statusDisplay.Foreground = Brushes.Orange;
    }

    public void GoForward()
    {
        Debug.WriteLine("前进");
        _statusDisplay.Text = "前进功能暂不可用";
        _statusDisplay.Foreground = Brushes.Orange;
    }

    public void Refresh()
    {
        Debug.WriteLine("刷新");
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            _statusDisplay.Text = "页面已刷新";
            _statusDisplay.Foreground = Brushes.Green;
        }
    }
} 