using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NcfDesktopApp.GUI.ViewModels;

namespace NcfDesktopApp.GUI.Views;

public partial class BrowserView : UserControl
{
    private MainWindowViewModel? _viewModel;

    public BrowserView()
    {
        InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
        this.Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel != null)
        {
            _viewModel.BrowserViewReference = this;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 初始化浏览器占位符
        InitializeBrowser();
    }

    private void InitializeBrowser()
    {
        try
        {
            // 通知ViewModel浏览器已准备就绪
            _viewModel?.OnBrowserReady();
        }
        catch (Exception ex)
        {
            // 通知ViewModel浏览器初始化失败
            _viewModel?.OnBrowserError($"浏览器初始化失败: {ex.Message}");
        }
    }

    public async Task NavigateToUrl(string url)
    {
        // 暂时只是模拟导航
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                _viewModel?.OnNavigationStarted(url);
                // 这里将来会实现真正的导航
                await Task.Delay(1000); // 模拟加载时间
                _viewModel?.OnNavigationCompleted(url);
            }
            catch (Exception ex)
            {
                _viewModel?.OnBrowserError($"导航失败: {ex.Message}");
            }
        }
    }

    public bool CanGoBack => false; // 暂时不支持
    public bool CanGoForward => false; // 暂时不支持

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        // 暂时不实现
    }

    private void ForwardButton_Click(object? sender, RoutedEventArgs e)
    {
        // 暂时不实现
    }

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        // 暂时不实现
    }
} 