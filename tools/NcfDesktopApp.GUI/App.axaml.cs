using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using NcfDesktopApp.GUI.ViewModels;
using NcfDesktopApp.GUI.Views;
using AvaloniaWebView;

namespace NcfDesktopApp.GUI;

public partial class App : Application
{
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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var viewModel = new MainWindowViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = viewModel,
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
                    mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                    mainWindow.Activate();
                }
            };

            viewModel.ShowDesktopRobotRequested = () =>
            {
                if (!robotWindow.IsVisible)
                {
                    robotWindow.Show();
                }
                robotWindow.Activate();
            };

            mainWindow.Opened += (_, _) => robotWindow.Show();
            mainWindow.Closed += (_, _) => robotWindow.Close();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
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
