using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NcfDesktopApp.GUI.Views;

public partial class TemplateWorkspaceWindow : Window
{
    public TemplateWorkspaceWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
