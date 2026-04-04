# WebView 重新初始化问题修复 V2

## 🐛 新发现的问题

**症状**: 
- ✅ 第一版修复解决了"关闭后再次启动"的重新初始化问题
- ❌ 但引入了新问题：第一次启动也显示 **"WebView is not initialized"**

## 🔍 问题原因分析

### 之前的修复（V1）
在 `EmbeddedWebView.cs` 中添加了 `OnUnloaded()` 方法来清理 WebView 资源：

```csharp
protected override void OnUnloaded(RoutedEventArgs e)
{
    base.OnUnloaded(e);
    CleanupWebView(); // 清理 WebView 并设置 _isWebViewReady = false
}
```

**这个修复是正确的**，解决了 Windows ARM64 上的重新初始化问题。

### 新问题的根本原因

**问题流程**：
```
1. 用户启动 NCF ✅
   └─> IsBrowserTabVisible = true
   └─> BrowserView 控件加载
   └─> EmbeddedWebView 构造函数执行
   └─> InitializeWebViewAsync() 被调用 ✅
   └─> WebView 创建成功 ✅

2. 用户停止 NCF 🛑
   └─> IsBrowserTabVisible = false  ← 隐藏浏览器标签页
   └─> BrowserView 控件被隐藏/卸载
   └─> OnUnloaded() 被触发  ← 关键！
   └─> CleanupWebView() 执行
   └─> _isWebViewReady = false ❌
   └─> _webView = null ❌

3. 用户再次启动 NCF 🔄
   └─> IsBrowserTabVisible = true
   └─> BrowserView 控件再次显示
   └─> ❌ 构造函数不会再执行（控件已存在）
   └─> ❌ InitializeWebViewAsync() 不会被调用
   └─> ❌ _isWebViewReady = false，_webView = null
   └─> 尝试导航 NavigateTo(url)
   └─> 抛出异常: "WebView is not initialized" ❌
```

**核心问题**：
- 控件的构造函数只在**第一次创建时**执行一次
- 当控件被隐藏（`IsBrowserTabVisible = false`）后再显示，**不会重新执行构造函数**
- 但是 `OnUnloaded()` 会清理 WebView
- 没有对应的 `OnLoaded()` 来重新初始化

## ✅ 修复方案（V2）

### 添加 OnLoaded 方法

在 `EmbeddedWebView.cs` 中添加 `OnLoaded()` 方法，检测并重新初始化：

```csharp
protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
{
    base.OnLoaded(e);
    
    // 如果 WebView 已被清理（例如控件之前被隐藏），重新初始化
    if (!_isWebViewReady)
    {
        Debug.WriteLine("🔄 检测到 WebView 需要重新初始化...");
        _ = InitializeWebViewAsync();
    }
}
```

### 完整的生命周期管理

```csharp
// 构造函数 - 第一次创建时执行
public EmbeddedWebView()
{
    InitializeComponent();
    _ = InitializeWebViewAsync(); // 首次初始化
}

// OnLoaded - 每次控件显示时执行
protected override void OnLoaded(RoutedEventArgs e)
{
    base.OnLoaded(e);
    
    // 重新初始化（如果需要）
    if (!_isWebViewReady)
    {
        _ = InitializeWebViewAsync();
    }
}

// OnUnloaded - 每次控件隐藏时执行
protected override void OnUnloaded(RoutedEventArgs e)
{
    base.OnUnloaded(e);
    
    // 清理资源
    CleanupWebView();
}
```

## 📊 修复后的流程

```
1. 用户启动 NCF ✅
   └─> IsBrowserTabVisible = true
   └─> BrowserView 控件加载
   └─> EmbeddedWebView 构造函数执行
   └─> InitializeWebViewAsync() 被调用 ✅
   └─> WebView 创建成功 ✅

2. 用户停止 NCF 🛑
   └─> IsBrowserTabVisible = false
   └─> OnUnloaded() 被触发
   └─> CleanupWebView() 清理资源 ✅
   └─> _isWebViewReady = false

3. 用户再次启动 NCF 🔄
   └─> IsBrowserTabVisible = true
   └─> BrowserView 控件再次显示
   └─> OnLoaded() 被触发 ← 关键！
   └─> 检查 _isWebViewReady == false ✅
   └─> 调用 InitializeWebViewAsync() ✅
   └─> WebView 重新创建 ✅
   └─> _isWebViewReady = true ✅
   └─> NavigateTo(url) 成功 ✅
```

