# WebView2 自动检测和安装功能说明

## 📋 功能概述

本应用已集成 WebView2 Runtime 自动检测和安装功能，确保 Windows 用户能够顺利使用内置浏览器。

---

## 🎯 主要功能

### 1. 启动时自动检测

应用程序启动时会自动检测 WebView2 Runtime 是否已安装：

- ✅ **已安装**：显示版本信息，直接使用内置浏览器
- ❌ **未安装**：自动下载并安装 WebView2 Runtime

### 2. 自动安装流程

如果检测到 WebView2 未安装，应用会：

1. **下载** WebView2 Bootstrapper（约 2MB）
   - 使用官方链接：`https://go.microsoft.com/fwlink/p/?LinkId=2124703`
   - 自动检测系统架构（x64、x86、ARM64）

2. **静默安装** WebView2 Runtime
   - 使用 `/silent /install` 参数
   - 无需用户手动操作
   - 显示实时安装进度

3. **验证安装** 
   - 通过注册表确认安装成功
   - 获取已安装的版本号

### 3. 友好的错误处理

如果 WebView2 安装失败或初始化失败，会显示友好的错误界面：

#### 错误信息包括：
- ❌ **失败原因**：
  - WebView2 Runtime 未安装或安装失败
  - 系统权限不足
  - 组件版本不兼容

#### 提供的解决方案：
- 🌍 **在外部浏览器中打开** - 一键在系统默认浏览器中打开 NCF
- ⬇️ **下载 WebView2 Runtime** - 跳转到官方下载页面手动安装
- 💡 **重启提示** - 安装后重启应用即可使用内置浏览器

---

## 🔍 技术实现

### 核心组件

#### 1. `WebView2Service.cs`

负责 WebView2 Runtime 的检测和安装：

```csharp
public class WebView2Service
{
    // 检查 WebView2 是否已安装
    public bool IsWebView2Installed()
    
    // 获取已安装的 WebView2 版本
    public string? GetInstalledVersion()
    
    // 自动下载并安装 WebView2 Runtime
    public async Task<bool> InstallWebView2RuntimeAsync(IProgress<(string, double)>?)
    
    // 确保 WebView2 已安装（检测+安装）
    public async Task<bool> EnsureWebView2InstalledAsync(IProgress<(string, double)>?)
}
```

**检测方法**：
- 读取注册表：`HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
- 或 64 位路径：`HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
- 读取 `pv` 键获取版本号

**安装流程**：
1. 下载 WebView2 Bootstrapper 到临时目录
2. 使用 `Process.Start()` 运行安装程序
3. 等待安装完成（最多 5 分钟）
4. 验证注册表确认安装成功
5. 清理临时文件

#### 2. `MainWindowViewModel.cs`

在应用初始化时集成 WebView2 检测：

```csharp
private async Task InitializeBrowserAsync()
{
    // 仅在 Windows 上检查
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        AddLog("🔍 检查 WebView2 Runtime...");
        
        // 自动安装
        var installed = await _webView2Service.EnsureWebView2InstalledAsync(progress);
        
        if (!installed)
        {
            // 显示错误信息和解决方案
            HasBrowserError = true;
            BrowserErrorMessage = "WebView2 Runtime 安装失败...";
        }
    }
}
```

#### 3. `EmbeddedWebView.cs`

增强的错误处理界面：

```csharp
private void ShowFallbackView()
{
    // 创建友好的错误界面
    // - 错误标题和描述
    // - 失败原因列表
    // - 解决方案按钮：
    //   1. 在外部浏览器中打开
    //   2. 下载 WebView2 Runtime（仅 Windows）
}
```

---

## 📊 用户体验流程

### 场景 1：首次运行（WebView2 未安装）

```
启动应用
    ↓
检测到 WebView2 未安装
    ↓
自动下载 Bootstrapper (2MB)
    ↓
静默安装 WebView2 Runtime
    ↓
验证安装成功
    ↓
✅ 使用内置浏览器
```

**日志输出示例**：
```
🚀 正在初始化 NCF 桌面应用程序...
🌐 正在初始化内置浏览器...
🔍 检查 WebView2 Runtime...
   检测到 WebView2 未安装，正在自动安装...
   正在下载 WebView2 Runtime...
   下载中...
   下载完成，正在安装...
   正在安装...
   安装完成，正在验证...
   WebView2 Runtime 安装成功
✅ WebView2 Runtime 已就绪
✅ 应用程序初始化完成
```

### 场景 2：WebView2 已安装

```
启动应用
    ↓
检测到 WebView2 已安装 (版本: 120.0.6099.199)
    ↓
✅ 直接使用内置浏览器
```

