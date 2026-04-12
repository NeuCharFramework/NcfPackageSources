[中文版](TESTING_GUIDE.cn.md)

# WebView2 automatic installation test guide

## 🎯 Test purpose

Test the automatic detection and installation of WebView2 for the NCF desktop application.

---

## 📁 Tool files

The following tool files have been created for testing use:

1. **`quick-clean-webview2.bat`** ⭐ - Batch launcher (needs to run as administrator)
2. **`quick-clean-webview2.ps1`** 🔧 - PowerShell cleaning script (core tool)

---

## 🚀 Rapid testing process

### Method A: Use batch file (easiest)

1. **Right click** `quick-clean-webview2.bat`
2. Select **"Run as administrator"**
3. Read the instructions and press any key to confirm
4. Wait for the cleanup to complete
5. Launch the NCF app test in plain PowerShell

### Method B: Use PowerShell (recommended, the output is more detailed)```powershell
# 1. 以管理员身份打开 PowerShell
#    右键点击 PowerShell 图标 → "以管理员身份运行"

# 2. 进入工具目录
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\build-tool

# 3. 运行清理脚本
.\quick-clean-webview2.ps1

# 4. 等待清理完成，应该显示：
# 🎉 SUCCESS: Ready to test!
# Registry:  ✅ Cleaned
# Files:     ✅ Cleaned (或 ⚠️ Still exist - 这也没问题！)
```### 💡 IMPORTANT NOTE

- **Just clean the registry**: Our application detects WebView2 through the registry, whether the file exists does not affect the test
- **File deletion may fail**: File deletion may fail due to system lock, but this does not affect the test!
- **Registry cleaning is enough**: As long as the registry cleaning is successful, the application will think that WebView2 is not installed

---

## 📝 Detailed test steps

### Step 1: Clean WebView2

**Using PowerShell (Admin)**:```powershell
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\build-tool
.\quick-clean-webview2.ps1
```**Expected Output**:```
========================================
WebView2 Cleanup for Testing
========================================

Step 1: Stopping WebView2 processes...
  - Stopping: msedgewebview2 (PID: 1234)
  - Stopping: msedgewebview2 (PID: 5678)
  ✅ Stopped 2 process(es)
  ⏳ Waiting 3 seconds for processes to fully terminate...

Step 2: Stopping Edge update services...
  (可能没有服务需要停止)

Step 3: Deleting registry keys...
  (This is the CRITICAL step for testing auto-installation)
  ✅ Deleted: HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\...
  ✓ Not found (already clean): HKLM:\SOFTWARE\Microsoft\EdgeUpdate\...

Step 4: Deleting installation files...
  (This is OPTIONAL - file deletion may fail due to system locks)
  - Found: C:\Program Files (x86)\Microsoft\EdgeWebView\Application
  - Attempting to delete (5 retries)...
  ✅ Files deleted successfully!
  
  或者（如果文件删除失败）：
  ⚠️  Could not delete files: 对路径的访问被拒绝。
     Reason: Files may be locked by system services or Edge browser

========================================
Verification & Results
========================================

Registry:  ✅ Cleaned
Files:     ✅ Cleaned  (或 ⚠️ Still exist - 这也没问题！)

========================================
🎉 SUCCESS: Ready to test!

The registry has been cleaned, which is sufficient
for testing the auto-installation feature.

Note: Installation files still exist, but this is OK!
      The app checks registry, not files.

========================================
Next Steps:
========================================
1. Open a NORMAL PowerShell window (not admin)
2. Navigate to your publish folder:
   cd Y:\...\publish-self-contained\win-arm64-final
3. Run: .\NcfDesktopApp.GUI.exe
4. Watch the logs for auto-installation!
```---

### Step 2: Verify cleanup (optional)

Manual verification:```powershell
# 检查注册表
$regPath = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
Test-Path $regPath
# 应该返回: False

# 检查文件
Test-Path "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application"
# 应该返回: False
```---

### Step 3: Start application test automatic installation```powershell
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\publish-self-contained\win-arm64-final

.\NcfDesktopApp.GUI.exe
```---

### Step 4: Observe the automatic installation process

In the app's Settings tab, you should see:```
🚀 正在初始化 NCF 桌面应用程序...
📋 检查最新版本...
   最新版本: v0.x.x
🌐 正在初始化内置浏览器...
🔍 检查 WebView2 Runtime...
❌ WebView2 Runtime 未安装
   检测到 WebView2 未安装，正在自动安装...
   正在下载 WebView2 Runtime...
   下载中... 10%
   下载中... 30%
   下载中... 50%
   下载完成，正在安装...
   正在安装... 60%
   正在安装... 80%
   正在安装... 90%
   安装完成，正在验证...
   WebView2 Runtime 安装成功
