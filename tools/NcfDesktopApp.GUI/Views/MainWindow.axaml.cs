/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MainWindow.axaml.cs
    文件功能描述：桌面应用主窗口交互与尺寸适配逻辑
    
    
    创建标识：Senparc - 20250718
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强更新源选择、下载反馈与桌面窗口兼容性

    修改标识：Senparc - 20260726
    修改描述：修复 macOS 内嵌 WebView 复制粘贴快捷键无响应

----------------------------------------------------------------*/
using Avalonia.Controls;
using Avalonia.Input;
using NcfDesktopApp.GUI.Services;
using NcfDesktopApp.GUI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NcfDesktopApp.GUI.Views;

public partial class MainWindow : Window
{
    /// <summary>为 true 时跳过「NCF 运行中」关闭确认（避免二次 Close 再次弹框）。</summary>
    private bool _allowCloseWithoutNcfConfirm;

    /// <summary>防止 NativeMenu 与 KeyDown 对同一次 ⌘V 各处理一次。</summary>
    private DateTime _lastEditCommandUtc = DateTime.MinValue;
    private WebViewEditBridge.EditCommand _lastEditCommand = WebViewEditBridge.EditCommand.None;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnMainWindowOpened;
        Closing += OnMainWindowClosing;
    }

    private async void OnEditCutClick(object? sender, EventArgs e) =>
        await ExecuteEditCommandAsync(WebViewEditBridge.EditCommand.Cut);

    private async void OnEditCopyClick(object? sender, EventArgs e) =>
        await ExecuteEditCommandAsync(WebViewEditBridge.EditCommand.Copy);

    private async void OnEditPasteClick(object? sender, EventArgs e) =>
        await ExecuteEditCommandAsync(WebViewEditBridge.EditCommand.Paste);

    private async void OnEditSelectAllClick(object? sender, EventArgs e) =>
        await ExecuteEditCommandAsync(WebViewEditBridge.EditCommand.SelectAll);

    /// <summary>
    /// 非 macOS / NativeMenu 未接管时的键盘兜底。
    /// 仅处理编辑快捷键，不拦截普通打字。
    /// </summary>
    private async void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (!WebViewEditBridge.MatchesEditGesture(e, out var command))
        {
            return;
        }

        // Avalonia 文本控件已有平台快捷键，交给其自身处理，避免双重粘贴。
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (WebViewEditBridge.IsEditableAvaloniaFocus(focused))
        {
            return;
        }

        if (await ExecuteEditCommandAsync(command).ConfigureAwait(true))
        {
            e.Handled = true;
            Console.WriteLine($"[EditShortcut] KeyDown 已处理: {command}, Key={e.Key}, Mods={e.KeyModifiers}");
        }
    }

    private async Task<bool> ExecuteEditCommandAsync(WebViewEditBridge.EditCommand command)
    {
        try
        {
            var now = DateTime.UtcNow;
            if (command == _lastEditCommand && (now - _lastEditCommandUtc).TotalMilliseconds < 120)
            {
                Console.WriteLine($"[EditShortcut] 忽略重复触发: {command}");
                return true;
            }

            _lastEditCommand = command;
            _lastEditCommandUtc = now;

            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();

            // 1) Avalonia 原生文本控件：直接调用控件 API（设置页、地址栏等）
            // 仅当控件实际可见时处理，避免切到浏览器标签后焦点仍留在隐藏 TextBox。
            if (focused is TextBox textBox && textBox.IsEffectivelyVisible)
            {
                switch (command)
                {
                    case WebViewEditBridge.EditCommand.Cut:
                        textBox.Cut();
                        return true;
                    case WebViewEditBridge.EditCommand.Copy:
                        textBox.Copy();
                        return true;
                    case WebViewEditBridge.EditCommand.Paste:
                        textBox.Paste();
                        return true;
                    case WebViewEditBridge.EditCommand.SelectAll:
                        textBox.SelectAll();
                        return true;
                }
            }

            // 2) 浏览器标签页内嵌 WebView（登录页等）
            // 焦点在 WKWebView 时 Avalonia FocusManager 常为 null，只要浏览器标签激活就路由到 WebView。
            if (DataContext is MainWindowViewModel vm
                && vm.IsBrowserTabActive
                && vm.BrowserViewReference is BrowserView browserView
                && browserView.IsEmbeddedWebViewReady)
            {
                var ok = command switch
                {
                    WebViewEditBridge.EditCommand.Cut => await browserView.WebViewCutAsync().ConfigureAwait(true),
                    WebViewEditBridge.EditCommand.Copy => await browserView.WebViewCopyAsync().ConfigureAwait(true),
                    WebViewEditBridge.EditCommand.Paste => await browserView.WebViewPasteAsync().ConfigureAwait(true),
                    WebViewEditBridge.EditCommand.SelectAll => await browserView.WebViewSelectAllAsync().ConfigureAwait(true),
                    _ => false
                };
                Console.WriteLine($"[EditShortcut] WebView 路由: {command}, ok={ok}");
                return ok;
            }

            Console.WriteLine($"[EditShortcut] 未找到可处理目标: {command}, focused={focused?.GetType().Name ?? "null"}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EditShortcut] 执行失败: {command}, {ex.Message}");
            return false;
        }
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
