# 阶段 1️⃣: Windows WebView2 集成（免费开源方案）

## 📋 步骤信息
- **步骤ID**: step-01
- **步骤名称**: Windows WebView2 集成
- **预计时间**: 6.5 小时
- **优先级**: 🔥 高
- **状态**: ⏳ 待开始

## 🎯 目标
在 Windows 平台上集成 **Microsoft.Web.WebView2**（完全免费），实现真正的内嵌浏览器功能，让用户可以在应用内直接访问 NCF 网页。

## 📂 涉及文件
- `NcfDesktopApp.GUI.csproj` - 添加 WebView2 NuGet 包
- `Views/Controls/WindowsWebView2Control.cs` - 新建，WebView2 控件封装
- `Views/BrowserView.axaml` - 修改，集成 WebView2 控件
- `Views/BrowserView.axaml.cs` - 修改，添加平台检测逻辑
- `Views/Controls/EmbeddedWebView.cs` - 修改，使用新的 WebView2 控件

## 🔨 实施步骤

### 1. 添加 Microsoft.Web.WebView2 NuGet 包 (0.5小时)

**操作**：
```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfDesktopApp.GUI
dotnet add package Microsoft.Web.WebView2.Wpf --version 1.0.2470.55
```

**修改 `NcfDesktopApp.GUI.csproj`**：
```xml
<ItemGroup>
  <!-- 现有包... -->
  
  <!-- Windows WebView2 (免费开源) -->
  <PackageReference Include="Microsoft.Web.WebView2.Wpf" Version="1.0.2470.55" Condition="'$(RuntimeIdentifier)' == 'win-x64' OR '$(RuntimeIdentifier)' == 'win-arm64' OR $([MSBuild]::IsOSPlatform('Windows'))" />
</ItemGroup>
```

### 2. 创建 WindowsWebView2Control.cs (2小时)

**新建文件**：`Views/Controls/WindowsWebView2Control.cs`

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

#if WINDOWS
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
#endif

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// Windows WebView2 控件封装（免费开源）
/// </summary>
public class WindowsWebView2Control : NativeControlHost
{
    private string _currentUrl = "";
    private bool _isInitialized = false;
    
#if WINDOWS
    private WebView2? _webView2;
#endif

    public static readonly DirectProperty<WindowsWebView2Control, string> SourceProperty =
        AvaloniaProperty.RegisterDirect<WindowsWebView2Control, string>(
            nameof(Source),
            o => o.Source,
            (o, v) => o.Source = v);

    private string _source = "";
    public string Source
    {
        get => _source;
        set
        {
            SetAndRaise(SourceProperty, ref _source, value);
            if (_isInitialized && !string.IsNullOrEmpty(value))
            {
                _ = NavigateAsync(value);
            }
        }
    }

    public WindowsWebView2Control()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("WindowsWebView2Control 仅支持 Windows 平台");
        }
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
#if WINDOWS
        try
        {
            var parentHandle = parent.Handle;
            
            // 创建 WebView2 控件
            _webView2 = new WebView2
            {
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "NcfDesktopApp",
                        "WebView2"
                    )
                }
            };

            // 异步初始化
            _ = InitializeWebView2Async();

            // 返回控件句柄
            var hwnd = _webView2.Handle;
            return new PlatformHandle(hwnd, "HWND");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"创建 WebView2 失败: {ex.Message}");
            OnNavigationFailed($"WebView2 初始化失败: {ex.Message}");
            throw;
        }
#else
        throw new PlatformNotSupportedException();
#endif
    }

