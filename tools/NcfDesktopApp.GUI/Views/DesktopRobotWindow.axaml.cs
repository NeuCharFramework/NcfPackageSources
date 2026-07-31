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
using Avalonia.Threading;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;
using NcfDesktopApp.GUI.ViewModels;

namespace NcfDesktopApp.GUI.Views;

public partial class DesktopRobotWindow : Window
{
    private readonly DispatcherTimer _globalPointerTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(50)
    };

    public DesktopRobotWindow()
    {
        InitializeComponent();
        _globalPointerTimer.Tick += (_, _) => UpdateGlobalGaze();
        Opened += (_, _) =>
        {
            PositionNearWorkingAreaCorner();
            Robot?.ResetGaze();
            _globalPointerTimer.Start();
        };
        Closed += (_, _) => _globalPointerTimer.Stop();
    }

    public Action? OpenMainWindowRequested { get; set; }

    public Action? VoiceInputRequested { get; set; }

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
            Robot?.ReactToPointer();
            BeginMoveDrag(e);
        }
    }

    private void RootBorder_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        var mascotCenter = MascotView.TranslatePoint(
            new Point(MascotView.Bounds.Width / 2, MascotView.Bounds.Height / 2), this);
        if (!mascotCenter.HasValue)
        {
            return;
        }

        var horizontalRange = Math.Max(1, Bounds.Width * .65);
        var verticalRange = Math.Max(1, Bounds.Height * .65);
        Robot?.UpdateGaze(
            (point.X - mascotCenter.Value.X) / horizontalRange,
            (point.Y - mascotCenter.Value.Y) / verticalRange);
    }

    private void RootBorder_OnPointerExited(object? sender, PointerEventArgs e)
    {
        Robot?.ResetGaze();
    }

    private void UpdateGlobalGaze()
    {
        if (!IsVisible || !GlobalPointerTracker.TryGetScreenPosition(out var pointerPosition))
        {
            return;
        }

        var mascotCenter = MascotView.TranslatePoint(
            new Point(MascotView.Bounds.Width / 2, MascotView.Bounds.Height / 2), this);
        if (!mascotCenter.HasValue)
        {
            return;
        }

        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        var scaling = screen?.Scaling > 0 ? screen.Scaling : 1;
        var mascotCenterOnScreen = new PixelPoint(
            Position.X + (int)Math.Round(mascotCenter.Value.X * scaling),
            Position.Y + (int)Math.Round(mascotCenter.Value.Y * scaling));
        var horizontalRange = Math.Max(1, Bounds.Width * scaling * .65);
        var verticalRange = Math.Max(1, Bounds.Height * scaling * .65);

        Robot?.UpdateGaze(
            (pointerPosition.X - mascotCenterOnScreen.X) / horizontalRange,
            (pointerPosition.Y - mascotCenterOnScreen.Y) / verticalRange);
    }

    private void HideButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OpenMainButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenMainWindowRequested?.Invoke();
    }

    private void MascotAutoMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        Robot?.UseAutomaticMascot();
    }

    private void VoiceInputButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Robot?.ReactToPointer();
        VoiceInputRequested?.Invoke();
    }

    private void VoiceInputMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        Robot?.ReactToPointer();
        VoiceInputRequested?.Invoke();
    }

    private void MascotMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string tag } &&
            Enum.TryParse<NcfMascotKind>(tag, ignoreCase: true, out var mascot))
        {
            Robot?.UseMascotOverride(mascot);
        }
    }

    private DesktopRobotViewModel? Robot => DataContext as DesktopRobotViewModel;
}
