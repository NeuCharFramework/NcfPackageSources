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
        
        // 初始化按钮状态
        UpdateNavigationButtons();
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