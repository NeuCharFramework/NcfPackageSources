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
            await WebView.NavigateTo(url);
        }
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        // 暂时不实现历史记录功能
        // 可以在后续版本中添加
    }

    private void ForwardButton_Click(object? sender, RoutedEventArgs e)
    {
        // 暂时不实现历史记录功能
        // 可以在后续版本中添加
    }

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WebView != null && DataContext is ViewModels.MainWindowViewModel viewModel)
        {
            var url = viewModel.SiteUrl;
            if (!string.IsNullOrEmpty(url) && url != "未启动")
            {
                _ = WebView.NavigateTo(url);
            }
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