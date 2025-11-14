using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NcfDesktopApp.GUI.Views.Controls;

namespace NcfDesktopApp.GUI.Views;

public partial class BrowserView : UserControl
{
    private EmbeddedWebView? WebView => this.FindControl<EmbeddedWebView>("WebViewControl");

    public BrowserView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel viewModel)
        {
            viewModel.BrowserViewReference = this;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 设置WebView事件处理
        if (WebView != null)
        {
            WebView.NavigationStarted += OnNavigationStarted;
            WebView.NavigationCompleted += OnNavigationCompleted;
            WebView.NavigationFailed += OnNavigationFailed;
        }
        
        // 初始化按钮状态
        UpdateNavigationButtons();
    }

    public async Task NavigateToUrl(string url)
    {
        if (WebView != null)
        {
            // 等待 WebView 初始化，最多等待 5 秒
            var maxRetries = 50; // 5 秒 (50 * 100ms)
            var retryCount = 0;
            
            while (!WebView.IsWebViewReady && retryCount < maxRetries)
            {
                await Task.Delay(100);
                retryCount++;
            }
            
            // 即使超时也尝试导航（可能已经初始化好了）
            await WebView.NavigateTo(url);
        }
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        WebView?.GoBack();
    }

    private void ForwardButton_Click(object? sender, RoutedEventArgs e)
    {
        WebView?.GoForward();
    }

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WebView != null)
        {
            WebView.Refresh();
        }
    }

    private async void UrlTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await NavigateToUrlFromTextBox();
        }
    }

    private async void GoButton_Click(object? sender, RoutedEventArgs e)
    {
        await NavigateToUrlFromTextBox();
    }

    private async Task NavigateToUrlFromTextBox()
    {
        var textBox = this.FindControl<TextBox>("UrlTextBox");
        if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            var url = textBox.Text.Trim();
            
            // 如果 URL 不包含协议，自动添加 http://
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }
            
            // 更新 ViewModel 中的 URL
            if (DataContext is ViewModels.MainWindowViewModel viewModel)
            {
                viewModel.SiteUrl = url;
            }
            
            // 导航到新 URL
            await NavigateToUrl(url);
        }
    }

    private void OnNavigationStarted(object? sender, string url)
    {
        if (DataContext is ViewModels.MainWindowViewModel viewModel)
        {
            viewModel.OnNavigationStarted(url);
        }
    }

    private void OnNavigationCompleted(object? sender, string url)
    {
        if (DataContext is ViewModels.MainWindowViewModel viewModel)
        {
            viewModel.OnNavigationCompleted(url);
            // 同步更新地址栏显示
            viewModel.SiteUrl = url;
        }
        
        // 同步更新地址栏 TextBox（因为使用了 OneWay 绑定，需要手动更新）
        var textBox = this.FindControl<TextBox>("UrlTextBox");
        if (textBox != null)
        {
            textBox.Text = url;
        }
        
        UpdateNavigationButtons();
    }

    private void OnNavigationFailed(object? sender, string error)
    {
        if (DataContext is ViewModels.MainWindowViewModel viewModel)
        {
            viewModel.OnBrowserError(error);
        }
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        var backButton = this.FindControl<Button>("BackButton");
        var forwardButton = this.FindControl<Button>("ForwardButton");
        
        if (backButton != null && WebView != null)
        {
            backButton.IsEnabled = WebView.CanGoBack;
        }
        
        if (forwardButton != null && WebView != null)
        {
            forwardButton.IsEnabled = WebView.CanGoForward;
        }
    }
} 