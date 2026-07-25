/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopRobotWindow.axaml.cs
    文件功能描述：桌面机器人窗口定位、拖动和主窗口唤起

    创建标识：Senparc - 20260725
----------------------------------------------------------------*/

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace NcfDesktopApp.GUI.Views;

public partial class DesktopRobotWindow : Window
{
    public DesktopRobotWindow()
    {
        InitializeComponent();
        Opened += (_, _) => PositionNearWorkingAreaCorner();
    }

    public Action? OpenMainWindowRequested { get; set; }

    private void PositionNearWorkingAreaCorner()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null)
        {
            return;
        }

        var workingArea = screen.WorkingArea;
        var scaling = screen.Scaling > 0 ? screen.Scaling : 1;
        var widthPixels = (int)Math.Round(Width * scaling);
        var heightPixels = (int)Math.Round(Height * scaling);
        const int margin = 18;
        Position = new PixelPoint(
            workingArea.Right - widthPixels - margin,
            workingArea.Bottom - heightPixels - margin);
    }

    private void RootBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void HideButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OpenMainButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenMainWindowRequested?.Invoke();
    }
}
