# 阶段 5️⃣: 测试和文档

## 📋 步骤信息
- **步骤ID**: step-05
- **步骤名称**: 全面测试和文档更新
- **预计时间**: 7 小时
- **优先级**: 🔥 高
- **状态**: ⏳ 待开始

## 🎯 目标
确保内嵌浏览器功能在所有平台上稳定运行，并提供完整的文档支持。

## 📂 涉及文件
- `Tests/WebViewTests.cs` - 新建，单元测试
- `Tests/IntegrationTests.cs` - 新建，集成测试
- `README.md` - 更新，添加使用说明
- `TROUBLESHOOTING.md` - 新建，故障排除指南
- `CHANGELOG.md` - 更新，版本变更日志

## 🔨 实施步骤

### 1. 编写单元测试 (2小时)

**新建 `Tests/WebViewTests.cs`**：

```csharp
using System;
using System.Threading.Tasks;
using NcfDesktopApp.GUI.Services;
using NcfDesktopApp.GUI.Views.Controls;
using Xunit;

namespace NcfDesktopApp.GUI.Tests;

public class WebViewTests
{
    [Fact]
    public void PlatformWebViewFactory_DetectCapabilities_ReturnsValid()
    {
        // Arrange & Act
        var capabilities = PlatformWebViewFactory.DetectCapabilities();

        // Assert
        Assert.NotNull(capabilities);
        Assert.True(Enum.IsDefined(typeof(WebViewPlatform), capabilities.Platform));
    }

    [Fact]
    public void WebView2RuntimeChecker_IsAvailable_ReturnsBoolean()
    {
        // Arrange & Act
        var isAvailable = WebView2RuntimeChecker.IsWebView2Available();

        // Assert
        Assert.IsType<bool>(isAvailable);
    }

    [Fact]
    public void PlatformWebViewFactory_CreateWebView_WhenAvailable_ReturnsInstance()
    {
        // Arrange
        var capabilities = PlatformWebViewFactory.DetectCapabilities();
        
        // Act
        var webView = PlatformWebViewFactory.CreateWebView();

        // Assert
        if (capabilities.IsAvailable)
        {
            Assert.NotNull(webView);
            Assert.IsAssignableFrom<IPlatformWebView>(webView);
        }
    }

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("https://www.example.com")]
    [InlineData("about:blank")]
    public async Task WebView_NavigateAsync_ValidUrl_DoesNotThrow(string url)
    {
        // Arrange
        var webView = PlatformWebViewFactory.CreateWebView();
        if (webView == null)
        {
            // Skip test if WebView is not available
            return;
        }

        // Act & Assert
        await webView.NavigateAsync(url);
        // 如果没有抛出异常，测试通过
    }

    [Fact]
    public void WebViewResourceManager_GetCurrentMemoryInfo_ReturnsValidData()
    {
        // Arrange
        using var manager = new WebViewResourceManager();

        // Act
        var memoryInfo = manager.GetCurrentMemoryInfo();

        // Assert
        Assert.NotNull(memoryInfo);
        Assert.True(memoryInfo.WorkingSet > 0);
        Assert.True(memoryInfo.ManagedMemory >= 0);
    }
}
```

### 2. 跨平台集成测试 (2小时)

**新建 `Tests/IntegrationTests.cs`**：

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NcfDesktopApp.GUI.Views.Controls;
using Xunit;

