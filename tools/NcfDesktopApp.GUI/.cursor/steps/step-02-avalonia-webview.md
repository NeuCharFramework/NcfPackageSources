# 阶段 2️⃣: WebView.Avalonia 配置（macOS/Linux）- 免费开源

## 📋 步骤信息
- **步骤ID**: step-02
- **步骤名称**: WebView.Avalonia 跨平台集成
- **预计时间**: 6.5 小时
- **优先级**: 🔥 高
- **状态**: ⏳ 待开始

## 🎯 目标
在 macOS 和 Linux 平台上正确配置和使用 **WebView.Avalonia**（完全免费开源），实现跨平台的内嵌浏览器功能。

**技术栈**：
- **macOS**: WKWebView (系统原生)
- **Linux**: WebKitGTK (开源)
- **包**: WebView.Avalonia v11.0.0.1 (已在项目中)

## 📂 涉及文件
- `NcfDesktopApp.GUI.csproj` - 验证 WebView.Avalonia 配置
- `Views/Controls/AvaloniaWebViewControl.cs` - 新建，封装 WebView.Avalonia
- `Views/Controls/EmbeddedWebView.cs` - 修改，集成 Avalonia WebView
- `README.md` - 更新平台依赖说明

## 🔨 实施步骤

### 1. 验证 WebView.Avalonia 包配置 (0.5小时)

**检查当前配置**：
项目已经包含以下包：
```xml
<PackageReference Include="WebView.Avalonia" Version="11.0.0.1" />
<PackageReference Include="WebView.Avalonia.Desktop" Version="11.0.0.1" />
```

**验证依赖**：
```bash
dotnet list package
# 应该看到：
# WebView.Avalonia 11.0.0.1
# WebView.Avalonia.Desktop 11.0.0.1
```

**平台特定依赖**：

**macOS**：
- 无需额外安装（使用系统 WKWebView）
- 要求：macOS 10.14+

**Linux**：
```bash
# Ubuntu/Debian
sudo apt-get install libwebkit2gtk-4.0-dev

# Fedora/CentOS
sudo dnf install webkit2gtk3-devel

# Arch Linux
sudo pacman -S webkit2gtk
```

### 2. 创建 AvaloniaWebViewControl.cs (2小时)

**新建文件**：`Views/Controls/AvaloniaWebViewControl.cs`

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using WebViewControl;

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// Avalonia WebView 控件封装（免费开源，支持 macOS/Linux）
/// </summary>
public class AvaloniaWebViewControl : UserControl
{
    private WebView? _webView;
    private bool _isInitialized = false;
    private string _pendingUrl = "";

    public static readonly StyledProperty<string> SourceProperty =
        AvaloniaProperty.Register<AvaloniaWebViewControl, string>(nameof(Source), "");

    public string Source
    {
        get => GetValue(SourceProperty);
        set
        {
            SetAndRaise(SourceProperty, value);
            if (_isInitialized && !string.IsNullOrEmpty(value))
            {
                _ = NavigateAsync(value);
            }
            else
            {
                _pendingUrl = value;
            }
        }
    }

    public AvaloniaWebViewControl()
    {
        InitializeComponent();
        _ = InitializeWebViewAsync();
    }

    private void InitializeComponent()
    {
        // 创建占位内容
        var loadingPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 10
        };