**日志输出示例**：
```
🚀 正在初始化 NCF 桌面应用程序...
🌐 正在初始化内置浏览器...
🔍 检查 WebView2 Runtime...
✅ WebView2 Runtime 已就绪 (版本: 120.0.6099.199)
✅ WebView2 Runtime 已就绪
✅ 应用程序初始化完成
```

### 场景 3：安装失败

```
启动应用
    ↓
尝试自动安装
    ↓
❌ 安装失败
    ↓
显示错误界面：
  - 错误原因
  - 解决方案按钮
    1. 在外部浏览器中打开
    2. 手动下载 WebView2
```

**错误界面**：
```
❌ 内置浏览器初始化失败

无法加载内置浏览器组件。这可能是因为：
  • WebView2 Runtime 未安装或安装失败
  • 系统权限不足
  • 组件版本不兼容

您可以尝试以下解决方案：

[🌍 在外部浏览器中打开]

[⬇️ 下载 WebView2 Runtime]

💡 下载并安装 WebView2 后，重启应用即可使用内置浏览器
```

---

## 🛠️ 手动安装 WebView2

如果自动安装失败，用户可以手动安装：

### 方法 1：使用应用内按钮
1. 点击 **"⬇️ 下载 WebView2 Runtime"** 按钮
2. 浏览器会打开官方下载页面
3. 下载并运行安装程序
4. 重启 NCF 桌面应用

### 方法 2：直接下载
访问 Microsoft 官方下载页面：
- **Bootstrapper（推荐）**：https://go.microsoft.com/fwlink/p/?LinkId=2124703
- **离线安装包**：https://developer.microsoft.com/microsoft-edge/webview2/

### 方法 3：通过命令行
在 PowerShell 中运行（管理员权限）：

```powershell
# 下载并安装
$url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
$output = "$env:TEMP\WebView2Bootstrapper.exe"
Invoke-WebRequest -Uri $url -OutFile $output
Start-Process -FilePath $output -ArgumentList "/silent /install" -Wait
```

---

## ✅ 验证安装

### 通过注册表
打开注册表编辑器（`regedit`），检查以下路径：
```
HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}
```

查看 `pv` 键的值，即为 WebView2 版本号。

### 通过 PowerShell
```powershell
Get-AppxPackage -Name "*WebView2*"
```

### 通过应用日志
启动 NCF 桌面应用，查看日志输出中的版本信息。

---

## 🔧 故障排查

### 问题 1：自动安装失败

**可能原因**：
- 网络连接问题
- 防火墙/杀毒软件拦截
- 磁盘空间不足
- 系统权限不足

**解决方案**：
1. 检查网络连接
2. 临时禁用防火墙/杀毒软件
3. 确保至少有 200MB 可用磁盘空间
4. 以管理员身份运行应用
5. 手动下载安装（见上文）

### 问题 2：WebView 初始化失败

**可能原因**：
- WebView2 版本过旧
- 系统组件损坏
- 权限问题

**解决方案**：
1. 更新 WebView2 到最新版本
2. 重新安装 WebView2
3. 以管理员身份运行应用
4. 使用外部浏览器（临时方案）

### 问题 3：在外部浏览器中打开失败

**可能原因**：
- 没有默认浏览器
- 浏览器关联损坏

**解决方案**：
1. 设置默认浏览器
2. 手动复制 URL 到浏览器地址栏

---

## 📝 日志级别

应用会在日志中记录详细的 WebView2 检测和安装信息：

- **ℹ️ Info**：正常流程信息
- **✅ Success**：成功完成的操作
- **⚠️ Warning**：警告信息（不影响主要功能）
- **❌ Error**：错误信息（需要用户干预）
- **🔍 Debug**：调试信息

---

## 🔄 更新策略

WebView2 Runtime 会通过 Microsoft Edge 更新通道自动更新：
- 自动检测新版本
- 后台静默更新
- 无需用户干预

应用会在启动时显示当前安装的 WebView2 版本。

---

## 📚 参考资源

- [WebView2 官方文档](https://learn.microsoft.com/microsoft-edge/webview2/)
- [WebView2 下载页面](https://developer.microsoft.com/microsoft-edge/webview2/)
- [WebView.Avalonia GitHub](https://github.com/MicroSugarDeveloperOrg/Webviews.Avalonia)

---

## 💡 最佳实践

1. **首次运行**：确保网络连接良好，让应用自动完成 WebView2 安装
2. **企业部署**：可以预先安装 WebView2 Runtime 到系统镜像
3. **离线环境**：提前下载离线安装包，手动安装后再运行应用
4. **故障恢复**：如遇问题，优先使用"在外部浏览器中打开"功能

---

**最后更新**：2025-11-14  
**版本**：1.0.0

