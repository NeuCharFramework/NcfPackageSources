using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform;
using System.Net.Http;
using System.Text;
using System.IO;

namespace NcfDesktopApp.GUI.Views.Controls;

public partial class RealWebView : UserControl
{
    public static readonly StyledProperty<string> SourceProperty =
        AvaloniaProperty.Register<RealWebView, string>(nameof(Source), "");

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
    private Button _openEmbeddedButton = null!;

    public RealWebView()
    {
        InitializeComponent();
        _ = InitializeWebViewAsync();
    }

    private void InitializeComponent()
    {
        var content = new StackPanel
        {
            Spacing = 15,
            Margin = new Thickness(20)
        };

        // 状态显示
        var statusArea = new Border
        {
            Background = Brushes.LightBlue,
            BorderBrush = Brushes.SteelBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(20),
            MinHeight = 120
        };

        var statusContent = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 10
        };

        var iconText = new TextBlock
        {
            Text = "🌐",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        _statusText = new TextBlock
        {
            Text = "正在初始化内嵌浏览器...",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkSlateBlue
        };

        statusContent.Children.Add(iconText);
        statusContent.Children.Add(_statusText);
        statusArea.Child = statusContent;

        // 按钮区域
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10,
            Margin = new Thickness(0, 15, 0, 0)
        };

        _refreshButton = new Button
        {
            Content = "🔄 刷新",
            Padding = new Thickness(15, 8),
            Background = Brushes.Green,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            IsEnabled = false
        };
        _refreshButton.Click += OnRefreshClick;

        _openEmbeddedButton = new Button
        {
            Content = "📱 内嵌显示",
            Padding = new Thickness(15, 8),
            Background = Brushes.RoyalBlue,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            IsEnabled = false
        };
        _openEmbeddedButton.Click += OnOpenEmbeddedClick;

        _openExternalButton = new Button
        {
            Content = "🌍 外部浏览器",
            Padding = new Thickness(15, 8),
            Background = Brushes.Orange,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            IsEnabled = false
        };
        _openExternalButton.Click += OnOpenExternalClick;

        buttonPanel.Children.Add(_refreshButton);
        buttonPanel.Children.Add(_openEmbeddedButton);
        buttonPanel.Children.Add(_openExternalButton);