        loadingPanel.Children.Add(new TextBlock
        {
            Text = "🌐",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        loadingPanel.Children.Add(new TextBlock
        {
            Text = "正在初始化浏览器...",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        Content = loadingPanel;
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // 创建 WebView.Avalonia 控件
                    _webView = new WebView
                    {
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                    };

                    // 注册事件
                    _webView.PropertyChanged += OnWebViewPropertyChanged;

                    // 使用反射订阅导航事件（WebView.Avalonia API 可能不同）
                    SubscribeToNavigationEvents();

                    // 替换内容
                    Content = _webView;
                    _isInitialized = true;

                    Debug.WriteLine("✅ WebView.Avalonia 初始化成功");

                    // 如果有待导航的 URL
                    if (!string.IsNullOrEmpty(_pendingUrl))
                    {
                        _ = NavigateAsync(_pendingUrl);
                        _pendingUrl = "";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ WebView.Avalonia 初始化失败: {ex.Message}");
                    ShowErrorView(ex.Message);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"初始化异常: {ex}");
            ShowErrorView(ex.Message);
        }
    }

    private void SubscribeToNavigationEvents()
    {
        if (_webView == null) return;

        try
        {
            // WebView.Avalonia 可能使用不同的事件名称
            // 需要根据实际 API 调整

            var type = _webView.GetType();
            
            // 尝试订阅 NavigationStarted 事件
            var navStartedEvent = type.GetEvent("NavigationStarted");
            if (navStartedEvent != null)
            {
                var handler = new EventHandler<object>((s, e) => OnNavigationStarted(e));
                navStartedEvent.AddEventHandler(_webView, handler);
            }

            // 尝试订阅 NavigationCompleted 事件
            var navCompletedEvent = type.GetEvent("NavigationCompleted");
            if (navCompletedEvent != null)
            {
                var handler = new EventHandler<object>((s, e) => OnNavigationCompleted(e));
                navCompletedEvent.AddEventHandler(_webView, handler);
            }

            Debug.WriteLine("✅ 导航事件订阅成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ 导航事件订阅失败: {ex.Message}");
        }
    }

    private void OnWebViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // 监听 Source 属性变化
        if (e.Property.Name == "Source" && _webView != null)
        {
            var url = _webView.GetValue(WebView.SourceProperty) as string;
            if (!string.IsNullOrEmpty(url))
            {
                Debug.WriteLine($"🔗 WebView URL 变化: {url}");
            }
        }
    }

    private void OnNavigationStarted(object eventArgs)
    {
        try
        {
            // 提取 URL（根据实际事件参数类型调整）
            var url = ExtractUrlFromEventArgs(eventArgs);
            Debug.WriteLine($"🚀 开始导航: {url}");
            NavigationStarted?.Invoke(this, url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"处理导航开始事件失败: {ex.Message}");
        }
    }

    private void OnNavigationCompleted(object eventArgs)
    {
        try
        {
            var url = ExtractUrlFromEventArgs(eventArgs);
            Debug.WriteLine($"✅ 导航完成: {url}");
            NavigationCompleted?.Invoke(this, url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"处理导航完成事件失败: {ex.Message}");
        }
    }

    private string ExtractUrlFromEventArgs(object eventArgs)
    {
        try
        {
            // 尝试通过反射获取 URL
            var type = eventArgs.GetType();
            var urlProperty = type.GetProperty("Url") ?? type.GetProperty("Uri");
            if (urlProperty != null)
            {
                var value = urlProperty.GetValue(eventArgs);
                return value?.ToString() ?? "";
            }
        }
        catch { }
        
        return _webView?.GetValue(WebView.SourceProperty) as string ?? "";
    }

    private async Task NavigateAsync(string url)
    {
        if (_webView == null || !_isInitialized)
        {
            Debug.WriteLine("⏳ WebView 未初始化，URL 将在初始化后加载");
            _pendingUrl = url;
            return;
        }

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Debug.WriteLine($"🔗 导航到: {url}");
                
                // WebView.Avalonia 使用 Source 属性导航
                _webView.SetValue(WebView.SourceProperty, url);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 导航失败: {ex.Message}");
            NavigationFailed?.Invoke(this, $"导航失败: {ex.Message}");
        }
    }

    public async Task NavigateTo(string url)
    {
        await NavigateAsync(url);
    }

