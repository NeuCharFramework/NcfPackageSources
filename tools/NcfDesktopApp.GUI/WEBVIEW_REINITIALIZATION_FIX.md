# WebView reinitialization problem fixed

## 🐛 Problem description

**Platform**: Windows ARM64 (other platforms may also be affected)
**symptom**:
- When starting the application for the first time, WebView can display the website normally
- After closing NCF and running it again, WebView displays an error: **"WebView is not initialized"**

## 🔍 Root cause

exist`EmbeddedWebView.cs`middle,`OnUnloaded()`The cleanup logic of the method is incomplete:

### ❌ Original code (with problems)
```csharp
protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
{
    base.OnUnloaded(e);
    
    // 清理资源
    _webView = null;  // 仅设置为 null，没有真正清理
}
```

**Problem Analysis:**
1. ❌ Only`_webView`Set to null, not removed from the container
2. ❌ No reset`_isWebViewReady`logo
3. ❌ The web page resources and memory of WebView2 are not released
4. ❌ May cause the user data directory to be locked (especially on Windows ARM64)

**Error process:**
```
第一次启动 ✅ → 创建 WebView → 正常显示
         ↓
      关闭 NCF
         ↓
OnUnloaded() 仅设为 null → WebView2 资源未释放 ⚠️
         ↓
第二次启动 → 尝试创建新 WebView → 资源冲突 → "WebView is not initialized" ❌
```

## ✅ Repair solution

### New code (fixed)
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

## 🎯 Repair points

1. **Navigate to Blank Page** (`about:blank`)
- Release the resources occupied by the current web page
- Clean JavaScript engine and DOM

2. **Remove WebView from container**
- Make sure the UI tree no longer references WebView
- Allow the garbage collector to reclaim resources

3. **Reset initialization flag** ⭐ **The most critical**
   - `_isWebViewReady = false`
   - `_currentUrl = ""`
- Ensure next initialization starts from a clean state

4. **Set to null**
- Release the reference, allowing garbage collection

## 🧪 Test steps

### Test environment
- ✅ Windows 10/11 ARM64
- ✅ Windows 10/11 x64
- ✅ macOS (Apple Silicon)
- ✅ macOS (Intel)

### Test process

#### Test 1: Basic restart test
1. Start NCF Desktop App
2. Wait for WebView initialization to complete
3. Start the NCF application and open it in the built-in browser
4. Verify that the web page displays normally ✅
5. Close the browser tab (stop NCF)
6. Start NCF again
7. **Verification**: WebView can be displayed normally without "WebView is not initialized" error ✅

#### Test 2: Restart the test multiple times
1. Repeat the above steps 5-10 times
2. **Verification**: It can be displayed normally every time without errors ✅

#### Test 3: Exit application testing completely
1. Start the application, start NCF, and display normally
2. Close the browser tab
3. Completely exit the application (close the main window)
4. Restart the application
5. Start NCF
6. **Verification**: WebView can display normally ✅

#### Test 4: Resource Cleanup Verification
1. Open Task Manager/Activity Monitor
2. Start the application and start NCF
3. Record memory usage M1
4. Close the browser tab
5. Wait 5 seconds
6. Record memory usage M2
7. **Verification**: M2 < M1 (memory has been released)✅

## 📝 Debug log example

### Clear logs normally
```
[13:45:23] 🧹 开始清理 WebView 资源...
[13:45:23]    ✓ WebView 已导航到空白页
[13:45:23]    ✓ WebView 已从容器移除
[13:45:23] ✅ WebView 资源清理完成
```

### Exception cleanup log (non-fatal)
```
[13:45:23] 🧹 开始清理 WebView 资源...
[13:45:23]    ⚠️ WebView 清理警告: Object is already disposed
[13:45:23] ✅ WebView 资源清理完成
```

## 🔄 Related documents

- **REPAIR FILE**:`Views/Controls/EmbeddedWebView.cs`
- Modification method:`OnUnloaded()`(Lines 522-528)
- New method:`CleanupWebView()`(Lines 530-574)

## ⚠️ Known limitations

1. **WebView.Avalonia Limitations**
   - `AvaloniaWebView.WebView`Not realized`IDisposable`
- Unable to actively call`Dispose()`method
- Rely on automatic cleaning by garbage collector

2. **Platform differences**
- Windows: using WebView2 (Edge Chromium)
- macOS: Using WKWebView (Safari)
- Linux: using WebKitGTK
- Cleanup behavior may be slightly different

## 🎉 Expected results

After the fix, users can:
- ✅ After starting and stopping NCF many times, WebView works normally
- ✅ Resources are released correctly after closing the browser tab
- ✅ Completely exit the app and then restart, the WebView will initialize normally.
- ✅ No more initialization errors on Windows ARM64

## 📚 References

- [AvaloniaWebView Documentation](https://github.com/AvaloniaWebView/AvaloniaWebView)
- [WebView2 Best Practices](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/best-practices)
- [Avalonia Control Lifecycle](https://docs.avaloniaui.net/docs/guides/custom-controls/defining-properties)

---

**Repair Date**: 2025-11-16
**Fixed version**: Hybrid version
**Test status**: ⏳ To be confirmed by user testing

