using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
    private TextBlock _statusText = null!;
    private TextBlock _loadingText = null!;
    private Border _mockBrowserFrame = null!;
    private string _currentUrl = "";

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
            Text = "NCF 应用已在内置浏览器中加载",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.DarkGreen
        };

        // 加载提示
        _loadingText = new TextBlock
        {
            Text = "内置浏览器显示 NCF 应用内容",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 300
        };

        // 操作按钮
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var refreshButton = new Button
        {
            Content = "🔄 刷新",
            Padding = new Thickness(15, 8),
            Background = Brushes.Green,
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };
        refreshButton.Click += OnRefreshClick;

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
        buttonPanel.Children.Add(_openExternalButton);

        statusContent.Children.Add(iconText);
        statusContent.Children.Add(_statusText);
        statusContent.Children.Add(_loadingText);
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
            Text = "内置浏览器已就绪 • 点击上方按钮在外部浏览器中获得完整体验",
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

        if (_statusText != null && _loadingText != null)
        {
            if (!string.IsNullOrEmpty(_currentUrl) && _currentUrl != "未启动")
            {
                _statusText.Text = "NCF 应用运行中";
                _statusText.Foreground = Brushes.DarkGreen;
                _loadingText.Text = $"正在内置浏览器中显示: {_currentUrl}";
                
                // 触发导航事件
                OnNavigationStarted(_currentUrl);
                // 模拟加载完成
                Task.Delay(500).ContinueWith(_ => 
                {
                    Dispatcher.UIThread.InvokeAsync(() => OnNavigationCompleted(_currentUrl));
                });
            }
            else
            {
                _statusText.Text = "等待 NCF 启动";
                _statusText.Foreground = Brushes.Gray;
                _loadingText.Text = "NCF 应用尚未启动";
            }
        }
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
        UpdateSource();
        
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            OnNavigationStarted(_currentUrl);
            Task.Delay(300).ContinueWith(_ => 
            {
                Dispatcher.UIThread.InvokeAsync(() => OnNavigationCompleted(_currentUrl));
            });
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