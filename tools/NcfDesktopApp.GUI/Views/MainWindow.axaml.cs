/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MainWindow.axaml.cs
    文件功能描述：桌面应用主窗口交互与尺寸适配逻辑
    
    
    创建标识：Senparc - 20250718
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强更新源选择、下载反馈与桌面窗口兼容性

----------------------------------------------------------------*/
using Avalonia.Controls;
using NcfDesktopApp.GUI.ViewModels;
using System;
using System.Linq;

namespace NcfDesktopApp.GUI.Views;

public partial class MainWindow : Window
{
    /// <summary>为 true 时跳过「NCF 运行中」关闭确认（避免二次 Close 再次弹框）。</summary>
    private bool _allowCloseWithoutNcfConfirm;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnMainWindowOpened;
        Closing += OnMainWindowClosing;
    }

    private void OnMainWindowOpened(object? sender, EventArgs e)
    {
        AdjustWindowSizeToScreen();
    }

    private async void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowCloseWithoutNcfConfirm)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (!vm.IsNcfRunning)
        {
            return;
        }

        e.Cancel = true;

        try
        {
            if (!await vm.TryPrepareShutdownForWindowCloseAsync().ConfigureAwait(true))
            {
                return;
            }

            _allowCloseWithoutNcfConfirm = true;
            Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[关闭主窗口] 停止 NCF 或确认流程异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据屏幕分辨率自适应调整窗口尺寸
    /// 确保在低分辨率屏幕上窗口不会超出边界
    /// </summary>
    private void AdjustWindowSizeToScreen()
    {
        try
        {
            // 获取当前屏幕
            var screen = Screens?.ScreenFromWindow(this);
            if (screen == null)
            {
                // 如果无法获取当前屏幕，尝试获取主屏幕
                screen = Screens?.All?.FirstOrDefault();
            }

            if (screen?.WorkingArea != null)
            {
                var workingArea = screen.WorkingArea;
                
                // Screen.WorkingArea 使用物理像素，Window 尺寸使用 DIP；Retina/缩放屏必须先换算。
                var scaling = screen.Scaling > 0 ? screen.Scaling : 1;
                var workingWidth = workingArea.Width / scaling;
                var workingHeight = workingArea.Height / scaling;

                // 定义理想尺寸。高度略高于默认值，尽量让左侧紧凑布局无需滚动。
                const double idealWidth = 1040;
                const double idealHeight = 900;
                const double preferredMinWidth = 820;
                const double preferredMinHeight = 600;
                const double safetyMargin = 24;

                var availableWidth = Math.Max(1, workingWidth - safetyMargin);
                var availableHeight = Math.Max(1, workingHeight - safetyMargin);

                // 小屏幕上动态降低最小尺寸，否则固定 MinHeight 会把窗口强行撑到屏幕之外。
                MinWidth = Math.Min(preferredMinWidth, availableWidth);
                MinHeight = Math.Min(preferredMinHeight, availableHeight);
                MaxWidth = availableWidth;
                MaxHeight = availableHeight;

                var targetWidth = Math.Max(MinWidth, Math.Min(idealWidth, availableWidth));
                var targetHeight = Math.Max(MinHeight, Math.Min(idealHeight, availableHeight));

                // 应用调整后的尺寸
                Width = targetWidth;
                Height = targetHeight;

                // Opened 后重新居中，保证调整后的窗口完整位于当前屏幕工作区内。
                var targetPixelWidth = (int)Math.Round(targetWidth * scaling);
                var targetPixelHeight = (int)Math.Round(targetHeight * scaling);
                Position = new Avalonia.PixelPoint(
                    workingArea.X + Math.Max(0, (workingArea.Width - targetPixelWidth) / 2),
                    workingArea.Y + Math.Max(0, (workingArea.Height - targetPixelHeight) / 2));

                // 输出调试信息
                Console.WriteLine($"[窗口自适应] 屏幕工作区: {workingArea.Width}x{workingArea.Height} px，缩放: {scaling:F2}");
                Console.WriteLine($"[窗口自适应] 可用空间: {availableWidth:F0}x{availableHeight:F0} DIP");
                Console.WriteLine($"[窗口自适应] 窗口尺寸: {targetWidth}x{targetHeight}");
                
                // 如果窗口尺寸被调整，记录警告
                if (targetWidth < idealWidth || targetHeight < idealHeight)
                {
                    Console.WriteLine($"[窗口自适应] ⚠️ 检测到低分辨率屏幕，窗口尺寸已从 {idealWidth}x{idealHeight} 调整为 {targetWidth}x{targetHeight}");
                }
            }
            else
            {
                Console.WriteLine("[窗口自适应] ⚠️ 无法获取屏幕工作区信息，使用默认窗口尺寸");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[窗口自适应] ❌ 调整窗口尺寸时出错: {ex.Message}");
            // 出错时保持默认尺寸，不影响应用启动
        }
    }
}