    public void Refresh()
    {
        try
        {
            // WebView.Avalonia 刷新方法
            var type = _webView?.GetType();
            var refreshMethod = type?.GetMethod("Reload") ?? type?.GetMethod("Refresh");
            refreshMethod?.Invoke(_webView, null);
            
            Debug.WriteLine("🔄 刷新页面");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"刷新失败: {ex.Message}");
        }
    }

    public void GoBack()
    {
        try
        {
            if (CanGoBack)
            {
                var type = _webView?.GetType();
                var goBackMethod = type?.GetMethod("GoBack");
                goBackMethod?.Invoke(_webView, null);
                
                Debug.WriteLine("⬅️ 后退");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"后退失败: {ex.Message}");
        }
    }

    public void GoForward()
    {
        try
        {
            if (CanGoForward)
            {
                var type = _webView?.GetType();
                var goForwardMethod = type?.GetMethod("GoForward");
                goForwardMethod?.Invoke(_webView, null);
                
                Debug.WriteLine("➡️ 前进");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"前进失败: {ex.Message}");
        }
    }

    public bool CanGoBack
    {
        get
        {
            try
            {
                var type = _webView?.GetType();
                var canGoBackProp = type?.GetProperty("CanGoBack");
                return canGoBackProp?.GetValue(_webView) as bool? ?? false;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool CanGoForward
    {
        get
        {
            try
            {
                var type = _webView?.GetType();
                var canGoForwardProp = type?.GetProperty("CanGoForward");
                return canGoForwardProp?.GetValue(_webView) as bool? ?? false;
            }
            catch
            {
                return false;
            }
        }
    }

    private void ShowErrorView(string errorMessage)
    {
        var errorPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 15
        };

        errorPanel.Children.Add(new TextBlock
        {
            Text = "❌",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        errorPanel.Children.Add(new TextBlock
        {
            Text = "浏览器初始化失败",
            FontSize = 18,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        errorPanel.Children.Add(new TextBlock
        {
            Text = errorMessage,
            FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        });

        var tipText = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? "Linux: 请安装 libwebkit2gtk-4.0-dev\n运行: sudo apt-get install libwebkit2gtk-4.0-dev"
            : "请确保系统浏览器引擎可用";

        errorPanel.Children.Add(new TextBlock
        {
            Text = tipText,
            FontSize = 11,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400,
            Foreground = Avalonia.Media.Brushes.Gray
        });

        Content = errorPanel;
    }

    // 事件
    public event EventHandler<string>? NavigationStarted;
    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? NavigationFailed;
}
```

### 3. 修改 EmbeddedWebView 集成 Avalonia WebView (2小时)

**修改 `Views/Controls/EmbeddedWebView.cs`** 的 `TryCreateNativeWebView` 方法：

```csharp
private Control? TryCreateNativeWebView(out Type? controlType)
{
    controlType = null;
    
    // 1. Windows 平台：优先使用 WebView2（免费）
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        try
        {
#if WINDOWS
            var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .GetAvailableBrowserVersionString();
            if (!string.IsNullOrEmpty(version))
            {
                Debug.WriteLine($"✅ 使用 WebView2: {version}");
                var webView2Control = new WindowsWebView2Control();
                controlType = typeof(WindowsWebView2Control);
                return webView2Control;
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ WebView2 不可用: {ex.Message}");
        }
    }
    
    // 2. macOS/Linux 平台：使用 WebView.Avalonia（免费）
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || 
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        try
        {
            Debug.WriteLine($"✅ 使用 WebView.Avalonia ({RuntimeInformation.OSDescription})");
            var avaloniaWebView = new AvaloniaWebViewControl();
            controlType = typeof(AvaloniaWebViewControl);
            return avaloniaWebView;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ WebView.Avalonia 创建失败: {ex.Message}");
        }
    }
    
    // 3. 降级方案：使用占位符
    Debug.WriteLine("⚠️ 使用降级方案：占位符 WebView");
    return null;
}
```

### 4. 测试 macOS 和 Linux 功能 (2小时)

**macOS 测试**：
```bash
# 在 macOS 上编译运行
cd /path/to/NcfDesktopApp.GUI
dotnet build -r osx-arm64  # Apple Silicon
dotnet build -r osx-x64    # Intel Mac
dotnet run
```

**Linux 测试**：
```bash
# Ubuntu/Debian
sudo apt-get install libwebkit2gtk-4.0-dev

# 编译运行
dotnet build -r linux-x64
dotnet run
```

## ✅ 验收标准

### 功能验收
- [ ] macOS 平台成功加载 WebView（基于 WKWebView）
- [ ] Linux 平台成功加载 WebView（基于 WebKitGTK）
- [ ] NCF URL 可以在 WebView 中正确显示
- [ ] 前进/后退/刷新功能正常
- [ ] 导航事件正确触发

### 技术验收
- [ ] macOS 编译通过
- [ ] Linux 编译通过
- [ ] 无平台特定的编译错误
- [ ] 依赖正确安装

### 质量验收
- [ ] 错误处理完整（依赖缺失等）
- [ ] 日志输出详细
- [ ] 降级方案可用

## 🧪 测试方法

### macOS 测试
1. 在 macOS 11+ 系统上运行
2. 验证 WKWebView 加载
3. 测试 NCF 网页显示
4. 测试导航功能

### Linux 测试
1. 安装 WebKitGTK 依赖
2. 运行应用
3. 验证 WebView 加载
4. 测试所有功能

### 预期结果
- ✅ 所有平台都能显示内嵌网页
- ✅ 导航功能跨平台一致
- ✅ 错误提示清晰

## 📝 注意事项

### ⚠️ Linux 依赖
必须安装 WebKitGTK：
```bash
# Ubuntu/Debian
sudo apt-get install libwebkit2gtk-4.0-dev libgtk-3-dev

# Fedora
sudo dnf install webkit2gtk3-devel gtk3-devel
```

### ⚠️ macOS 权限
应用可能需要网络访问权限，确保 Info.plist 配置正确。

### 💡 最佳实践
- 使用反射处理 API 差异
- 提供详细的错误提示
- 实现降级方案

## 🔗 相关资源
- [WebView.Avalonia GitHub](https://github.com/OutSystems/WebView)
- [WebKitGTK 文档](https://webkitgtk.org/)
- [macOS WKWebView 文档](https://developer.apple.com/documentation/webkit/wkwebview)

---

**状态**: ⏳ 待开始  
**优先级**: 🔥 高  
**依赖**: step-01 (可并行开发)  
**预计时间**: 6.5小时

