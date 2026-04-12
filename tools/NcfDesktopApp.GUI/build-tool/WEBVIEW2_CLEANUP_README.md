[中文版](WEBVIEW2_CLEANUP_README.cn.md)

# WebView2 Cleanup Tool - Quick Guide

## 🎯 Purpose

Used to clean up the registry keys for the WebView2 Runtime in order to test the automatic installation feature of the NCF Desktop App.

---

## 📦 File Description

### `quick-clean-webview2.bat` ⭐ Recommended
- **PURPOSE**: Batch launcher to automatically run cleanup scripts with correct permissions
- **How to use**: Right click → "Run as administrator"
- **Advantages**: Simple and convenient, one-click cleaning

### `quick-clean-webview2.ps1` 🔧 Core script
- **Use**: PowerShell cleanup script (core tool)
- **How to use**: Run in Administrator PowerShell
- **Advantages**: Detailed output, suitable for debugging

---

## 🚀 Quick to use

### The simplest method (recommended)

1. Find `quick-clean-webview2.bat`
2. **Right click** → Select **"Run as administrator"**
3. Read the prompts and press any key to continue
4. Wait for the cleanup to complete
5. Done!

### PowerShell method (more detailed output)```powershell
# 1. 右键点击 PowerShell 图标 → "以管理员身份运行"

# 2. 进入工具目录
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\build-tool

# 3. 运行清理脚本
.\quick-clean-webview2.ps1

# 4. 等待清理完成
```---

## ✅ Success sign

After successful cleaning, you will see:```
========================================
Verification & Results
========================================

Registry:  ✅ Cleaned
Files:     ✅ Cleaned  (或 ⚠️ Still exist)

========================================
🎉 SUCCESS: Ready to test!
```**Important**: Even if the file shows `⚠️ Still exist`, it is successful! Our application only checks the registry.

---

## 🔍 What to clean up?

### CRITICAL - must succeed)
- ✅ **WebView2 Registry Key**
  - `HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
  - This is the key for your application to detect WebView2!

### Optional cleanup (OPTIONAL - may fail)
- 🗑️ **WebView2 installation file**
  - `C:\Program Files (x86)\Microsoft\EdgeWebView\Application`
  - File deletion may fail due to system lockup
  - **Does not affect testing**

### Process Cleanup
- 🛑 Stop all `msedgewebview2` processes
- 🛑 Stop Edge update service if running

---

## ⚠️ FAQ

### Q: Why do I need administrator rights?
**A**: Deleting the `HKLM` registry path requires administrator privileges.

### Q: File deletion failed, what should I do?
**A**: **Don’t worry**! As long as the registry cleaning is successful, you can test it. The application detects WebView2 through the registry and does not check files.

### Q: How to verify that the cleanup is successful?
**A**: Running the following command should return `False`:```powershell
Test-Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
```### Q: How to test after cleaning?
**A**: Run in a **normal** PowerShell window (no administrator required):```powershell
cd Y:\...\publish-self-contained\win-arm64-final
.\NcfDesktopApp.GUI.exe
```---

## 📋 Complete testing process

1. **Clean up WebView2**```
   右键 "quick-clean-webview2.bat" → "以管理员身份运行"
   ```2. **Start the application** (normal PowerShell)```powershell
   cd Y:\...\publish-self-contained\win-arm64-final
   .\NcfDesktopApp.GUI.exe
   ```3. **Observation log**
   - App will detect that WebView2 is not installed
   - Automatically download and install
   - Show progress percentage
   - WebView works normally after installation is complete

---

## 📚 Detailed documentation

For more detailed testing steps and troubleshooting, please refer to:
- [`TESTING_GUIDE.md`](./TESTING_GUIDE.md) - Complete Testing Guide

---

## 🔄 Repeat test

Want to test the automated installation again? Just:

1. Run the cleaning tool again
2. Restart the application
3. Observe the automatic installation process

It's that simple!

---

**Tip**: The cleanup tool will not affect the use of the Edge browser or other applications.

**Last updated**: 2025-11-14
