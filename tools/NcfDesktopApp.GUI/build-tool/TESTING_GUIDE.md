# WebView2 自动安装测试指南

## 🎯 测试目的

测试 NCF 桌面应用的 WebView2 自动检测和安装功能。

---

## 📁 工具文件

已创建以下工具文件供测试使用：

1. **`3-quick-clean-webview2.bat`** ⭐ - 批处理启动器（需以管理员身份运行）
2. **`quick-clean-webview2.ps1`** 🔧 - PowerShell 清理脚本（核心工具）

---

## 🚀 快速测试流程

### 方法 A：使用批处理文件（最简单）

1. **右键点击** `3-quick-clean-webview2.bat`
2. 选择 **"以管理员身份运行"**
3. 阅读说明并按任意键确认
4. 等待清理完成
5. 在普通 PowerShell 中启动 NCF 应用测试

### 方法 B：使用 PowerShell（推荐，输出更详细）

```powershell
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
```

### 💡 重要说明

- **只需清理注册表**：我们的应用通过注册表检测 WebView2，文件是否存在不影响测试
- **文件删除可能失败**：由于系统锁定，文件删除可能失败，但这不影响测试！
- **注册表清理即可**：只要注册表清理成功，应用就会认为 WebView2 未安装

---

## 📝 详细测试步骤

### 第 1 步：清理 WebView2

**使用 PowerShell（管理员）**：

```powershell
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\build-tool
.\quick-clean-webview2.ps1
```

**预期输出**：
```
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
```

---

### 第 2 步：验证清理（可选）

手动验证：

```powershell
# 检查注册表
$regPath = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
Test-Path $regPath
# 应该返回: False

# 检查文件
Test-Path "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application"
# 应该返回: False
```

---

### 第 3 步：启动应用测试自动安装

```powershell
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\publish-self-contained\win-arm64-final

.\NcfDesktopApp.GUI.exe
```

---

### 第 4 步：观察自动安装过程

在应用的"设置"标签页中，你应该看到：

```
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
```

**安装时间**：通常 1-3 分钟，取决于网络速度。

---

### 第 5 步：验证功能

1. **内置浏览器工作**
   - 点击"启动 NCF"
   - 等待 NCF 应用启动
   - 浏览器标签页自动打开
   - 显示 NCF 网页内容

2. **地址栏可用**
   - 在地址栏输入新 URL
   - 按 Enter 或点击"→"按钮
   - 导航到新页面

3. **关闭标签确认**
   - 点击"✕ 关闭标签"按钮
   - 显示确认对话框
   - 选择"关闭"或"取消"

---

## 🔄 重复测试

想再次测试？只需：

1. 运行 `.\quick-clean-webview2.ps1`
2. 重新启动应用
3. 观察自动安装过程

---

## ⚠️ 故障排查

### 问题 1：清理脚本报错"权限不足"

**解决**：
```powershell
# 确保以管理员身份运行 PowerShell
# 右键点击 PowerShell 图标 → "以管理员身份运行"
```

### 问题 2：文件无法删除（被占用）

**这是正常现象，不影响测试！**

我们的应用通过注册表检测 WebView2，不检查文件。只要注册表清理成功，测试就可以进行。

**如果你想完全删除文件**：
```powershell
# 1. 关闭所有浏览器和应用（Edge、Chrome 等）
# 2. 打开任务管理器（Ctrl+Shift+Esc）
# 3. 结束所有 msedgewebview2* 进程
# 4. 或者重启计算机
# 5. 再次运行清理脚本
```

但通常这不是必需的！

### 问题 3：批处理文件显示乱码

**解决**：
```powershell
# 不使用 .bat 文件，直接运行 PowerShell 脚本
.\quick-clean-webview2.ps1
```

### 问题 4：自动安装失败

**检查**：
1. 网络连接是否正常
2. 防火墙是否拦截
3. 磁盘空间是否足够（至少 200MB）

**如果安装失败**，应用会显示友好的错误界面，提供：
- 🌍 在外部浏览器中打开
- ⬇️ 手动下载 WebView2 链接

---

## 📊 测试检查清单

**清理阶段**：
- [ ] 清理工具以管理员身份运行成功
- [ ] 注册表已清理（✅ Cleaned）
- [ ] 文件清理（✅ Cleaned 或 ⚠️ Still exist 都可以）

**自动安装阶段**：
- [ ] 应用启动时检测到 WebView2 未安装
- [ ] 自动下载 WebView2 Bootstrapper
- [ ] 静默安装 WebView2 Runtime
- [ ] 显示安装进度（百分比更新）
- [ ] 安装成功验证（或超时警告但仍继续）
- [ ] 内置浏览器正常显示网页内容

**功能验证阶段**：
- [ ] 地址栏可编辑和导航
- [ ] 按 Enter 或点击"→"按钮可导航
- [ ] 关闭标签时显示确认对话框
- [ ] 点击"关闭"后进程完全终止

---

## 💡 快速命令参考

```powershell
# 清理 WebView2
cd Y:\...\NcfDesktopApp.GUI\build-tool
.\quick-clean-webview2.ps1

# 启动应用
cd Y:\...\NcfDesktopApp.GUI\publish-self-contained\win-arm64-final
.\NcfDesktopApp.GUI.exe

# 手动验证
Test-Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
Test-Path "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application"
```

---

## 📚 相关文档

- [WebView2 自动化功能说明](../WEBVIEW2_AUTO_SETUP.md)
- [WebView2 清理指南](./WEBVIEW2_CLEANUP_GUIDE.md)
- [更新日志](../CHANGELOG_WEBVIEW.md)

---

**祝测试顺利！** 🎉

---

## ❓ 常见问题 (FAQ)

### Q1: 为什么文件删除失败但仍然显示 SUCCESS？

**A**: 因为我们的应用**只检查注册表**来判断 WebView2 是否安装，不检查文件。只要注册表清理成功，应用就会认为 WebView2 未安装，从而触发自动安装流程。

### Q2: 我需要完全删除 WebView2 文件吗？

**A**: **不需要**。注册表清理就足够了。文件删除只是为了更彻底的清理，但对测试自动安装功能来说不是必需的。

### Q3: 如果自动安装显示"验证超时"怎么办？

**A**: 这可能是因为注册表更新延迟。但安装程序已经成功运行（退出码为 0），所以：
- 应用会继续运行
- WebView 很可能已经可以使用
- 如果不行，重启应用即可

### Q4: 可以在非管理员模式下清理吗？

**A**: **不可以**。删除注册表的 `HKLM` 路径需要管理员权限。但测试应用本身不需要管理员权限。

### Q5: 清理后还能用 Edge 浏览器吗？

**A**: **可以**。WebView2 Runtime 和 Edge 浏览器是独立的，清理 WebView2 不影响 Edge 浏览器的使用。

---

**最后更新**：2025-11-14 (更新了清理流程和文件删除说明)

