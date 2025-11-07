# 阶段 3️⃣: 统一抽象层设计

## 📋 步骤信息
- **步骤ID**: step-03
- **步骤名称**: 跨平台抽象层设计
- **预计时间**: 5.5 小时
- **优先级**: 🔥 高
- **状态**: ⏳ 待开始

## 🎯 目标
创建统一的 WebView 抽象接口，屏蔽平台差异，使得上层代码可以透明地在不同平台使用不同的 WebView 实现。

## 📂 涉及文件
- `Views/Controls/IPlatformWebView.cs` - 新建，WebView 抽象接口
- `Views/Controls/PlatformWebViewFactory.cs` - 新建，工厂类
- `Views/Controls/EmbeddedWebView.cs` - 重构，使用抽象层
- `Views/BrowserView.axaml.cs` - 简化，通过抽象层操作

## 🔨 实施步骤

### 1. 定义 IPlatformWebView 接口 (1小时)

**新建文件**：`Views/Controls/IPlatformWebView.cs`

```csharp
using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// 跨平台 WebView 抽象接口
/// </summary>
public interface IPlatformWebView
{
    /// <summary>
    /// 获取 WebView 控件实例
    /// </summary>
    Control GetControl();

    /// <summary>
    /// 导航到指定 URL
    /// </summary>
    Task NavigateAsync(string url);

    /// <summary>
    /// 刷新当前页面
    /// </summary>
    void Refresh();

    /// <summary>
    /// 后退到上一页
    /// </summary>
    void GoBack();

    /// <summary>
    /// 前进到下一页
    /// </summary>
    void GoForward();

    /// <summary>
    /// 是否可以后退
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// 是否可以前进
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// 当前 URL
    /// </summary>
    string CurrentUrl { get; }

    /// <summary>
    /// 是否已初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 导航开始事件
    /// </summary>
    event EventHandler<string>? NavigationStarted;

    /// <summary>
    /// 导航完成事件
    /// </summary>
    event EventHandler<string>? NavigationCompleted;

    /// <summary>
    /// 导航失败事件
    /// </summary>
    event EventHandler<string>? NavigationFailed;

    /// <summary>
    /// 清理资源
    /// </summary>
    void Dispose();
}

/// <summary>
/// WebView 平台类型
/// </summary>
public enum WebViewPlatform
{
    /// <summary>
    /// Windows WebView2（基于 Chromium，免费）
    /// </summary>
    WebView2,

    /// <summary>
    /// macOS WKWebView（系统原生，免费）
    /// </summary>
    WKWebView,

    /// <summary>
    /// Linux WebKitGTK（开源免费）
    /// </summary>
    WebKitGTK,

    /// <summary>
    /// 降级方案（占位符）
    /// </summary>
    Fallback
}

/// <summary>
/// WebView 能力检测结果
/// </summary>
public class WebViewCapabilities
{
    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 平台类型
    /// </summary>
    public WebViewPlatform Platform { get; set; }

    /// <summary>
    /// 版本信息
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// 错误信息（如果不可用）
    /// </summary>
    public string ErrorMessage { get; set; } = "";

    /// <summary>
    /// 依赖缺失信息
    /// </summary>
    public string[] MissingDependencies { get; set; } = Array.Empty<string>();
}
```

### 2. 创建 PlatformWebViewFactory (1.5小时)

**新建文件**：`Views/Controls/PlatformWebViewFactory.cs`

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// 跨平台 WebView 工厂类
/// 负责根据当前平台创建合适的 WebView 实例
/// </summary>
public static class PlatformWebViewFactory
{
    /// <summary>
    /// 检测当前平台的 WebView 能力
    /// </summary>
    public static WebViewCapabilities DetectCapabilities()
    {
        var capabilities = new WebViewCapabilities();

        // Windows 平台
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return DetectWindowsCapabilities();
        }

