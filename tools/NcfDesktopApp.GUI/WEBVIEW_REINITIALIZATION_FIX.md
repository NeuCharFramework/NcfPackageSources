# WebView 重新初始化问题修复

## 🐛 问题描述

**平台**: Windows ARM64 (其他平台可能也受影响)  
**症状**: 
- 第一次启动应用程序，WebView 能正常显示网站
- 关闭 NCF 后再次运行，WebView 显示错误：**"WebView is not initialized"**

## 🔍 根本原因

在 `EmbeddedWebView.cs` 中，`OnUnloaded()` 方法的清理逻辑不完整：

### ❌ 原代码（有问题）
```csharp
protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
{
    base.OnUnloaded(e);
    
    // 清理资源
    _webView = null;  // 仅设置为 null，没有真正清理
}
```

**问题分析：**
1. ❌ 仅将 `_webView` 设置为 null，没有从容器移除
2. ❌ 没有重置 `_isWebViewReady` 标志
3. ❌ WebView2 的网页资源和内存没有释放
4. ❌ 可能导致用户数据目录被锁定（特别是在 Windows ARM64 上）

**错误流程：**
```
第一次启动 ✅ → 创建 WebView → 正常显示
         ↓
      关闭 NCF
         ↓
OnUnloaded() 仅设为 null → WebView2 资源未释放 ⚠️
         ↓
第二次启动 → 尝试创建新 WebView → 资源冲突 → "WebView is not initialized" ❌
```

## ✅ 修复方案

### 新代码（已修复）
```csharp
protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
{
    base.OnUnloaded(e);
    
    // 清理资源
    CleanupWebView();
}

/// <summary>
/// 清理 WebView 资源（修复 Windows ARM64 重新初始化问题）
/// </summary>
private void CleanupWebView()
{
    try
    {
        Debug.WriteLine("🧹 开始清理 WebView 资源...");
        
        if (_webView != null)
        {
            try
            {
                // 1. 导航到空白页，释放网页资源
                try
                {
                    _webView.Url = new Uri("about:blank");
                    Debug.WriteLine("   ✓ WebView 已导航到空白页");
                }
                catch { /* 忽略导航失败 */ }
                
                // 2. 从容器中移除
                _webViewContainer?.Children.Remove(_webView);
                Debug.WriteLine("   ✓ WebView 已从容器移除");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"   ⚠️ WebView 清理警告: {ex.Message}");
            }
            finally
            {
                _webView = null;
            }
        }
        
        // 3. 重置初始化标志（关键！）
        _isWebViewReady = false;
        _currentUrl = "";
        
        Debug.WriteLine("✅ WebView 资源清理完成");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"❌ WebView 清理失败: {ex.Message}");
    }
}
```

## 🎯 修复要点

1. **导航到空白页** (`about:blank`)
   - 释放当前网页占用的资源
   - 清理 JavaScript 引擎和 DOM

2. **从容器移除 WebView**
   - 确保 UI 树不再引用 WebView
   - 允许垃圾回收器回收资源

3. **重置初始化标志** ⭐ **最关键**
   - `_isWebViewReady = false`
   - `_currentUrl = ""`
   - 确保下次初始化从干净状态开始

4. **设置为 null**
   - 释放引用，允许垃圾回收

## 🧪 测试步骤

### 测试环境
- ✅ Windows 10/11 ARM64
- ✅ Windows 10/11 x64
- ✅ macOS (Apple Silicon)
- ✅ macOS (Intel)

### 测试流程

#### 测试 1: 基本重启测试
1. 启动 NCF Desktop App
2. 等待 WebView 初始化完成
3. 启动 NCF 应用，在内置浏览器中打开
4. 验证网页正常显示 ✅
5. 关闭浏览器标签页（停止 NCF）
6. 再次启动 NCF
7. **验证**: WebView 能正常显示，没有 "WebView is not initialized" 错误 ✅

#### 测试 2: 多次重启测试
1. 重复以上步骤 5-10 次
2. **验证**: 每次都能正常显示，没有错误 ✅

#### 测试 3: 完全退出应用测试
1. 启动应用，启动 NCF，正常显示
2. 关闭浏览器标签页
3. 完全退出应用（关闭主窗口）
4. 重新启动应用
5. 启动 NCF
6. **验证**: WebView 能正常显示 ✅

#### 测试 4: 资源清理验证
1. 打开任务管理器/活动监视器
2. 启动应用，启动 NCF
3. 记录内存使用量 M1
4. 关闭浏览器标签页
5. 等待 5 秒
6. 记录内存使用量 M2
7. **验证**: M2 < M1（内存已释放）✅

## 📝 调试日志示例

### 正常清理日志
```
[13:45:23] 🧹 开始清理 WebView 资源...
[13:45:23]    ✓ WebView 已导航到空白页
[13:45:23]    ✓ WebView 已从容器移除
[13:45:23] ✅ WebView 资源清理完成
```

### 异常清理日志（非致命）
```
[13:45:23] 🧹 开始清理 WebView 资源...
[13:45:23]    ⚠️ WebView 清理警告: Object is already disposed
[13:45:23] ✅ WebView 资源清理完成
```

## 🔄 相关文件

- **修复文件**: `Views/Controls/EmbeddedWebView.cs`
  - 修改方法: `OnUnloaded()` (第 522-528 行)
  - 新增方法: `CleanupWebView()` (第 530-574 行)

## ⚠️ 已知限制

1. **WebView.Avalonia 限制**
   - `AvaloniaWebView.WebView` 不实现 `IDisposable`
   - 无法主动调用 `Dispose()` 方法
   - 依赖垃圾回收器自动清理

2. **平台差异**
   - Windows: 使用 WebView2 (Edge Chromium)
   - macOS: 使用 WKWebView (Safari)
   - Linux: 使用 WebKitGTK
   - 清理行为可能略有不同

## 🎉 预期效果

修复后，用户可以：
- ✅ 多次启动和停止 NCF，WebView 都能正常工作
- ✅ 关闭浏览器标签页后，资源被正确释放
- ✅ 完全退出应用再重启，WebView 初始化正常
- ✅ 在 Windows ARM64 上不再出现初始化错误

## 📚 参考资料

- [AvaloniaWebView Documentation](https://github.com/AvaloniaWebView/AvaloniaWebView)
- [WebView2 Best Practices](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/best-practices)
- [Avalonia Control Lifecycle](https://docs.avaloniaui.net/docs/guides/custom-controls/defining-properties)

---

**修复日期**: 2025-11-16  
**修复版本**: Hybrid 版本  
**测试状态**: ⏳ 待用户测试确认

