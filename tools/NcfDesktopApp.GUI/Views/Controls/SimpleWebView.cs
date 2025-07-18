using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace NcfDesktopApp.GUI.Views.Controls;

public partial class SimpleWebView : UserControl
{
    public static readonly StyledProperty<string> SourceProperty =
        AvaloniaProperty.Register<SimpleWebView, string>(nameof(Source), "");

    public string Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private Border _contentBorder = null!;
    private StackPanel _content = null!;
    private TextBlock _urlDisplay = null!;
    private Button _openExternalButton = null!;
    private Button _switchModeButton = null!;
    private TextBlock _statusText = null!;
    private TextBlock _loadingText = null!;
    private TextBlock _pageInfoText = null!;
    private Border _mockBrowserFrame = null!;
    private static readonly HttpClient _httpClient = new();
    private string _currentUrl = "";
    private bool _isShowingRealContent = false;

    public SimpleWebView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _content = new StackPanel
        {
            Spacing = 10
        };

        // 创建模拟的浏览器框架
        CreateMockBrowserFrame();

        _contentBorder = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = _content
        };

        Content = _contentBorder;
    }

    private void CreateMockBrowserFrame()
    {
        // 创建模拟的浏览器界面
        var browserFrame = new StackPanel
        {
            Spacing = 10
        };

        // 顶部地址栏
        var addressBar = new Border
        {
            Background = Brushes.LightGray,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 5)
        };

        _urlDisplay = new TextBlock
        {
            FontFamily = "Consolas",
            FontSize = 12,
            Foreground = Brushes.DarkBlue,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        addressBar.Child = _urlDisplay;

        // 网站状态区域
        var statusArea = new Border
        {
            Background = Brushes.AliceBlue,
            BorderBrush = Brushes.SkyBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(20),
            MinHeight = 200
        };

        var statusContent = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 15
        };

        // 网站图标
        var iconText = new TextBlock
        {
            Text = "🌐",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        // 状态文本
        _statusText = new TextBlock
        {
            Text = "内嵌浏览器 - NCF应用预览",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkGreen
        };

        // 加载提示
        _loadingText = new TextBlock
        {
            Text = "点击\"显示网页内容\"查看真实页面",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        };

        // 页面信息显示容器
        var pageInfoBorder = new Border
        {
            Background = Brushes.WhiteSmoke,
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(4),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1)
        };

        _pageInfoText = new TextBlock
        {
            FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkSlateGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 500
        };

        pageInfoBorder.Child = _pageInfoText;

        // 操作按钮
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var refreshButton = new Button
        {
            Content = "🔄 刷新页面信息",
            Padding = new Thickness(15, 8),
            Background = Brushes.Green,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };
        refreshButton.Click += OnRefreshClick;

        _switchModeButton = new Button
        {
            Content = "📄 显示网页内容",
            Padding = new Thickness(15, 8),
            Background = Brushes.DodgerBlue,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };
        _switchModeButton.Click += OnSwitchModeClick;

        _openExternalButton = new Button
        {
            Content = "🌍 在外部浏览器中打开",
            Padding = new Thickness(15, 8),
            Background = Brushes.Orange,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };
        _openExternalButton.Click += OnOpenExternalClick;

        buttonPanel.Children.Add(refreshButton);
        buttonPanel.Children.Add(_switchModeButton);
        buttonPanel.Children.Add(_openExternalButton);

        statusContent.Children.Add(iconText);
        statusContent.Children.Add(_statusText);
        statusContent.Children.Add(_loadingText);
        statusContent.Children.Add(pageInfoBorder);
        statusContent.Children.Add(buttonPanel);

        statusArea.Child = statusContent;

        // 底部信息栏
        var infoBar = new Border
        {
            Background = Brushes.LightGray,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(10, 5)
        };

        var infoText = new TextBlock
        {
            Text = "混合模式：页面信息 + 真实内容预览 • 点击上方按钮切换显示模式",
            FontSize = 11,
            Foreground = Brushes.DarkGray,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        infoBar.Child = infoText;

        // 组装浏览器框架
        browserFrame.Children.Add(addressBar);
        browserFrame.Children.Add(statusArea);
        browserFrame.Children.Add(infoBar);

        _mockBrowserFrame = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Child = browserFrame
        };

        _content.Children.Add(_mockBrowserFrame);
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
        
        if (_urlDisplay != null)
        {
            _urlDisplay.Text = string.IsNullOrEmpty(_currentUrl) ? "未设置地址" : _currentUrl;
        }
        
        if (_openExternalButton != null)
        {
            _openExternalButton.IsEnabled = !string.IsNullOrEmpty(_currentUrl);
        }

        if (_switchModeButton != null)
        {
            _switchModeButton.IsEnabled = !string.IsNullOrEmpty(_currentUrl) && _currentUrl != "未启动";
        }

        if (_statusText != null && _loadingText != null)
        {
            if (!string.IsNullOrEmpty(_currentUrl) && _currentUrl != "未启动")
            {
                _statusText.Text = "NCF 应用运行中";
                _statusText.Foreground = Brushes.DarkGreen;
                _loadingText.Text = "点击\"显示网页内容\"查看真实页面";
                
                // 异步获取页面信息
                _ = FetchPageInfoAsync(_currentUrl);
                
                // 触发导航事件
                OnNavigationStarted(_currentUrl);
            }
            else
            {
                _statusText.Text = "等待 NCF 启动";
                _statusText.Foreground = Brushes.Gray;
                _loadingText.Text = "NCF 应用尚未启动";
                if (_pageInfoText != null)
                {
                    _pageInfoText.Text = "";
                    _pageInfoText.IsVisible = false;
                }
            }
        }
    }

    private async Task FetchPageInfoAsync(string url)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_loadingText != null && !_isShowingRealContent)
                {
                    _loadingText.Text = "正在连接 NCF 应用...";
                }
            });

            // 添加超时控制
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            var content = await response.Content.ReadAsStringAsync();
            var pageInfo = ExtractPageInfo(content, response);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_loadingText != null && !_isShowingRealContent)
                {
                    _loadingText.Text = "✅ 成功连接到 NCF 应用";
                    _loadingText.Foreground = Brushes.Green;
                }

                if (_pageInfoText != null)
                {
                    _pageInfoText.Text = pageInfo;
                    _pageInfoText.IsVisible = true;
                }

                OnNavigationCompleted(url);
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_loadingText != null && !_isShowingRealContent)
                {
                    _loadingText.Text = $"⚠️ 连接失败: {ex.Message}";
                    _loadingText.Foreground = Brushes.Red;
                }

                if (_pageInfoText != null)
                {
                    _pageInfoText.Text = "无法获取页面信息，建议使用外部浏览器访问完整功能";
                    _pageInfoText.IsVisible = true;
                    _pageInfoText.Foreground = Brushes.Orange;
                }

                OnNavigationFailed($"页面加载失败: {ex.Message}");
            });
        }
    }

    private string ExtractPageInfo(string htmlContent, HttpResponseMessage response)
    {
        var info = new StringBuilder();
        
        // 提取页面标题
        var titleMatch = Regex.Match(htmlContent, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            var title = titleMatch.Groups[1].Value.Trim();
            info.AppendLine($"📄 页面标题: {title}");
        }

        // HTTP状态信息
        info.AppendLine($"🔗 状态: {response.StatusCode} ({(int)response.StatusCode})");
        
        // 内容类型
        if (response.Content.Headers.ContentType != null)
        {
            info.AppendLine($"📁 内容类型: {response.Content.Headers.ContentType.MediaType}");
        }

        // 内容长度
        if (response.Content.Headers.ContentLength.HasValue)
        {
            var sizeKb = response.Content.Headers.ContentLength.Value / 1024.0;
            info.AppendLine($"📊 页面大小: {sizeKb:F1} KB");
        }

        // 检测是否包含常见的框架
        if (htmlContent.Contains("bootstrap", StringComparison.OrdinalIgnoreCase))
        {
            info.AppendLine("🎨 检测到: Bootstrap");
        }
        if (htmlContent.Contains("jquery", StringComparison.OrdinalIgnoreCase))
        {
            info.AppendLine("⚡ 检测到: jQuery");
        }
        if (htmlContent.Contains("vue", StringComparison.OrdinalIgnoreCase))
        {
            info.AppendLine("🖼️ 检测到: Vue.js");
        }
        if (htmlContent.Contains("angular", StringComparison.OrdinalIgnoreCase))
        {
            info.AppendLine("🅰️ 检测到: Angular");
        }

        // 提取描述信息
        var descMatch = Regex.Match(htmlContent, @"<meta[^>]*name=[""']description[""'][^>]*content=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        if (descMatch.Success)
        {
            var desc = descMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(desc))
            {
                info.AppendLine($"📝 描述: {desc.Substring(0, Math.Min(desc.Length, 100))}...");
            }
        }

        var result = info.ToString().Trim();
        return string.IsNullOrEmpty(result) ? "成功连接到 NCF 应用，页面加载正常" : result;
    }

    private void OnSwitchModeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentUrl) || _currentUrl == "未启动")
            return;

        _isShowingRealContent = !_isShowingRealContent;

        if (_isShowingRealContent)
        {
            ShowRealWebContent();
        }
        else
        {
            ShowPageInfo();
        }
    }

    private void ShowRealWebContent()
    {
        try
        {
            // 生成包含iframe的HTML页面
            var html = GenerateEmbeddedHtml(_currentUrl);
            
            // 创建临时HTML文件
            var tempPath = Path.GetTempFileName() + ".html";
            File.WriteAllText(tempPath, html, Encoding.UTF8);

            // 在系统默认浏览器中打开（这是一个临时解决方案）
            // 注意：真正的WebView需要Avalonia Accelerate或WebViewControl-Avalonia
            OpenInExternalBrowser($"file://{tempPath}");

            // 更新界面状态
            if (_statusText != null)
            {
                _statusText.Text = "正在外部浏览器中显示网页内容";
                _statusText.Foreground = Brushes.Blue;
            }

            if (_loadingText != null)
            {
                _loadingText.Text = "💡 真正的内嵌显示需要Avalonia Accelerate WebView";
                _loadingText.Foreground = Brushes.Orange;
            }

            if (_switchModeButton != null)
            {
                _switchModeButton.Content = "📊 显示页面信息";
            }

            // 清理临时文件（延迟删除）
            Task.Delay(5000).ContinueWith(_ =>
            {
                try { File.Delete(tempPath); } catch { }
            });
        }
        catch (Exception ex)
        {
            if (_loadingText != null)
            {
                _loadingText.Text = $"❌ 无法生成网页内容: {ex.Message}";
                _loadingText.Foreground = Brushes.Red;
            }
        }
    }

    private void ShowPageInfo()
    {
        if (_statusText != null)
        {
            _statusText.Text = "NCF 应用运行中";
            _statusText.Foreground = Brushes.DarkGreen;
        }

        if (_loadingText != null)
        {
            _loadingText.Text = "✅ 显示页面分析信息";
            _loadingText.Foreground = Brushes.Green;
        }

        if (_switchModeButton != null)
        {
            _switchModeButton.Content = "📄 显示网页内容";
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
    <title>NCF 应用 - 内嵌预览</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #f5f5f5;
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
        .notice {{
            background: #FF9800;
            color: white;
            padding: 8px 20px;
            text-align: center;
            font-size: 14px;
        }}
        iframe {{
            width: 100%;
            height: calc(100vh - 100px);
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
            <strong>NCF 桌面应用 - 内嵌浏览器预览</strong>
        </div>
        <div class='url'>{ncfUrl}</div>
    </div>
    <div class='notice'>
        💡 这是临时预览方案。如需真正的内嵌显示，推荐使用 Avalonia Accelerate WebView
    </div>
    <div class='loading' id='loading'>
        正在加载 NCF 应用...<br>
        <small>如果长时间无响应，请检查 NCF 是否正常运行</small>
    </div>
    <iframe src='{ncfUrl}' onload='document.getElementById(""loading"").style.display=""none""' onerror='showError()'></iframe>
    
    <script>
        function showError() {{
            document.getElementById('loading').innerHTML = '❌ 加载失败<br><small>请确认 NCF 应用正在运行</small>';
            document.getElementById('loading').style.display = 'block';
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

    private void OnOpenExternalClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            OpenInExternalBrowser(_currentUrl);
        }
    }

    private void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // 刷新当前显示
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            OnNavigationStarted(_currentUrl);
            _ = FetchPageInfoAsync(_currentUrl);
        }
    }

    public async Task NavigateTo(string url)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Source = url;
        });
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
        }
        catch (Exception ex)
        {
            // 可以通过事件向上传递错误信息
            Debug.WriteLine($"无法打开外部浏览器: {ex.Message}");
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