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
using System.Text.Json;

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
    private Border _previewArea = null!;

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
            Text = "正在初始化预览界面...",
            FontSize = 12,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkSlateBlue
        };

        statusArea.Child = _statusText;

        // 预览区域
        _webViewContainer = new StackPanel
        {
            Spacing = 15
        };

        _previewArea = new Border
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
            Text = "🌐 NCF 应用预览",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkBlue
        };

        var descText = new TextBlock
        {
            Text = "正在初始化预览界面...",
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
        content.Children.Add(_previewArea);

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
                    await InitializePreviewAsync();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"预览初始化失败: {ex.Message}", Brushes.Red);
                    ShowFallbackView();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"预览初始化异常: {ex.Message}");
            ShowFallbackView();
        }
    }

    private async Task InitializePreviewAsync()
    {
        try
        {
            UpdateStatus("正在初始化预览界面...", Brushes.Blue);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isWebViewReady = true;
                UpdateStatus("预览界面已就绪", Brushes.Green);
                
                // 启用控制按钮
                _refreshButton.IsEnabled = true;
                _openExternalButton.IsEnabled = true;

                // 如果有初始 URL，则获取预览信息
                if (!string.IsNullOrEmpty(Source))
                {
                    _ = FetchPreviewAsync(Source);
                }
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"预览初始化失败: {ex.Message}", Brushes.Red);
            throw;
        }
    }

    private async Task FetchPreviewAsync(string url)
    {
        if (!_isWebViewReady || string.IsNullOrEmpty(url))
            return;

        try
        {
            OnNavigationStarted(url);
            UpdateStatus("正在获取应用信息...", Brushes.Blue);

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(url, cts.Token);
            var content = await response.Content.ReadAsStringAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _currentUrl = url;
                _urlTextBox.Text = url;
                ShowPreviewInfo(content, response, url);
                UpdateStatus("应用信息获取成功", Brushes.Green);
                OnNavigationCompleted(url);
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowErrorInfo(url, ex.Message);
                UpdateStatus("获取应用信息失败", Brushes.Red);
                OnNavigationFailed(ex.Message);
            });
        }
    }

    private void ShowPreviewInfo(string htmlContent, HttpResponseMessage response, string url)
    {
        _webViewContainer.Children.Clear();

        var previewContent = new StackPanel
        {
            Spacing = 15
        };

        var previewBorder = new Border
        {
            Padding = new Thickness(20),
            Child = previewContent
        };

        // 标题
        var titleText = new TextBlock
        {
            Text = "🌐 NCF 应用状态",
            FontSize = 20,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkBlue,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // 状态信息
        var statusInfo = CreateStatusInfoPanel(response, url);
        
        // 页面预览
        var pagePreview = CreatePagePreviewPanel(htmlContent);
        
        // 快速操作按钮
        var actionButtons = CreateActionButtonsPanel(url);

        previewContent.Children.Add(titleText);
        previewContent.Children.Add(statusInfo);
        previewContent.Children.Add(pagePreview);
        previewContent.Children.Add(actionButtons);

        _webViewContainer.Children.Add(previewBorder);
    }

    private Border CreateStatusInfoPanel(HttpResponseMessage response, string url)
    {
        var statusPanel = new StackPanel
        {
            Spacing = 8
        };

        var statusBorder = new Border
        {
            Background = Brushes.LightGreen,
            BorderBrush = Brushes.Green,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15),
            Child = statusPanel
        };

        var urlText = new TextBlock
        {
            Text = $"📍 地址：{url}",
            FontSize = 12,
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            Foreground = Brushes.DarkGreen
        };

        var statusText = new TextBlock
        {
            Text = $"✅ 状态：{response.StatusCode} ({(int)response.StatusCode})",
            FontSize = 12,
            Foreground = Brushes.DarkGreen
        };

        var sizeText = new TextBlock
        {
            Text = $"📊 大小：{response.Content.Headers.ContentLength?.ToString() ?? "未知"} 字节",
            FontSize = 12,
            Foreground = Brushes.DarkGreen
        };

        statusPanel.Children.Add(urlText);
        statusPanel.Children.Add(statusText);
        statusPanel.Children.Add(sizeText);

        return statusBorder;
    }

    private Border CreatePagePreviewPanel(string htmlContent)
    {
        var previewPanel = new StackPanel
        {
            Spacing = 8
        };

        var previewBorder = new Border
        {
            Background = Brushes.LightYellow,
            BorderBrush = Brushes.Orange,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15),
            Child = previewPanel
        };

        var titleText = new TextBlock
        {
            Text = "📄 页面信息",
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Brushes.DarkOrange
        };

        // 提取页面标题
        var title = ExtractPageTitle(htmlContent);
        var titleInfo = new TextBlock
        {
            Text = $"标题：{title}",
            FontSize = 12,
            Foreground = Brushes.DarkOrange,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        // 检测框架
        var frameworks = DetectFrameworks(htmlContent);
        var frameworkText = new TextBlock
        {
            Text = $"框架：{string.Join(", ", frameworks)}",
            FontSize = 12,
            Foreground = Brushes.DarkOrange
        };

        previewPanel.Children.Add(titleText);
        previewPanel.Children.Add(titleInfo);
        previewPanel.Children.Add(frameworkText);

        return previewBorder;
    }

    private Border CreateActionButtonsPanel(string url)
    {
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var buttonBorder = new Border
        {
            Background = Brushes.LightBlue,
            BorderBrush = Brushes.Blue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15),
            Child = buttonPanel
        };

        var openButton = new Button
        {
            Content = "🌍 在浏览器中打开",
            Padding = new Thickness(15, 8),
            Background = Brushes.Blue,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4)
        };
        openButton.Click += (s, e) => OpenInExternalBrowser(url);

        var refreshButton = new Button
        {
            Content = "🔄 刷新状态",
            Padding = new Thickness(15, 8),
            Background = Brushes.Green,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4)
        };
        refreshButton.Click += (s, e) => _ = FetchPreviewAsync(url);

        buttonPanel.Children.Add(openButton);
        buttonPanel.Children.Add(refreshButton);

        return buttonBorder;
    }

    private void ShowErrorInfo(string url, string errorMessage)
    {
        _webViewContainer.Children.Clear();

        var errorContent = new StackPanel
        {
            Spacing = 15
        };

        var errorBorder = new Border
        {
            Padding = new Thickness(20),
            Child = errorContent
        };

        var errorTitle = new TextBlock
        {
            Text = "❌ 连接失败",
            FontSize = 20,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Red,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var errorInfo = new Border
        {
            Background = Brushes.MistyRose,
            BorderBrush = Brushes.Red,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"📍 地址：{url}",
                        FontSize = 12,
                        Foreground = Brushes.DarkRed
                    },
                    new TextBlock
                    {
                        Text = $"❌ 错误：{errorMessage}",
                        FontSize = 12,
                        Foreground = Brushes.DarkRed,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    }
                }
            }
        };

        var suggestionText = new TextBlock
        {
            Text = "💡 建议：\n• 确认 NCF 应用正在运行\n• 检查端口号是否正确\n• 尝试手动启动 NCF 应用",
            FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var retryButton = new Button
        {
            Content = "🔄 重试",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
            Background = Brushes.Orange,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4)
        };
        retryButton.Click += (s, e) => _ = FetchPreviewAsync(url);

        errorContent.Children.Add(errorTitle);
        errorContent.Children.Add(errorInfo);
        errorContent.Children.Add(suggestionText);
        errorContent.Children.Add(retryButton);

        _webViewContainer.Children.Add(errorBorder);
    }

    private string ExtractPageTitle(string htmlContent)
    {
        try
        {
            var titleStart = htmlContent.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
            if (titleStart >= 0)
            {
                titleStart += 7;
                var titleEnd = htmlContent.IndexOf("</title>", titleStart, StringComparison.OrdinalIgnoreCase);
                if (titleEnd > titleStart)
                {
                    return htmlContent.Substring(titleStart, titleEnd - titleStart).Trim();
                }
            }
        }
        catch { }
        
        return "未找到标题";
    }

    private System.Collections.Generic.List<string> DetectFrameworks(string htmlContent)
    {
        var frameworks = new System.Collections.Generic.List<string>();
        var lowerContent = htmlContent.ToLowerInvariant();
        
        if (lowerContent.Contains("bootstrap"))
            frameworks.Add("Bootstrap");
        if (lowerContent.Contains("jquery"))
            frameworks.Add("jQuery");
        if (lowerContent.Contains("vue"))
            frameworks.Add("Vue.js");
        if (lowerContent.Contains("angular"))
            frameworks.Add("Angular");
        if (lowerContent.Contains("react"))
            frameworks.Add("React");
        if (lowerContent.Contains("asp.net"))
            frameworks.Add("ASP.NET");
        if (lowerContent.Contains("razor"))
            frameworks.Add("Razor");
        
        return frameworks.Count > 0 ? frameworks : new System.Collections.Generic.List<string> { "未检测到" };
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
            _ = FetchPreviewAsync(Source);
        }
    }

    public async Task NavigateTo(string url)
    {
        await FetchPreviewAsync(url);
    }

    private void OnBackClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateStatus("后退功能暂不可用", Brushes.Blue);
    }

    private void OnForwardClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateStatus("前进功能暂不可用", Brushes.Blue);
    }

    private void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            _ = FetchPreviewAsync(_currentUrl);
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
            UpdateStatus("已在外部浏览器中打开", Brushes.Blue);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开外部浏览器失败: {ex.Message}");
            UpdateStatus("打开外部浏览器失败", Brushes.Red);
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
            Text = "❌ 预览界面初始化失败",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Red
        };

        var descText = new TextBlock
        {
            Text = "无法加载预览界面。\n请使用外部浏览器打开 NCF 应用。",
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
        _httpClient?.Dispose();
    }
} 