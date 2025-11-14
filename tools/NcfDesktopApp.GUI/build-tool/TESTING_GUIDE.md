# WebView2 自动安装测试指南

## 🎯 测试目的

测试 NCF 桌面应用的 WebView2 自动检测和安装功能。

---

## 📁 工具文件

已创建以下工具文件供测试使用：

1. **`1-diagnose-webview2.bat`** - 诊断 WebView2 状态（有编码问题，建议使用 PowerShell）
2. **`2-clean-webview2.bat`** - 完全清理 WebView2（有编码问题，建议使用 PowerShell）
3. **`3-quick-clean-webview2.bat`** ⭐ - **快速清理工具（推荐用于测试）**
4. **`quick-clean-webview2.ps1`** - PowerShell 清理脚本

---

## 🚀 快速测试流程

### 方法 A：使用批处理文件（最简单）

1. **右键点击** `3-quick-clean-webview2.bat`
2. 选择 **"以管理员身份运行"**
3. 按任意键确认
4. 等待清理完成
5. 启动 NCF 应用测试

### 方法 B：使用 PowerShell（推荐）

```powershell
# 1. 以管理员身份打开 PowerShell

# 2. 进入工具目录
cd Y:\SenparcProjects\NeuCharFramework\NcfPackageSources\tools\NcfDesktopApp.GUI\build-tool

# 3. 运行清理脚本
.\quick-clean-webview2.ps1

# 4. 等待清理完成，应该显示：
# ✅ SUCCESS: WebView2 has been completely removed
```

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
WebView2 Quick Cleanup for Testing
========================================

Step 1: Deleting registry keys...
  - Deleted: HKLM:\SOFTWARE\WOW6432Node\...
  - Not found: HKLM:\SOFTWARE\Microsoft\...

Step 2: Deleting installation files...
  - Deleting: C:\Program Files (x86)\Microsoft\EdgeWebView\Application
  - Deleted successfully

========================================
Verification
========================================

✅ SUCCESS: WebView2 has been completely removed

You can now test the auto-installation feature!
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

**解决**：
```powershell
# 1. 关闭所有浏览器和应用
# 2. 打开任务管理器（Ctrl+Shift+Esc）
# 3. 结束所有 msedgewebview2* 进程
# 4. 再次运行清理脚本
```

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

- [ ] 清理工具运行成功
- [ ] 验证 WebView2 已完全移除
- [ ] 应用启动时检测到未安装
- [ ] 自动下载 Bootstrapper
- [ ] 静默安装 WebView2
- [ ] 显示安装进度
- [ ] 安装成功验证
- [ ] 内置浏览器正常显示
- [ ] 地址栏可编辑和导航
- [ ] 关闭标签显示确认对话框
- [ ] 进程完全终止

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

**最后更新**：2025-11-15