        content.Children.Add(statusArea);
        content.Children.Add(buttonPanel);

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
                    await InitializeEmbeddedViewAsync();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"WebView初始化失败: {ex.Message}", Brushes.Red);
                    ShowFallbackView();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebView初始化异常: {ex.Message}");
            ShowFallbackView();
        }
    }

    private async Task InitializeEmbeddedViewAsync()
    {
        try
        {
            UpdateStatus("正在创建内嵌预览...", Brushes.Orange);
            
            // 创建一个可以显示HTML内容的区域
            var embedContainer = new ScrollViewer
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 10, 10, 24),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var contentArea = new StackPanel
            {
                Spacing = 15
            };

            var welcomeText = new TextBlock
            {
                Text = "📱 智能内嵌浏览器",
                FontSize = 18,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Foreground = Brushes.DarkBlue
            };

            var descText = new TextBlock
            {
                Text = "点击\"内嵌显示\"按钮在独立窗口中显示NCF应用\n或使用\"外部浏览器\"获得完整体验",
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            };

            contentArea.Children.Add(welcomeText);
            contentArea.Children.Add(descText);
            
            embedContainer.Content = contentArea;
            _contentBorder.Child = embedContainer;

            UpdateStatus("✅ 内嵌浏览器已就绪", Brushes.Green);
            _isWebViewReady = true;

            // 如果有URL，获取页面信息
            if (!string.IsNullOrEmpty(_currentUrl) && _currentUrl != "未启动")
            {
                await FetchPageInfoAsync(_currentUrl, contentArea);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"创建预览失败: {ex.Message}", Brushes.Red);
            ShowFallbackView();
        }
    }

    private async Task FetchPageInfoAsync(string url, StackPanel container)
    {
        try
        {
            UpdateStatus("正在连接 NCF 应用...", Brushes.Blue);

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await _httpClient.GetAsync(url, cts.Token);
            var content = await response.Content.ReadAsStringAsync();

            // 创建页面信息显示
            var infoPanel = new Border
            {
                Background = Brushes.LightYellow,
                BorderBrush = Brushes.Orange,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var infoText = new TextBlock
            {
                Text = ExtractPageInfo(content, response, url),
                FontSize = 13,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = Brushes.DarkOliveGreen
            };

            infoPanel.Child = infoText;
            container.Children.Add(infoPanel);

            UpdateStatus("✅ NCF 应用连接成功", Brushes.Green);
            OnNavigationCompleted(url);
        }
        catch (Exception ex)
        {
            var errorPanel = new Border
            {
                Background = Brushes.MistyRose,
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var errorText = new TextBlock
            {
                Text = $"❌ 连接失败：{ex.Message}\n\n建议：\n• 确认 NCF 应用正在运行\n• 检查端口号是否正确\n• 尝试使用外部浏览器访问",
                FontSize = 13,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = Brushes.DarkRed
            };

            errorPanel.Child = errorText;
            container.Children.Add(errorPanel);

            UpdateStatus("⚠️ 连接失败", Brushes.Red);
            OnNavigationFailed(ex.Message);
        }
    }

    private string ExtractPageInfo(string htmlContent, HttpResponseMessage response, string url)
    {
        var info = new StringBuilder();
        info.AppendLine($"🌐 NCF 应用状态报告");
        info.AppendLine($"📍 地址：{url}");
        info.AppendLine($"✅ 状态：{response.StatusCode} ({(int)response.StatusCode})");
        
        if (response.Content.Headers.ContentLength.HasValue)
        {
            var sizeKb = response.Content.Headers.ContentLength.Value / 1024.0;
            info.AppendLine($"📊 页面大小：{sizeKb:F1} KB");
        }

        // 检测框架
        var frameworks = new System.Collections.Generic.List<string>();
        if (htmlContent.Contains("bootstrap", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("Bootstrap");
        if (htmlContent.Contains("jquery", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("jQuery");
        if (htmlContent.Contains("vue", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("Vue.js");
        if (htmlContent.Contains("angular", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("Angular");

        if (frameworks.Count > 0)
        {
            info.AppendLine($"🎨 检测到：{string.Join(", ", frameworks)}");
        }

        info.AppendLine();
        info.AppendLine($"💡 提示：点击\"内嵌显示\"在独立窗口中查看NCF应用");

        return info.ToString();
    }

    private void ShowFallbackView()
    {
        var fallbackContent = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 15
        };

        fallbackContent.Children.Add(new TextBlock
        {
            Text = "⚠️",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        fallbackContent.Children.Add(new TextBlock
        {
            Text = "浏览器组件不可用",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        fallbackContent.Children.Add(new TextBlock
        {
            Text = "请使用外部浏览器访问 NCF 应用完整功能",
            FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        _contentBorder.Child = fallbackContent;
        UpdateStatus("回退到基础模式", Brushes.Orange);
    }

    private void UpdateStatus(string message, IBrush color)
    {
        if (_statusText != null)
        {
            _statusText.Text = message;
            _statusText.Foreground = color;
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
        _currentUrl = Source ?? "";
        
        bool isValidUrl = !string.IsNullOrEmpty(_currentUrl) && 
                         _currentUrl != "未启动" && 
                         Uri.IsWellFormedUriString(_currentUrl, UriKind.Absolute);
        
        if (_refreshButton != null)
        {
            _refreshButton.IsEnabled = isValidUrl;
        }

        if (_openExternalButton != null)
        {
            _openExternalButton.IsEnabled = isValidUrl;
        }

        if (_openEmbeddedButton != null)
        {
            _openEmbeddedButton.IsEnabled = isValidUrl;
        }

        if (isValidUrl && _isWebViewReady)
        {
            _ = NavigateToUrlAsync(_currentUrl);
        }
        else if (string.IsNullOrEmpty(_currentUrl) || _currentUrl == "未启动")
        {
            UpdateStatus("等待 NCF 启动", Brushes.Gray);
        }
    }

    private async Task NavigateToUrlAsync(string url)
    {
        try
        {
            UpdateStatus($"正在连接到: {url}", Brushes.Blue);
            OnNavigationStarted(url);

            // 更新页面信息
            var currentContent = _contentBorder.Child;
            if (currentContent is ScrollViewer scrollViewer && 
                scrollViewer.Content is StackPanel stackPanel)
            {
                // 清除之前的内容，保留标题
                while (stackPanel.Children.Count > 2)
                {
                    stackPanel.Children.RemoveAt(stackPanel.Children.Count - 1);
                }
                
                await FetchPageInfoAsync(url, stackPanel);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"导航失败: {ex.Message}", Brushes.Red);
            OnNavigationFailed(ex.Message);
        }
    }

    public async Task NavigateTo(string url)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Source = url;
        });
    }

    private void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            _ = NavigateToUrlAsync(_currentUrl);
        }
    }

    private void OnOpenEmbeddedClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            OpenEmbeddedBrowser(_currentUrl);
        }
    }

    private void OnOpenExternalClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            OpenInExternalBrowser(_currentUrl);
        }
    }

    private void OpenEmbeddedBrowser(string url)
    {
        try
        {
            // 创建一个包含iframe的HTML文件
            var htmlContent = GenerateEmbeddedHtml(url);
            var tempPath = Path.GetTempFileName() + ".html";
            File.WriteAllText(tempPath, htmlContent, Encoding.UTF8);

            // 在系统默认浏览器中打开HTML文件
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", tempPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", tempPath);
            }

            UpdateStatus("已在独立窗口中打开内嵌浏览器", Brushes.Blue);
            
            // 延迟删除临时文件
            Task.Delay(5000).ContinueWith(_ =>
            {
                try { File.Delete(tempPath); } catch { }
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"无法打开内嵌浏览器: {ex.Message}", Brushes.Red);
            OnNavigationFailed($"无法打开内嵌浏览器: {ex.Message}");
        }
    }

    private string GenerateEmbeddedHtml(string ncfUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>NCF 应用 - 内嵌浏览器</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #f5f5f5;
            overflow: hidden;
        }}
        .header {{
            background: #2196F3;
            color: white;
            padding: 10px 20px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .url {{
            background: rgba(255,255,255,0.2);
            padding: 5px 10px;
            border-radius: 4px;
            font-family: monospace;
            font-size: 14px;
        }}
        .controls {{
            display: flex;
            gap: 10px;
        }}
        .btn {{
            background: rgba(255,255,255,0.2);
            border: none;
            color: white;
            padding: 5px 10px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 12px;
        }}
        .btn:hover {{
            background: rgba(255,255,255,0.3);
        }}
        iframe {{
            width: 100%;
            height: calc(100vh - 60px);
            border: none;
            display: block;
        }}
        .loading {{
            text-align: center;
            padding: 50px;
            color: #666;
        }}
        .loading::before {{
            content: '🌐';
            font-size: 48px;
            display: block;
            margin-bottom: 20px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <div>
            <strong>NCF 桌面应用 - 内嵌浏览器</strong>
        </div>
        <div class='url'>{ncfUrl}</div>
        <div class='controls'>
            <button class='btn' onclick='refresh()'>🔄 刷新</button>
            <button class='btn' onclick='openExternal()'>🌍 外部</button>
        </div>
    </div>
    <div class='loading' id='loading'>
        正在加载 NCF 应用...<br>
        <small>如果长时间无响应，请检查 NCF 是否正常运行</small>
    </div>
    <iframe src='{ncfUrl}' onload='hideLoading()' onerror='showError()'></iframe>
    
    <script>
        function hideLoading() {{
            document.getElementById('loading').style.display = 'none';
        }}
        
        function showError() {{
            document.getElementById('loading').innerHTML = '❌ 加载失败<br><small>请确认 NCF 应用正在运行</small>';
            document.getElementById('loading').style.display = 'block';
        }}
        
        function refresh() {{
            location.reload();
        }}
        
        function openExternal() {{
            window.open('{ncfUrl}', '_blank');
        }}
        
        // 5秒后自动隐藏加载提示（防止iframe onload不触发）
        setTimeout(function() {{
            var loading = document.getElementById('loading');
            if (loading.style.display !== 'none') {{
                loading.innerHTML = '⚠️ 加载中...<br><small>如果页面空白，可能需要在 NCF 中允许iframe嵌入</small>';
            }}
        }}, 5000);
    </script>
</body>
</html>";
    }

    private void OpenInExternalBrowser(string url)
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

            UpdateStatus("已在外部浏览器中打开", Brushes.Blue);
        }
        catch (Exception ex)
        {
            UpdateStatus($"无法打开外部浏览器: {ex.Message}", Brushes.Red);
            OnNavigationFailed($"无法打开外部浏览器: {ex.Message}");
        }
    }

    // 事件定义
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
}