#if WINDOWS
    private async Task InitializeWebView2Async()
    {
        try
        {
            if (_webView2 == null) return;

            // 确保 WebView2 Runtime 已安装
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Debug.WriteLine($"WebView2 Runtime 版本: {version}");

            // 初始化 CoreWebView2
            await _webView2.EnsureCoreWebView2Async(null);

            // 配置 WebView2 设置
            _webView2.CoreWebView2.Settings.IsScriptEnabled = true;
            _webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
            _webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
            _webView2.CoreWebView2.Settings.AreDevToolsEnabled = true;
            _webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;

            // 事件处理
            _webView2.CoreWebView2.NavigationStarting += OnNavigationStarting;
            _webView2.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView2.CoreWebView2.SourceChanged += OnSourceChanged;

            _isInitialized = true;
            Debug.WriteLine("WebView2 初始化成功");

            // 如果有待导航的 URL，现在导航
            if (!string.IsNullOrEmpty(_source))
            {
                await NavigateAsync(_source);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebView2 初始化失败: {ex.Message}");
            OnNavigationFailed($"初始化失败: {ex.Message}");
        }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        _currentUrl = e.Uri;
        Debug.WriteLine($"开始导航: {e.Uri}");
        NavigationStarted?.Invoke(this, e.Uri);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            Debug.WriteLine($"导航成功: {_currentUrl}");
            NavigationCompleted?.Invoke(this, _currentUrl);
        }
        else
        {
            var error = $"导航失败 (错误码: {e.WebErrorStatus})";
            Debug.WriteLine(error);
            OnNavigationFailed(error);
        }
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_webView2?.CoreWebView2 != null)
        {
            _currentUrl = _webView2.CoreWebView2.Source;
        }
    }

    private async Task NavigateAsync(string url)
    {
        if (_webView2?.CoreWebView2 == null || !_isInitialized)
        {
            Debug.WriteLine("WebView2 未初始化，等待初始化完成...");
            return;
        }

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _webView2.CoreWebView2.Navigate(url);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"导航失败: {ex.Message}");
            OnNavigationFailed($"导航失败: {ex.Message}");
        }
    }
#endif

    public void Refresh()
    {
#if WINDOWS
        _webView2?.Reload();
#endif
    }

    public void GoBack()
    {
#if WINDOWS
        if (_webView2?.CanGoBack == true)
        {
            _webView2.GoBack();
        }
#endif
    }

    public void GoForward()
    {
#if WINDOWS
        if (_webView2?.CanGoForward == true)
        {
            _webView2.GoForward();
        }
#endif
    }

    public bool CanGoBack =>
#if WINDOWS
        _webView2?.CanGoBack ?? false;
#else
        false;
#endif

    public bool CanGoForward =>
#if WINDOWS
        _webView2?.CanGoForward ?? false;
#else
        false;
#endif

    // 事件
    public event EventHandler<string>? NavigationStarted;
    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? NavigationFailed;

    protected virtual void OnNavigationFailed(string error)
    {
        NavigationFailed?.Invoke(this, error);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
#if WINDOWS
        if (_webView2 != null)
        {
            _webView2.Dispose();
            _webView2 = null;
        }
#endif
        base.DestroyNativeControlCore(control);
    }
}
```

### 3. 修改 EmbeddedWebView 以使用 WebView2 (1小时)

**修改 `Views/Controls/EmbeddedWebView.cs`**：

在 `TryCreateNativeWebView` 方法中添加 Windows 平台检测：

```csharp
private Control? TryCreateNativeWebView(out Type? controlType)
{
    controlType = null;
    
    // 优先使用 Windows WebView2（免费）
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        try
        {
            // 检查 WebView2 Runtime 是否可用
#if WINDOWS
            var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            if (!string.IsNullOrEmpty(version))
            {
                Debug.WriteLine($"检测到 WebView2 Runtime: {version}");
                var webView2Control = new WindowsWebView2Control();
                controlType = typeof(WindowsWebView2Control);
                return webView2Control;
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebView2 不可用: {ex.Message}");
        }
    }
    
    // 其他平台使用 WebView.Avalonia
    // ... (保留现有代码)
}
```

### 4. 添加 WebView2 Runtime 检测和安装提示 (2小时)

**新建 `Services/WebView2RuntimeChecker.cs`**：

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NcfDesktopApp.GUI.Services;

public class WebView2RuntimeChecker
{
    public static bool IsWebView2Available()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

#if WINDOWS
        try
        {
            var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            return !string.IsNullOrEmpty(version);
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    public static string GetWebView2Version()
    {
#if WINDOWS
        try
        {
            return Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString() ?? "未安装";
        }
        catch
        {
            return "未安装";
        }
#else
        return "不支持";
#endif
    }

    public static void OpenWebView2DownloadPage()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://developer.microsoft.com/microsoft-edge/webview2/",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开下载页面失败: {ex.Message}");
        }
    }

    public static async Task<bool> TryAutoInstallRuntimeAsync()
    {
        // 可以实现自动下载安装 WebView2 Runtime
        // 下载 Evergreen Bootstrapper: https://go.microsoft.com/fwlink/p/?LinkId=2124703
        
        // 这里先返回 false，让用户手动安装
        await Task.Delay(100);
        return false;
    }
}
```