        // macOS 平台
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return DetectMacOSCapabilities();
        }

        // Linux 平台
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return DetectLinuxCapabilities();
        }

        // 未知平台
        capabilities.IsAvailable = false;
        capabilities.Platform = WebViewPlatform.Fallback;
        capabilities.ErrorMessage = $"不支持的平台: {RuntimeInformation.OSDescription}";
        return capabilities;
    }

    private static WebViewCapabilities DetectWindowsCapabilities()
    {
        var capabilities = new WebViewCapabilities
        {
            Platform = WebViewPlatform.WebView2
        };

        try
        {
#if WINDOWS
            var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .GetAvailableBrowserVersionString();
            
            if (!string.IsNullOrEmpty(version))
            {
                capabilities.IsAvailable = true;
                capabilities.Version = version;
                Debug.WriteLine($"✅ WebView2 可用: {version}");
            }
            else
            {
                capabilities.IsAvailable = false;
                capabilities.ErrorMessage = "WebView2 Runtime 未安装";
                capabilities.MissingDependencies = new[]
                {
                    "WebView2 Runtime",
                    "下载地址: https://developer.microsoft.com/microsoft-edge/webview2/"
                };
            }
#else
            capabilities.IsAvailable = false;
            capabilities.ErrorMessage = "WebView2 仅在 Windows 平台可用";
#endif
        }
        catch (Exception ex)
        {
            capabilities.IsAvailable = false;
            capabilities.ErrorMessage = $"WebView2 检测失败: {ex.Message}";
            Debug.WriteLine($"❌ WebView2 检测失败: {ex.Message}");
        }

        return capabilities;
    }

    private static WebViewCapabilities DetectMacOSCapabilities()
    {
        var capabilities = new WebViewCapabilities
        {
            Platform = WebViewPlatform.WKWebView,
            IsAvailable = true,  // macOS 11+ 都支持 WKWebView
            Version = Environment.OSVersion.Version.ToString()
        };

        try
        {
            // WKWebView 是 macOS 系统组件，通常总是可用
            Debug.WriteLine($"✅ WKWebView 可用 (macOS {capabilities.Version})");
        }
        catch (Exception ex)
        {
            capabilities.IsAvailable = false;
            capabilities.ErrorMessage = $"WKWebView 检测失败: {ex.Message}";
            Debug.WriteLine($"❌ WKWebView 检测失败: {ex.Message}");
        }

        return capabilities;
    }

    private static WebViewCapabilities DetectLinuxCapabilities()
    {
        var capabilities = new WebViewCapabilities
        {
            Platform = WebViewPlatform.WebKitGTK
        };

        try
        {
            // 检测 libwebkit2gtk 是否安装
            var checkProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "pkg-config",
                Arguments = "--modversion webkit2gtk-4.0",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (checkProcess != null)
            {
                checkProcess.WaitForExit(3000);
                
                if (checkProcess.ExitCode == 0)
                {
                    var version = checkProcess.StandardOutput.ReadToEnd().Trim();
                    capabilities.IsAvailable = true;
                    capabilities.Version = version;
                    Debug.WriteLine($"✅ WebKitGTK 可用: {version}");
                }
                else
                {
                    capabilities.IsAvailable = false;
                    capabilities.ErrorMessage = "WebKitGTK 未安装";
                    capabilities.MissingDependencies = new[]
                    {
                        "Ubuntu/Debian: sudo apt-get install libwebkit2gtk-4.0-dev",
                        "Fedora: sudo dnf install webkit2gtk3-devel",
                        "Arch: sudo pacman -S webkit2gtk"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            capabilities.IsAvailable = false;
            capabilities.ErrorMessage = $"WebKitGTK 检测失败: {ex.Message}";
            Debug.WriteLine($"❌ WebKitGTK 检测失败: {ex.Message}");
        }

        return capabilities;
    }

    /// <summary>
    /// 创建适合当前平台的 WebView 实例
    /// </summary>
    public static IPlatformWebView? CreateWebView()
    {
        var capabilities = DetectCapabilities();

        if (!capabilities.IsAvailable)
        {
            Debug.WriteLine($"⚠️ WebView 不可用: {capabilities.ErrorMessage}");
            return null;
        }

        try
        {
            switch (capabilities.Platform)
            {
                case WebViewPlatform.WebView2:
                    Debug.WriteLine("🪟 创建 Windows WebView2");
                    return new WindowsWebView2Adapter();

                case WebViewPlatform.WKWebView:
                case WebViewPlatform.WebKitGTK:
                    Debug.WriteLine($"🌐 创建 Avalonia WebView ({capabilities.Platform})");
                    return new AvaloniaWebViewAdapter();

                default:
                    Debug.WriteLine("⚠️ 使用降级方案");
                    return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 创建 WebView 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取用户友好的错误提示
    /// </summary>
    public static string GetUserFriendlyErrorMessage(WebViewCapabilities capabilities)
    {
        if (capabilities.IsAvailable)
        {
            return "";
        }

        var message = $"内嵌浏览器不可用：{capabilities.ErrorMessage}\n\n";

        if (capabilities.MissingDependencies.Length > 0)
        {
            message += "解决方法：\n";
            foreach (var dep in capabilities.MissingDependencies)
            {
                message += $"• {dep}\n";
            }
        }

        return message;
    }
}
```

### 3. 创建适配器类 (2小时)

**新建文件**：`Views/Controls/WindowsWebView2Adapter.cs`

```csharp
using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// Windows WebView2 适配器
/// </summary>
public class WindowsWebView2Adapter : IPlatformWebView
{
    private readonly WindowsWebView2Control _control;
    private bool _disposed = false;

    public WindowsWebView2Adapter()
    {
        _control = new WindowsWebView2Control();
        _control.NavigationStarted += (s, url) => NavigationStarted?.Invoke(this, url);
        _control.NavigationCompleted += (s, url) => NavigationCompleted?.Invoke(this, url);
        _control.NavigationFailed += (s, error) => NavigationFailed?.Invoke(this, error);
    }

    public Control GetControl() => _control;

    public async Task NavigateAsync(string url)
    {
        _control.Source = url;
        await Task.CompletedTask;
    }

    public void Refresh() => _control.Refresh();

    public void GoBack() => _control.GoBack();

    public void GoForward() => _control.GoForward();

    public bool CanGoBack => _control.CanGoBack;

    public bool CanGoForward => _control.CanGoForward;

    public string CurrentUrl => _control.Source;

    public bool IsInitialized => true;

    public event EventHandler<string>? NavigationStarted;
    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? NavigationFailed;

    public void Dispose()
    {
        if (!_disposed)
        {
            // WebView2Control 会在 DestroyNativeControlCore 中清理
            _disposed = true;
        }
    }
}
```

**新建文件**：`Views/Controls/AvaloniaWebViewAdapter.cs`

```csharp
using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// Avalonia WebView 适配器（macOS/Linux）
/// </summary>
public class AvaloniaWebViewAdapter : IPlatformWebView
{
    private readonly AvaloniaWebViewControl _control;
    private bool _disposed = false;

    public AvaloniaWebViewAdapter()
    {
        _control = new AvaloniaWebViewControl();
        _control.NavigationStarted += (s, url) => NavigationStarted?.Invoke(this, url);
        _control.NavigationCompleted += (s, url) => NavigationCompleted?.Invoke(this, url);
        _control.NavigationFailed += (s, error) => NavigationFailed?.Invoke(this, error);
    }

    public Control GetControl() => _control;

    public async Task NavigateAsync(string url)
    {
        await _control.NavigateTo(url);
    }

    public void Refresh() => _control.Refresh();

    public void GoBack() => _control.GoBack();

    public void GoForward() => _control.GoForward();

    public bool CanGoBack => _control.CanGoBack;

    public bool CanGoForward => _control.CanGoForward;

    public string CurrentUrl => _control.Source;

    public bool IsInitialized => true;

    public event EventHandler<string>? NavigationStarted;
    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? NavigationFailed;

    public void Dispose()
    {
        if (!_disposed)
        {
            // 清理资源
            _disposed = true;
        }
    }
}
```

### 4. 重构 EmbeddedWebView (1小时)

**修改 `Views/Controls/EmbeddedWebView.cs`**：

```csharp
private async Task InitializeWebViewHostAsync()
{
    try
    {
        UpdateStatus("正在检测平台能力...", Brushes.Blue);
        
        // 检测 WebView 能力
        var capabilities = PlatformWebViewFactory.DetectCapabilities();
        
        if (!capabilities.IsAvailable)
        {
            var errorMsg = PlatformWebViewFactory.GetUserFriendlyErrorMessage(capabilities);
            UpdateStatus($"WebView 不可用", Brushes.Red);
            ShowErrorView(errorMsg);
            return;
        }
        
        UpdateStatus($"正在初始化 {capabilities.Platform}...", Brushes.Blue);
        
        // 创建 WebView 实例
        var webView = PlatformWebViewFactory.CreateWebView();
        
        if (webView == null)
        {
            UpdateStatus("创建 WebView 失败", Brushes.Red);
            ShowFallbackView();
            return;
        }
        
        // 订阅事件
        webView.NavigationStarted += (s, url) => OnNavigationStarted(url);
        webView.NavigationCompleted += (s, url) => OnNavigationCompleted(url);
        webView.NavigationFailed += (s, error) => OnNavigationFailed(error);
        
        // 获取控件并添加到容器
        var control = webView.GetControl();
        control.HorizontalAlignment = HorizontalAlignment.Stretch;
        control.VerticalAlignment = VerticalAlignment.Stretch;
        
        _webViewContainer.Children.Clear();
        _webViewContainer.Children.Add(control);
        Grid.SetRow(control, 0);
        
        _platformWebView = webView;
        _isWebViewReady = true;
        
        UpdateStatus($"✅ {capabilities.Platform} 已就绪", Brushes.Green);
        
        // 如果有待导航的 URL
        if (!string.IsNullOrEmpty(Source))
        {
            await NavigateToUrlAsync(Source);
        }
    }
    catch (Exception ex)
    {
        UpdateStatus($"初始化失败: {ex.Message}", Brushes.Red);
        ShowFallbackView();
    }
}
```

## ✅ 验收标准

### 功能验收
- [ ] 接口定义清晰，覆盖所有必要功能
- [ ] 工厂类能正确检测平台能力
- [ ] 适配器正确封装平台特定实现
- [ ] 上层代码与平台解耦

### 技术验收
- [ ] 代码编译通过
- [ ] 接口设计符合SOLID原则
- [ ] 工厂模式实现正确

### 质量验收
- [ ] 代码注释完整
- [ ] 错误处理完善
- [ ] 日志输出详细

## 📝 注意事项

### 💡 设计原则
- **单一职责**：每个适配器只负责一种 WebView
- **开闭原则**：易于扩展新的 WebView 实现
- **依赖倒置**：上层依赖抽象而非具体实现

---

**状态**: ⏳ 待开始  
**优先级**: 🔥 高  
**依赖**: step-01, step-02  
**预计时间**: 5.5小时

