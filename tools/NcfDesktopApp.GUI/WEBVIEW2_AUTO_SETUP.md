[中文版](WEBVIEW2_AUTO_SETUP.cn.md)

# WebView2 automatic detection and installation function description

## 📋 Function Overview

This application has integrated WebView2 Runtime automatic detection and installation functions to ensure that Windows users can use the built-in browser smoothly.

---

## 🎯 Main functions

### 1. Automatic detection at startup

When the application starts, it automatically detects whether the WebView2 Runtime is installed:

- ✅ **Installed**: Display version information and use the built-in browser directly
- ❌ **Not Installed**: Automatically download and install WebView2 Runtime

### 2. Automatic installation process

If it detects that WebView2 is not installed, the application will:

1. **Download** WebView2 Bootstrapper (about 2MB)
   - Use the official link: `https://go.microsoft.com/fwlink/p/?LinkId=2124703`
   - Automatically detect system architecture (x64, x86, ARM64)

2. **Silent installation** WebView2 Runtime
   - Use `/silent /install` parameter
   - No manual operation required by user
   - Shows real-time installation progress

3. **Verify installation**
   - Confirm successful installation through registry
   - Get the installed version number

### 3. Friendly error handling

If WebView2 fails to install or initialize, a friendly error interface will be displayed:

#### Error messages include:
- ❌ **Cause of failure**:
  - WebView2 Runtime is not installed or the installation failed
  - Insufficient system permissions
  - Component versions are incompatible

#### Solutions provided:
- 🌍 **Open in external browser** - Open NCF in the system default browser with one click
- ⬇️ **Download WebView2 Runtime** - Jump to the official download page to install manually
- 💡 **Restart Tip** - Restart the app after installation to use the built-in browser

---

## 🔍 Technical implementation

### Core components

#### 1. `WebView2Service.cs`

Responsible for the detection and installation of WebView2 Runtime:```csharp
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
```**Detection method**:
- Read the registry: `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
- or 64-bit path: `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
- Read the `pv` key to get the version number

**Installation process**:
1. Download WebView2 Bootstrapper to the temporary directory
2. Run the installer using `Process.Start()`
3. Wait for the installation to complete (up to 5 minutes)
4. Verify the registry to confirm successful installation
5. Clean up temporary files

#### 2. `MainWindowViewModel.cs`

Integrate WebView2 detection during application initialization:```csharp
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
```#### 3. `EmbeddedWebView.cs`

Enhanced error handling interface:```csharp
private void ShowFallbackView()
{
    // 创建友好的错误界面
    // - 错误标题和描述
    // - 失败原因列表
    // - 解决方案按钮：
    //   1. 在外部浏览器中打开
    //   2. 下载 WebView2 Runtime（仅 Windows）
}
```---

## 📊 User experience process

### Scenario 1: First run (WebView2 not installed)```
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
```**Log output example**:```
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
```### Scenario 2: WebView2 is installed```
启动应用
    ↓
检测到 WebView2 已安装 (版本: 120.0.6099.199)
    ↓
✅ 直接使用内置浏览器
```**Log output example**:```
🚀 正在初始化 NCF 桌面应用程序...
🌐 正在初始化内置浏览器...
🔍 检查 WebView2 Runtime...
✅ WebView2 Runtime 已就绪 (版本: 120.0.6099.199)
✅ WebView2 Runtime 已就绪
✅ 应用程序初始化完成
```### Scenario 3: Installation failed```
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
```**Error interface**:```
❌ 内置浏览器初始化失败

无法加载内置浏览器组件。这可能是因为：
  • WebView2 Runtime 未安装或安装失败
  • 系统权限不足
  • 组件版本不兼容

您可以尝试以下解决方案：

[🌍 在外部浏览器中打开]

[⬇️ 下载 WebView2 Runtime]

💡 下载并安装 WebView2 后，重启应用即可使用内置浏览器
```---

## 🛠️ Manually install WebView2

If the automatic installation fails, the user can install it manually:

### Method 1: Use the in-app button
1. Click the **"⬇️ Download WebView2 Runtime"** button
2. The browser will open the official download page
3. Download and run the installer
4. Restart the NCF desktop application

### Method 2: Direct download
Visit the official Microsoft download page:
- **Bootstrapper (recommended)**: https://go.microsoft.com/fwlink/p/?LinkId=2124703
- **Offline installation package**: https://developer.microsoft.com/microsoft-edge/webview2/

### Method 3: Via command line
Run in PowerShell (admin rights):```powershell
# 下载并安装
$url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
$output = "$env:TEMP\WebView2Bootstrapper.exe"
Invoke-WebRequest -Uri $url -OutFile $output
Start-Process -FilePath $output -ArgumentList "/silent /install" -Wait
```---

## ✅ Verify installation

### Via registry
Open the Registry Editor (`regedit`) and check the following paths:```
HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}
```View the value of the `pv` key, which is the WebView2 version number.

### Via PowerShell```powershell
Get-AppxPackage -Name "*WebView2*"
```### Via application log
Launch the NCF desktop application and view the version information in the log output.

---

## 🔧 Troubleshooting

### Problem 1: Automatic installation failed

**Possible reasons**:
- Internet connection issues
- Firewall/antivirus software blocking
- Insufficient disk space
- Insufficient system permissions

**Solution**:
1. Check network connection
2. Temporarily disable firewall/antivirus software
3. Make sure you have at least 200MB of free disk space
4. Run the application as administrator
5. Manual download and installation (see above)

### Problem 2: WebView initialization failed

**Possible reasons**:
- WebView2 version is out of date
- System components are damaged
- Permission issues

**Solution**:
1. Update WebView2 to the latest version
2. Reinstall WebView2
3. Run the application as administrator
4. Use an external browser (temporary solution)

### Problem 3: Failed to open in external browser

**Possible reasons**:
- No default browser
- Broken browser association

**Solution**:
1. Set default browser
2. Manually copy the URL to the browser address bar

---

## 📝 Log level

The application logs detailed WebView2 detection and installation information:

- **ℹ️ Info**: normal process information
- **✅ Success**: Operation completed successfully
- **⚠️ Warning**: warning message (does not affect main functions)
- **❌ Error**: Error message (requires user intervention)
- **🔍 Debug**: debugging information

---

## 🔄 Update strategy

The WebView2 Runtime is automatically updated through the Microsoft Edge update channel:
- Automatically detect new versions
- Silent updates in the background
- No user intervention required

The application displays the currently installed version of WebView2 on startup.

---

## 📚 Reference resources

- [WebView2 official document](https://learn.microsoft.com/microsoft-edge/webview2/)
- [WebView2 download page](https://developer.microsoft.com/microsoft-edge/webview2/)
- [WebView.Avalonia GitHub](https://github.com/MicroSugarDeveloperOrg/Webviews.Avalonia)

---

## 💡 Best Practices

1. **First run**: Make sure the network connection is good and let the application automatically complete the WebView2 installation
2. **Enterprise Deployment**: WebView2 Runtime can be pre-installed to the system image
3. **Offline environment**: Download the offline installation package in advance, install it manually and then run the application
4. **Failure recovery**: If you encounter problems, give priority to using the "Open in external browser" function

---

**Last updated**: 2025-11-14
**Version**: 1.0.0