### 5. 测试 Windows 平台功能 (1小时)

**测试清单**：
1. 在 Windows 10/11 上运行应用
2. 验证 WebView2 控件正确加载
3. 测试导航到 NCF URL
4. 测试前进/后退/刷新功能
5. 测试错误处理（无 Runtime 情况）

## 💻 关键代码片段

### WebView2 Runtime 检测（启动时）

在 `ViewModels/MainWindowViewModel.cs` 中添加：

```csharp
private async Task CheckWebView2RuntimeAsync()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        if (!WebView2RuntimeChecker.IsWebView2Available())
        {
            // 提示用户安装 WebView2 Runtime
            var version = WebView2RuntimeChecker.GetWebView2Version();
            UpdateStatus($"⚠️ WebView2 Runtime 未安装 (当前: {version})", "#FFA500");
            
            // 可以弹出对话框询问用户是否安装
            // 或者提供一个按钮让用户下载
        }
        else
        {
            var version = WebView2RuntimeChecker.GetWebView2Version();
            Debug.WriteLine($"✅ WebView2 Runtime 已就绪: {version}");
        }
    }
}
```

## ✅ 验收标准

### 功能验收
- [ ] Windows 平台成功加载 WebView2 控件
- [ ] NCF URL 可以在 WebView2 中正确显示
- [ ] 前进/后退/刷新按钮正常工作
- [ ] 页面加载状态正确显示
- [ ] WebView2 Runtime 检测功能正常

### 技术验收
- [ ] 代码编译通过（Windows 平台）
- [ ] 无 linter 错误
- [ ] NuGet 包正确安装
- [ ] 条件编译正确（仅 Windows 引用 WebView2）

### 质量验收
- [ ] 代码有详细注释
- [ ] 错误处理完整（Runtime 缺失、导航失败等）
- [ ] 资源正确释放（Dispose）
- [ ] 日志输出完整

## 🧪 测试方法

### 手动测试步骤

1. **环境准备**：
   ```bash
   # Windows 10/11 系统
   # 安装 WebView2 Runtime（如果未安装）
   ```

2. **编译运行**：
   ```bash
   cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfDesktopApp.GUI
   dotnet build
   dotnet run
   ```

3. **功能测试**：
   - 启动 NCF 应用
   - 观察浏览器标签页是否显示网页内容
   - 测试前进/后退按钮
   - 测试刷新按钮
   - 测试不同 URL 导航

4. **异常测试**：
   - 卸载 WebView2 Runtime 后测试降级行为
   - 测试网络断开时的错误处理
   - 测试无效 URL 的处理

### 预期结果
- ✅ WebView2 控件在 Windows 上正常工作
- ✅ NCF 网页完整渲染，与浏览器体验一致
- ✅ 所有导航功能正常
- ✅ 错误信息清晰，提供解决建议

## 📝 注意事项

### ⚠️ 重要提示
1. **WebView2 Runtime 依赖**：
   - Windows 11 已预装
   - Windows 10 可能需要安装
   - 提供清晰的安装提示和下载链接

2. **条件编译**：
   - 使用 `#if WINDOWS` 确保只在 Windows 编译
   - 避免其他平台编译错误

3. **内存管理**：
   - 正确实现 Dispose 模式
   - 及时清理 WebView2 资源

4. **错误处理**：
   - 捕获所有可能的异常
   - 提供友好的错误提示
   - 实现降级方案

### 💡 最佳实践
- 在应用启动时检测 Runtime
- 提供一键安装或下载链接
- 使用异步初始化避免阻塞 UI
- 记录详细的调试日志

## 🔗 相关资源
- [WebView2 官方文档](https://learn.microsoft.com/microsoft-edge/webview2/)
- [WebView2 Runtime 下载](https://developer.microsoft.com/microsoft-edge/webview2/)
- [WebView2 API 参考](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.wpf)
- [Avalonia NativeControlHost](https://docs.avaloniaui.net/docs/guides/custom-controls/how-to-create-a-custom-controls-library)

---

**状态**: ⏳ 待开始  
**优先级**: 🔥 高  
**依赖**: 无  
**预计时间**: 6.5小时