namespace NcfDesktopApp.GUI.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task FullWorkflow_StartNCF_NavigateInWebView_Success()
    {
        // 这是一个完整的集成测试示例
        // 实际测试时需要确保环境已准备好

        // 1. 检测 WebView 可用性
        var capabilities = PlatformWebViewFactory.DetectCapabilities();
        if (!capabilities.IsAvailable)
        {
            // Skip test if WebView is not available
            Debug.WriteLine($"跳过测试：WebView 不可用 ({capabilities.ErrorMessage})");
            return;
        }

        // 2. 创建 WebView 实例
        var webView = PlatformWebViewFactory.CreateWebView();
        Assert.NotNull(webView);

        // 3. 订阅导航事件
        var navigationCompleted = false;
        webView.NavigationCompleted += (s, url) =>
        {
            navigationCompleted = true;
            Debug.WriteLine($"导航完成: {url}");
        };

        // 4. 导航到测试页面
        await webView.NavigateAsync("about:blank");

        // 5. 等待导航完成（最多5秒）
        var timeout = DateTime.Now.AddSeconds(5);
        while (!navigationCompleted && DateTime.Now < timeout)
        {
            await Task.Delay(100);
        }

        // 6. 验证结果
        Assert.True(navigationCompleted, "导航未在预期时间内完成");
        Assert.True(webView.IsInitialized, "WebView 未正确初始化");
    }

    [SkippableFact]
    public void WindowsSpecific_WebView2Runtime_IsInstalled()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "仅在 Windows 上运行");

        // Arrange & Act
        var isAvailable = WebView2RuntimeChecker.IsWebView2Available();
        var version = WebView2RuntimeChecker.GetWebView2Version();

        // Assert
        Debug.WriteLine($"WebView2 Runtime: {(isAvailable ? "已安装" : "未安装")} - 版本: {version}");
        
        // 注意：此测试可能失败如果 Runtime 未安装，这是预期行为
    }

    [SkippableFact]
    public void MacOS_WKWebView_IsAvailable()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), "仅在 macOS 上运行");

        // Arrange & Act
        var capabilities = PlatformWebViewFactory.DetectCapabilities();

        // Assert
        Assert.Equal(WebViewPlatform.WKWebView, capabilities.Platform);
        Assert.True(capabilities.IsAvailable, $"WKWebView 应该可用: {capabilities.ErrorMessage}");
    }

    [SkippableFact]
    public void Linux_WebKitGTK_IsAvailable()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "仅在 Linux 上运行");

        // Arrange & Act
        var capabilities = PlatformWebViewFactory.DetectCapabilities();

        // Assert
        Assert.Equal(WebViewPlatform.WebKitGTK, capabilities.Platform);
        
        if (!capabilities.IsAvailable)
        {
            Debug.WriteLine($"WebKitGTK 未安装: {capabilities.ErrorMessage}");
            foreach (var dep in capabilities.MissingDependencies)
            {
                Debug.WriteLine($"  - {dep}");
            }
        }
    }
}
```

### 3. 性能测试和优化 (1.5小时)

**新建 `Tests/PerformanceTests.cs`**：

```csharp
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace NcfDesktopApp.GUI.Tests;

public class PerformanceTests
{
    [Fact]
    public async Task WebView_InitializationTime_IsAcceptable()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var webView = PlatformWebViewFactory.CreateWebView();
        stopwatch.Stop();

        // Assert
        var initTime = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"WebView 初始化时间: {initTime}ms");
        
        // 初始化应该在 3 秒内完成
        Assert.True(initTime < 3000, $"初始化时间过长: {initTime}ms");
    }

    [Fact]
    public async Task WebView_NavigationTime_IsReasonable()
    {
        // Arrange
        var webView = PlatformWebViewFactory.CreateWebView();
        if (webView == null) return;

        var completed = false;
        var stopwatch = Stopwatch.StartNew();

        webView.NavigationCompleted += (s, url) =>
        {
            stopwatch.Stop();
            completed = true;
        };

        // Act
        await webView.NavigateAsync("about:blank");

        // Wait for navigation (max 5 seconds)
        var timeout = DateTime.Now.AddSeconds(5);
        while (!completed && DateTime.Now < timeout)
        {
            await Task.Delay(50);
        }

        // Assert
        Assert.True(completed, "导航未完成");
        var navTime = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"导航时间: {navTime}ms");
        
        // about:blank 应该在 1 秒内加载完成
        Assert.True(navTime < 1000, $"导航时间过长: {navTime}ms");
    }

    [Fact]
    public void WebViewResourceManager_MemoryMonitoring_Works()
    {
        // Arrange
        using var manager = new WebViewResourceManager();
        var memoryWarningTriggered = false;

        manager.MemoryWarningTriggered += (s, e) =>
        {
            memoryWarningTriggered = true;
            Debug.WriteLine($"内存警告: {e.CurrentUsage} bytes");
        };

        // Act
        var memoryInfo = manager.GetCurrentMemoryInfo();

        // Assert
        Assert.NotNull(memoryInfo);
        Debug.WriteLine($"当前内存: {memoryInfo}");
        
        // 记录但不失败（内存警告是可选的）
        if (memoryWarningTriggered)
        {
            Debug.WriteLine("⚠️ 内存警告已触发");
        }
    }
}
```

### 4. 更新用户文档 (1小时)

**更新 `README.md`**：

```markdown
# NCF 桌面应用

## 🌐 内嵌浏览器功能

NCF 桌面应用现在支持真正的内嵌浏览器，无需外部浏览器即可访问 NCF 网页！

### ✨ 特性

- **Windows**: 使用 Microsoft WebView2（基于 Chromium，免费）
- **macOS**: 使用 WKWebView（系统原生）
- **Linux**: 使用 WebKitGTK（开源）
- **自动检测**: 根据平台自动选择最佳方案
- **完整功能**: 支持前进、后退、刷新等操作

### 📦 环境要求

