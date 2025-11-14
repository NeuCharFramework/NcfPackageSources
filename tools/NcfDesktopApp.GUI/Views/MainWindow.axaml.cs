using Avalonia.Controls;
using System;
using System.Linq;

namespace NcfDesktopApp.GUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AdjustWindowSizeToScreen();
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
                
                // 定义理想尺寸
                const double idealWidth = 900;
                const double idealHeight = 800;
                
                // 定义最小尺寸
                const double minWidth = 800;
                const double minHeight = 650;
                
                // 计算安全边距（为任务栏、标题栏等预留空间）
                const double safetyMarginWidth = 100;  // 左右各留50px
                const double safetyMarginHeight = 100; // 上下各留50px（考虑标题栏和任务栏）
                
                // 计算可用空间
                double availableWidth = workingArea.Width - safetyMarginWidth;
                double availableHeight = workingArea.Height - safetyMarginHeight;
                
                // 确定窗口尺寸
                double targetWidth = Math.Max(minWidth, Math.Min(idealWidth, availableWidth));
                double targetHeight = Math.Max(minHeight, Math.Min(idealHeight, availableHeight));
                
                // 应用调整后的尺寸
                Width = targetWidth;
                Height = targetHeight;
                
                // 输出调试信息
                Console.WriteLine($"[窗口自适应] 屏幕工作区: {workingArea.Width}x{workingArea.Height}");
                Console.WriteLine($"[窗口自适应] 可用空间: {availableWidth}x{availableHeight}");
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