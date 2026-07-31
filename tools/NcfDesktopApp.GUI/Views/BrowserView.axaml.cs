using System;
using System.Threading.Tasks;
using Avalonia.Controls;
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

    /// <summary>内嵌浏览器是否已就绪（供主窗口 Edit 快捷键路由）。</summary>
    public bool IsEmbeddedWebViewReady => WebView?.IsWebViewReady == true;

    public Task<bool> WebViewSelectAllAsync() =>
        WebView?.SelectAllAsync() ?? Task.FromResult(false);

    public Task<bool> WebViewCopyAsync() =>
        WebView?.CopyAsync() ?? Task.FromResult(false);

    public Task<bool> WebViewCutAsync() =>
        WebView?.CutAsync() ?? Task.FromResult(false);

    public Task<bool> WebViewPasteAsync() =>
        WebView?.PasteAsync() ?? Task.FromResult(false);

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WebView != null)
        {
            WebView.Refresh();
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
        
    }

    private void OnNavigationFailed(object? sender, string error)
    {
        if (DataContext is ViewModels.MainWindowViewModel viewModel)
        {
            viewModel.OnBrowserError(error);
        }
    }
}