✅ WebView2 Runtime 已就绪 (版本: xxx.x.xxxx.xx)
✅ WebView2 Runtime 已就绪
✅ 应用程序初始化完成
```**Installation Time**: Typically 1-3 minutes, depending on network speed.

---

### Step 5: Verify functionality

1. **Built-in browser works**
   - Click "Start NCF"
   - Wait for the NCF application to start
   - Browser tabs open automatically
   - Display NCF web content

2. **Address bar is available**
   - Enter the new URL in the address bar
   - Press Enter or click the "→" button
   - Navigate to new page

3. **Close tab confirmation**
   - Click the "✕ Close tab" button
   - Show confirmation dialog
   - Select "Close" or "Cancel"

---

## 🔄 Repeat test

Want to test again? Just:

1. Run `.\quick-clean-webview2.ps1`
2. Restart the application
3. Observe the automatic installation process

---

## ⚠️ Troubleshooting

### Question 1: The cleanup script reported an error of "Insufficient Permissions"

**Solution**:```powershell
# 确保以管理员身份运行 PowerShell
# 右键点击 PowerShell 图标 → "以管理员身份运行"
```### Problem 2: The file cannot be deleted (occupied)

**This is a normal phenomenon and does not affect the test! **

Our application detects WebView2 through the registry and does not check files. As long as the registry cleanup is successful, the test can proceed.

**If you want to completely delete the file**:```powershell
# 1. 关闭所有浏览器和应用（Edge、Chrome 等）
# 2. 打开任务管理器（Ctrl+Shift+Esc）
# 3. 结束所有 msedgewebview2* 进程
# 4. 或者重启计算机
# 5. 再次运行清理脚本
```But usually this is not necessary!

### Problem 3: The batch file displays garbled characters

**Solution**:```powershell
# 不使用 .bat 文件，直接运行 PowerShell 脚本
.\quick-clean-webview2.ps1
```### Problem 4: Automatic installation failed

**CHECK**:
1. Is the network connection normal?
2. Is the firewall blocking?
3. Is there enough disk space (at least 200MB)

**If the installation fails**, the application will display a friendly error interface, providing:
- 🌍 Open in external browser
- ⬇️ Manually download WebView2 link

---

## 📊 Test Checklist

**Cleaning Phase**:
- [ ] The cleaning tool was successfully run as administrator.
- [ ] Registry has been cleaned (✅ Cleaned)
- [ ] File cleaning (✅ Cleaned or ⚠️ Still exist are both acceptable)

**Automatic installation phase**:
- [ ] WebView2 not installed detected when application started
- [ ] Automatically download WebView2 Bootstrapper
- [ ] Silently install WebView2 Runtime
- [ ] shows installation progress (percent update)
- [ ] Verification of successful installation (or timeout warning but continuing)
- [ ] The built-in browser displays web content normally

**Functional Verification Phase**:
- [ ] Address bar editable and navigable
- [ ] Press Enter or click the "→" button to navigate
- [ ] Show confirmation dialog when closing tab
- [ ] The process is completely terminated after clicking "Close"

---

## 💡 Quick command reference```powershell
# 清理 WebView2
cd Y:\...\NcfDesktopApp.GUI\build-tool
.\quick-clean-webview2.ps1

# 启动应用
cd Y:\...\NcfDesktopApp.GUI\publish-self-contained\win-arm64-final
.\NcfDesktopApp.GUI.exe

# 手动验证
Test-Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
Test-Path "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application"
```---

## 📚 Related documents

- [WebView2 Automation Function Description](../WEBVIEW2_AUTO_SETUP.md)
- [WebView2 Cleanup Guide](./WEBVIEW2_CLEANUP_GUIDE.md)
- [Change Log](../CHANGELOG_WEBVIEW.md)

---

**Good luck with the test! ** 🎉

---

## ❓ Frequently Asked Questions (FAQ)

### Q1: Why does file deletion fail but SUCCESS is still displayed?

**A**: Because our application **only checks the registry** to determine whether WebView2 is installed, and does not check files. As long as the registry cleanup is successful, the application will think that WebView2 is not installed, triggering the automatic installation process.

### Q2: Do I need to completely delete the WebView2 files?

**A**: **Not required**. A registry cleanup is enough. File removal is only for a more thorough cleanup, but is not necessary to test the automatic installation functionality.

### Q3: What should I do if the automatic installation displays "Verification Timeout"?

**A**: This may be due to delayed registry updates. But the installer has run successfully (exit code 0), so:
- The application will continue to run
- WebView is most likely already available
- If it doesn't work, just restart the app

### Q4: Can it be cleaned in non-administrator mode?

**A**: **No**. Deleting the `HKLM` path from the registry requires administrator privileges. But testing the app itself doesn't require administrator rights.

### Q5: Can I still use the Edge browser after cleaning?

**A**: **Yes**. WebView2 Runtime and Edge browser are independent, and cleaning WebView2 will not affect the use of Edge browser.

---

**Last update**: 2025-11-14 (Updated cleaning process and file deletion instructions)