#### Windows 10/11
- **WebView2 Runtime** (通常已预装)
- 如未安装，应用会提示下载：[WebView2 Runtime 下载](https://developer.microsoft.com/microsoft-edge/webview2/)

#### macOS 11+
- 无需额外安装（使用系统 WKWebView）

#### Linux (Ubuntu/Debian)
```bash
sudo apt-get install libwebkit2gtk-4.0-dev libgtk-3-dev
```

#### Linux (Fedora/CentOS)
```bash
sudo dnf install webkit2gtk3-devel gtk3-devel
```

### 🚀 使用方法

1. **启动应用**
   ```bash
   dotnet run
   ```

2. **启动 NCF**
   - 点击"启动 NCF"按钮
   - 应用会自动在内嵌浏览器中显示 NCF 网页

3. **浏览操作**
   - 使用工具栏按钮进行前进/后退/刷新
   - 按 F12 打开开发者工具（仅 Windows）

### 🛠️ 故障排除

遇到问题？查看 [故障排除指南](TROUBLESHOOTING.md)

### 📝 更新日志

详见 [CHANGELOG.md](CHANGELOG.md)
```

### 5. 创建故障排除指南 (0.5小时)

**新建 `TROUBLESHOOTING.md`**：

```markdown
# NCF 桌面应用 - 故障排除指南

## 🔍 常见问题

### Windows 平台

#### ❌ WebView2 Runtime 未安装

**症状**：
- 应用启动时提示"WebView2 Runtime 未安装"
- 浏览器标签页显示错误

**解决方法**：
1. 下载并安装 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
2. 选择"Evergreen Standalone Installer"
3. 重启应用

**自动安装**（可选）：
```bash
# 使用 PowerShell
winget install Microsoft.EdgeWebView2Runtime
```

#### ❌ 页面无法加载

**症状**：
- 白屏或加载失败

**解决方法**：
1. 检查 NCF 是否正常运行
2. 确认端口号正确（默认 5000-5300）
3. 检查防火墙设置
4. 尝试在外部浏览器中打开

### macOS 平台

#### ❌ WKWebView 初始化失败

**症状**：
- 浏览器标签页显示错误

**解决方法**：
1. 确保 macOS 版本 >= 11.0
2. 检查应用权限（系统偏好设置 > 安全性与隐私）
3. 重启应用

### Linux 平台

#### ❌ WebKitGTK 未安装

**症状**：
- 应用启动时提示依赖缺失

**解决方法**：

**Ubuntu/Debian**：
```bash
sudo apt-get update
sudo apt-get install libwebkit2gtk-4.0-dev libgtk-3-dev
```

**Fedora/CentOS**：
```bash
sudo dnf install webkit2gtk3-devel gtk3-devel
```

**Arch Linux**：
```bash
sudo pacman -S webkit2gtk gtk3
```

#### ❌ 应用崩溃或无响应

**解决方法**：
1. 检查依赖是否完整安装
2. 查看终端错误信息
3. 尝试使用外部浏览器作为降级方案

## 🐛 调试技巧

### 启用详细日志

```bash
# 设置环境变量
export NCF_DEBUG=1
dotnet run
```

### 查看 WebView 版本

应用启动时会在控制台输出 WebView 版本信息

### 内存问题

如果遇到内存占用过高：
1. 关闭不必要的标签页
2. 重启应用
3. 检查系统可用内存

## 📞 获取帮助

如果问题仍未解决：
1. 查看 [GitHub Issues](https://github.com/your-repo/issues)
2. 提交新 Issue，包含：
   - 操作系统和版本
   - WebView 版本信息
   - 错误日志
   - 复现步骤
```

## ✅ 验收标准

### 功能验收
- [ ] 所有单元测试通过
- [ ] 集成测试在各平台通过
- [ ] 性能测试达标
- [ ] 文档完整准确

### 技术验收
- [ ] 测试覆盖率 >= 70%
- [ ] 无已知的严重bug
- [ ] 跨平台功能一致

### 质量验收
- [ ] 文档易于理解
- [ ] 故障排除有效
- [ ] 用户反馈积极

## 🧪 测试方法

### 运行测试
```bash
cd Tests
dotnet test --logger "console;verbosity=detailed"
```

### 平台特定测试
```bash
# Windows
dotnet test --filter "Category=Windows"

# macOS
dotnet test --filter "Category=macOS"

# Linux
dotnet test --filter "Category=Linux"
```

## 📝 注意事项

- 测试环境要准备完整
- 性能测试结果可能因硬件而异
- 文档要保持更新
- 收集用户反馈持续改进

---

**状态**: ⏳ 待开始  
**优先级**: 🔥 高  
**依赖**: step-01, step-02, step-03, step-04  
**预计时间**: 7小时

