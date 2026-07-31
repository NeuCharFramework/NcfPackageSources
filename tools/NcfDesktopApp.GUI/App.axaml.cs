using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using NcfDesktopApp.GUI.ViewModels;
using NcfDesktopApp.GUI.Views;
using AvaloniaWebView;
using System.Collections.Generic;

namespace NcfDesktopApp.GUI;

public partial class App : Application
{
    private readonly List<MainWindow> _workspaceWindows = new();

    public override void RegisterServices()
    {
        base.RegisterServices();
        // Initialize WebView.Avalonia; if only WebView is needed, this is sufficient
        AvaloniaWebViewBuilder.Initialize(default);
        // If in the future BlazorWebView is used, uncomment and configure below
        // using AvaloniaBlazorWebView; AvaloniaBlazorWebViewBuilder.Initialize(default);
    }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 每个窗口拥有独立的进程/端口/Bridge 会话；最后一个工作台关闭后才退出。
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = CreateWorkspaceWindow(desktop, showImmediately: false);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow CreateWorkspaceWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        bool showImmediately)
    {
        var viewModel = new MainWindowViewModel();
        var workspaceNumber = _workspaceWindows.Count + 1;
        var mainWindow = new MainWindow
        {
            DataContext = viewModel,
            Title = $"NCF Agent Workspace #{workspaceNumber}"
        };
        var robotWindow = new DesktopRobotWindow
        {
            DataContext = viewModel.Robot,
            OpenMainWindowRequested = () =>
            {
                if (!mainWindow.IsVisible)
                {
                    mainWindow.Show();
                }
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        };
        WorkspaceSettingsWindow? settingsWindow = null;
        TemplateWorkspaceWindow? templateWorkspaceWindow = null;

        viewModel.CreateWorkspaceWindowRequested = () =>
        {
            var newWindow = CreateWorkspaceWindow(desktop, showImmediately: true);
            newWindow.Activate();
        };
        viewModel.ShowDesktopRobotRequested = () =>
        {
            if (!robotWindow.IsVisible)
            {
                robotWindow.Show();
            }
            robotWindow.Activate();
        };
        viewModel.ShowWorkspaceSettingsRequested = () =>
        {
            if (settingsWindow?.IsVisible == true)
            {
                settingsWindow.Activate();
                return;
            }

            settingsWindow = new WorkspaceSettingsWindow
            {
                DataContext = viewModel
            };
            settingsWindow.Closed += (_, _) => settingsWindow = null;
            settingsWindow.Show(mainWindow);
        };
        viewModel.ShowTemplateWorkspaceRequested = () =>
        {
            if (templateWorkspaceWindow?.IsVisible == true)
            {
                templateWorkspaceWindow.Activate();
                return;
            }

            templateWorkspaceWindow = new TemplateWorkspaceWindow
            {
                DataContext = viewModel
            };
            templateWorkspaceWindow.Closed += (_, _) => templateWorkspaceWindow = null;
            templateWorkspaceWindow.Show(mainWindow);
        };

        mainWindow.Opened += (_, _) => robotWindow.Show();
        mainWindow.Closed += (_, _) =>
        {
            if (robotWindow.IsVisible)
            {
                robotWindow.Close();
            }
            _workspaceWindows.Remove(mainWindow);
        };
        _workspaceWindows.Add(mainWindow);

        if (showImmediately)
        {
            mainWindow.Show();
        }

        return mainWindow;
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
