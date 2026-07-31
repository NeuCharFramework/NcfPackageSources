using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NcfDesktopApp.GUI.Views;

public partial class WorkspaceSettingsWindow : Window
{
    public WorkspaceSettingsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