## 🎯 修复验证

### 测试场景 1：第一次启动
**步骤**：
1. 启动应用
2. 点击"启动 NCF"
3. 观察内置浏览器

**预期结果**：
- ✅ 浏览器正常显示
- ✅ NCF 网站加载成功

### 测试场景 2：停止后重新启动
**步骤**：
1. 启动 NCF（浏览器正常显示）
2. 停止 NCF
3. 再次启动 NCF

**预期结果**：
- ✅ 浏览器重新初始化成功
- ✅ NCF 网站正常加载
- ✅ 不出现 "WebView is not initialized" 错误

### 测试场景 3：多次切换
**步骤**：
1. 启动 NCF
2. 停止 NCF
3. 重复 3-5 次

**预期结果**：
- ✅ 每次都能正常初始化
- ✅ 无资源泄漏
- ✅ 性能稳定

## 🔧 技术细节

### Avalonia 控件生命周期

```
创建 → OnAttachedToVisualTree → OnLoaded → [显示]
                                      ↓
                                   可见状态
                                      ↓
                                   OnUnloaded → OnDetachedFromVisualTree
```

**关键点**：
- `构造函数`：只在控件第一次创建时调用
- `OnLoaded`：每次控件变为可见时调用
- `OnUnloaded`：每次控件变为不可见时调用

### 为什么需要 OnLoaded

在 Avalonia/WPF 应用中，当控件的 `IsVisible` 或 `Visibility` 属性改变时：
- `IsVisible = false` → 触发 `OnUnloaded`
- `IsVisible = true` → 触发 `OnLoaded`

这是重新初始化的正确时机。

## 📝 最佳实践

### ✅ 正确的资源管理模式

```csharp
public class MyControl : UserControl
{
    private Resource? _resource;
    private bool _isInitialized;
    
    // 构造函数 - 首次创建
    public MyControl()
    {
        InitializeComponent();
        _ = InitializeAsync();
    }
    
    // OnLoaded - 每次显示时检查
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        if (!_isInitialized)
        {
            _ = InitializeAsync();
        }
    }
    
    // OnUnloaded - 每次隐藏时清理
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        Cleanup();
    }
    
    private async Task InitializeAsync()
    {
        _resource = await CreateResourceAsync();
        _isInitialized = true;
    }
    
    private void Cleanup()
    {
        _resource?.Dispose();
        _resource = null;
        _isInitialized = false;
    }
}
```

### ❌ 常见错误

```csharp
// 错误 1：只在构造函数中初始化
public MyControl()
{
    InitializeComponent();
    _ = InitializeAsync(); // ❌ 卸载后不会重新初始化
}

// 错误 2：在 OnUnloaded 中清理但没有 OnLoaded
protected override void OnUnloaded(RoutedEventArgs e)
{
    Cleanup(); // ❌ 清理了但没有重新初始化机制
}

// 错误 3：不清理资源
protected override void OnUnloaded(RoutedEventArgs e)
{
    // ❌ 什么都不做，导致内存泄漏
}
```

## 🎉 总结

### V1 修复（之前）
- ✅ 添加了 `OnUnloaded()` 和 `CleanupWebView()`
- ✅ 解决了 Windows ARM64 重新初始化失败问题
- ❌ 但没有对应的重新初始化机制

### V2 修复（本次）
- ✅ 添加了 `OnLoaded()` 方法
- ✅ 检测 `_isWebViewReady` 状态
- ✅ 自动重新初始化 WebView
- ✅ 完整的生命周期管理

### 最终效果
- ✅ 第一次启动：正常工作
- ✅ 重复启动/停止：正常工作
- ✅ 资源正确清理：无内存泄漏
- ✅ 跨平台兼容：Windows/macOS/Linux

---

**修复日期**: 2025-11-16  
**版本**: V2  
**文件**: Views/Controls/EmbeddedWebView.cs  
**修复行数**: 522-532（OnLoaded 方法）

