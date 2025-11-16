# WebView2 清理工具 - 快速指南

## 🎯 用途

用于清理 WebView2 Runtime 的注册表项，以便测试 NCF Desktop App 的自动安装功能。

---

## 📦 文件说明

### `3-quick-clean-webview2.bat` ⭐ 推荐使用
- **用途**: 批处理启动器，自动以正确的权限运行清理脚本
- **使用方法**: 右键点击 → "以管理员身份运行"
- **优点**: 简单方便，一键清理

### `quick-clean-webview2.ps1` 🔧 核心脚本
- **用途**: PowerShell 清理脚本（核心工具）
- **使用方法**: 在管理员 PowerShell 中运行
- **优点**: 输出详细，适合调试

---

## 🚀 快速使用

### 最简单的方法（推荐）

1. 找到 `3-quick-clean-webview2.bat`
2. **右键点击** → 选择 **"以管理员身份运行"**
3. 阅读提示，按任意键继续
4. 等待清理完成
5. 完成！

### PowerShell 方法（输出更详细）

```powershell
# 1. 右键点击 PowerShell 图标 → "以管理员身份运行"

# 2. 进入工具目录
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\build-tool

# 3. 运行清理脚本
.\quick-clean-webview2.ps1

# 4. 等待清理完成
```

---

## ✅ 成功标志

清理成功后，你会看到：

```
========================================
Verification & Results
========================================

Registry:  ✅ Cleaned
Files:     ✅ Cleaned  (或 ⚠️ Still exist)

========================================
🎉 SUCCESS: Ready to test!
```

**重要**: 即使文件显示 `⚠️ Still exist`，也是成功的！我们的应用只检查注册表。

---

## 🔍 清理了什么？

### 关键清理（CRITICAL - 必须成功）
- ✅ **WebView2 注册表项**
  - `HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
  - 这是应用检测 WebView2 的关键！

### 可选清理（OPTIONAL - 可能失败）
- 🗑️ **WebView2 安装文件**
  - `C:\Program Files (x86)\Microsoft\EdgeWebView\Application`
  - 由于系统锁定，文件删除可能失败
  - **不影响测试**

### 进程清理
- 🛑 停止所有 `msedgewebview2` 进程
- 🛑 停止 Edge 更新服务（如果正在运行）

---

## ⚠️ 常见问题

### Q: 为什么需要管理员权限？
**A**: 删除 `HKLM` 注册表路径需要管理员权限。

### Q: 文件删除失败，怎么办？
**A**: **不用担心**！只要注册表清理成功，就可以测试了。应用通过注册表检测 WebView2，不检查文件。

### Q: 如何验证清理成功？
**A**: 运行以下命令，应该返回 `False`：
```powershell
Test-Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
```

### Q: 清理后如何测试？
**A**: 在**普通** PowerShell 窗口（不需要管理员）中运行：
```powershell
cd Y:\...\publish-self-contained\win-arm64-final
.\NcfDesktopApp.GUI.exe
```

---

## 📋 完整测试流程

1. **清理 WebView2**
   ```
   右键 "3-quick-clean-webview2.bat" → "以管理员身份运行"
   ```

2. **启动应用**（普通 PowerShell）
   ```powershell
   cd Y:\...\publish-self-contained\win-arm64-final
   .\NcfDesktopApp.GUI.exe
   ```

3. **观察日志**
   - 应用会检测到 WebView2 未安装
   - 自动下载并安装
   - 显示进度百分比
   - 安装完成后 WebView 正常工作

---

## 📚 详细文档

更详细的测试步骤和故障排查，请参考：
- [`TESTING_GUIDE.md`](./TESTING_GUIDE.md) - 完整测试指南

---

## 🔄 重复测试

想再次测试自动安装？只需：

1. 再次运行清理工具
2. 重新启动应用
3. 观察自动安装过程

就这么简单！

---

**提示**: 清理工具不会影响 Edge 浏览器或其他应用的使用。

**最后更新**: 2025-11-14

