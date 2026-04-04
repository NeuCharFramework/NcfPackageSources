# 🔍 CLI output performance analysis report

**Date**: 2025-11-17
**Problem**: CLI log output causes the interface to freeze and the startup time becomes significantly longer.

---

## ⚠️ Performance problem diagnosis

### Problem phenomenon
- ✅ **User Report**: The interface is laggy
- ✅ **User Report**: Startup time is significantly longer
- ✅ **Cause**: There is a large amount of Console.Write content during the startup process

### Problem scenario analysis
Assume that the NCF startup process outputs **200 logs** (actually it may be more):

| operate | The cost of each log | Total cost of 200 logs | Influence |
|------|--------------|----------------|------|
| Thread switching | 3 times | **600 times** | 🔴 Serious |
| String splitting | 1 time (O(n)) | **200 times** | 🔴 Serious |
| Control lookup | 1 time (traverse the visual tree) | **200 times** | 🔴 Serious |
| UI redraw | 1 time | **200 times** | 🟡 Medium |
| Delay tasks | 1 time (10ms) | **200 times** (2 seconds total delay) | 🟡 Medium |

**Total**: **1200+ extra operations** will be generated during startup, resulting in severe lag!

---

## 🐛 Specific performance bottlenecks

### Bottleneck 1: Frequent UI thread switching 🔴 Serious

**Current Implementation** (`ViewModels/MainWindowViewModel.cs:1099-1126`):

```csharp
private void AddCliLog(string message, bool isError)
{
    // 第1次线程切换
    if (!Dispatcher.UIThread.CheckAccess())
    {
        Dispatcher.UIThread.Post(() => AddCliLog(message, isError));
        return;
    }
    
    var timestamp = DateTime.Now.ToString("HH:mm:ss");
    var prefix = isError ? "[CLI:ERROR]" : "[CLI]";
    var logEntry = $"[{timestamp}] {prefix} {message}";
    
    _logBuffer.AppendLine(logEntry);
    
    // 每次都 Split 字符串（O(n) 操作）
    var lines = _logBuffer.ToString().Split('\n');  // 🔴 性能杀手
    if (lines.Length > 1000)
    {
        _logBuffer.Clear();
        _logBuffer.AppendLine(string.Join('\n', lines.Skip(lines.Length - 1000)));
    }
    
    LogText = _logBuffer.ToString();  // 触发 UI 更新
    
    ScrollToBottomIfNeeded();  // 第2次 + 第3次线程切换
}
```

**question**:
- ❌ Each log = **3 thread switches**
- ❌ 200 logs = **600 thread switches**
- ❌ Thread switching overhead: ~0.1-1ms/time → **60-600ms total delay**

---

### Bottleneck 2: Frequent string operations 🔴 Serious

```csharp
// 每次都要遍历整个字符串
var lines = _logBuffer.ToString().Split('\n');  // O(n)

if (lines.Length > 1000)
{
    // 又要 Join 整个数组
    _logBuffer.AppendLine(string.Join('\n', lines.Skip(lines.Length - 1000)));  // O(n)
}
```

**question**:
- ❌ Every time the log is`Split('\n')`→ **O(n) operations**
- ❌ 200 logs, average 100 lines each → **10,000 character comparisons**
- ❌ String operation overhead: ~0.1ms/time → **1 second + total delay**

---

### Bottleneck 3: Frequent control search and scrolling 🔴 Serious

**Current Implementation** (`ViewModels/MainWindowViewModel.cs:1131-1167`):

```csharp
private void ScrollToBottomIfNeeded()
{
    try
    {
        Dispatcher.UIThread.Post(() =>  // 第2次线程切换
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow as MainWindow;
                if (mainWindow?.Content is UserControl mainContent)
                {
                    // 🔴 每次都遍历视觉树查找控件
                    var scrollViewer = mainContent.FindControl<ScrollViewer>("LogScrollViewer");
                    if (scrollViewer != null)
                    {
                        var settingsView = mainContent as Views.SettingsView;
                        if (settingsView?.ShouldAutoScroll ?? true)
                        {
                            // 第3次线程切换 + 延迟
                            Task.Delay(10).ContinueWith(_ =>
                            {
                                Dispatcher.UIThread.Post(() =>  // 第3次线程切换
                                {
                                    scrollViewer.ScrollToEnd();
                                });
                            });
                        }
                    }
                }
            }
        });
    }
    catch { }
}
```

**question**:
- ❌ Every time the log is`FindControl<ScrollViewer>`→ **Traverse the visual tree**
- ❌ 200 logs = **200 control searches**
- ❌ Control search overhead: ~1-5ms/time → **200-1000ms total delay**
- ❌ Extra`Task.Delay(10)`→ **2 seconds total delay**

---

### Bottleneck 4: Frequent UI redraw 🟡 Moderate

```csharp
LogText = _logBuffer.ToString();  // 每次都触发 UI 更新
```

**question**:
- ❌ Each log triggers data binding
- ❌ SelectableTextBlock needs to be re-layout and rendering
- ❌ 200 logs = **200 UI redraws**

---

## 💡 Optimization plan

### Option 1: Batch update (recommended) ⭐⭐⭐⭐⭐

**Core idea**: Don’t update the UI for every log, but collect a batch and then update it uniformly.

**accomplish**:
```csharp
private readonly Queue<string> _pendingCliLogs = new Queue<string>();
private readonly Timer _logUpdateTimer;
private int _currentLineCount = 0;  // 维护行数计数器
private const int MaxLogLines = 1000;
private const int LogUpdateIntervalMs = 100;  // 每100ms更新一次

public MainWindowViewModel()
{
    // 初始化定时器
    _logUpdateTimer = new Timer(LogUpdateIntervalMs);
    _logUpdateTimer.Elapsed += OnLogUpdateTimerElapsed;
    _logUpdateTimer.AutoReset = true;
    _logUpdateTimer.Start();
}

private void AddCliLog(string message, bool isError)
{
    lock (_pendingCliLogs)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var prefix = isError ? "[CLI:ERROR]" : "[CLI]";
        var logEntry = $"[{timestamp}] {prefix} {message}";
        _pendingCliLogs.Enqueue(logEntry);
    }
    // 不再每次都更新 UI，由定时器批量更新
}

private void OnLogUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
{
    List<string> logsToAdd;
    
    lock (_pendingCliLogs)
    {
        if (_pendingCliLogs.Count == 0) return;
        
        logsToAdd = new List<string>(_pendingCliLogs);
        _pendingCliLogs.Clear();
    }
    
    Dispatcher.UIThread.Post(() =>
    {
        foreach (var log in logsToAdd)
        {
            _logBuffer.AppendLine(log);
            _currentLineCount++;
        }
        
        // 限制日志行数（避免字符串分割）
        if (_currentLineCount > MaxLogLines)
        {
            var lines = _logBuffer.ToString().Split('\n');
            if (lines.Length > MaxLogLines)
            {
                _logBuffer.Clear();
                var keptLines = lines.Skip(lines.Length - MaxLogLines);
                foreach (var line in keptLines)
                {
                    _logBuffer.AppendLine(line);
                }
                _currentLineCount = MaxLogLines;
            }
        }
        
        LogText = _logBuffer.ToString();
        ScrollToBottomIfNeeded();  // 只在批量更新时滚动一次
    });
}
```

**Optimization effect**:
| index | Before | After optimization | improve |
|------|------|--------|------|
| Thread switching | 600 times | ~10 times | **98% ↓** |
| String splitting | 200 times | ~2 times | **99% ↓** |
| Control lookup | 200 times | ~10 times | **95% ↓** |
| UI redraw | 200 times | ~10 times | **95% ↓** |
| total delay | 2-5 seconds | **<100ms** | **95%+ ↓** |

---

### Option 2: Caching ScrollViewer References ⭐⭐⭐⭐

**Question**: Every time`FindControl<ScrollViewer>`Traverse the visual tree

**Solution**:
```csharp
private ScrollViewer? _cachedScrollViewer;

private void ScrollToBottomIfNeeded()
{
    Dispatcher.UIThread.Post(() =>
    {
        try
        {
            // 缓存 ScrollViewer 引用
            if (_cachedScrollViewer == null)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow as MainWindow;
                    if (mainWindow?.Content is UserControl mainContent)
                    {
                        _cachedScrollViewer = mainContent.FindControl<ScrollViewer>("LogScrollViewer");
                    }
                }
            }
            
            if (_cachedScrollViewer != null)
            {
                var settingsView = _cachedScrollViewer.Parent as Views.SettingsView;
                if (settingsView?.ShouldAutoScroll ?? true)
                {
                    _cachedScrollViewer.ScrollToEnd();  // 直接滚动，不需要 Task.Delay
                }
            }
        }
        catch { }
    });
}
```

**Optimization effect**:
- ✅ Control search from **O(n) → O(1)**
- ✅ Reduce 200 visual tree traversals
- ✅ Remove unnecessary ones`Task.Delay(10)`

---

### Option 3: Row Counter ⭐⭐⭐

**Question**: Every time`Split('\n')`Check the number of rows

**Solution**:
```csharp
private int _currentLineCount = 0;  // 维护行数计数器

private void AddLog(string message)
{
    _logBuffer.AppendLine(message);
    _currentLineCount++;
    
    // 只在超出限制时才分割字符串
    if (_currentLineCount > MaxLogLines)
    {
        var lines = _logBuffer.ToString().Split('\n');
        if (lines.Length > MaxLogLines)
        {
            _logBuffer.Clear();
            var keptLines = lines.Skip(lines.Length - MaxLogLines);
            foreach (var line in keptLines)
            {
                _logBuffer.AppendLine(line);
            }
            _currentLineCount = MaxLogLines;
        }
    }
    
    LogText = _logBuffer.ToString();
}
```

**Optimization effect**:
- ✅ Avoid frequent string splitting
- ✅ Only perform expensive operations when necessary

---

### Solution 4: Log level filtering ⭐⭐

**Idea**: Allow users to filter unimportant CLI output

```csharp
public enum CliLogLevel
{
    Debug,    // 调试信息（默认不显示）
    Info,     // 一般信息
    Warning,  // 警告
    Error     // 错误
}

public CliLogLevel MinLogLevel { get; set; } = CliLogLevel.Info;

private void AddCliLog(string message, bool isError, CliLogLevel level = CliLogLevel.Info)
{
    if (level < MinLogLevel) return;  // 过滤低级别日志
    
    // ... 后续处理
}
```

**Optimization effect**:
- ✅ Reduce unnecessary log volume
- ✅ Users can adjust the level of detail as needed

---

## 📊 Optimization effect comparison

### Start scene (200 logs)

| plan | Thread switching | String splitting | Control lookup | UI redraw | Estimated time | Recommendation |
|------|---------|-----------|---------|--------|---------|--------|
| **Current Implementation** | 600 times | 200 times | 200 times | 200 times | **2-5 seconds** | ❌ |
| **Option 1 (Batch)** | ~10 times | ~2 times | ~10 times | ~10 times | **<100ms** | ⭐⭐⭐⭐⭐ |
| Plan 1+2 | ~10 times | ~2 times | 1 time | ~10 times | **<50ms** | ⭐⭐⭐⭐⭐ |
| Plan 1+2+3 | ~10 times | 0 times | 1 time | ~10 times | **<30ms** | ⭐⭐⭐⭐⭐ |

---

## 🎯 Recommended implementation plan

### Phase 1 (immediate implementation) ⭐⭐⭐⭐⭐
1. **Option 1: Batch update mechanism**
- Use Timer to batch update logs every 100ms
- Reduce performance overhead by 95%+
- **Expected improvement: 10-20 times faster startup**

2. **Option 2: Caching ScrollViewer**
- Avoid frequent control searches
- Remove unnecessary`Task.Delay(10)`
- **Expected improvement: Additional 30-50%**

### Second stage (optional optimization)
3. **Option 3: Row Counter**
- Avoid frequent string splitting
- **Expected improvement: Additional 10-20%**

4. **Option 4: Log level filtering**
- Let users choose log detail level
- **Expected improvement: 20-80% depending on filtering level**

---

## 💻 Implementation code example

### Complete optimization implementation

```csharp
// MainWindowViewModel.cs

private readonly Queue<string> _pendingCliLogs = new Queue<string>();
private readonly System.Timers.Timer _logUpdateTimer;
private int _currentLineCount = 0;
private ScrollViewer? _cachedScrollViewer;
private const int MaxLogLines = 1000;
private const int LogUpdateIntervalMs = 100;

public MainWindowViewModel(/* ... */)
{
    // ... 其他初始化

    // 初始化日志更新定时器
    _logUpdateTimer = new System.Timers.Timer(LogUpdateIntervalMs);
    _logUpdateTimer.Elapsed += OnLogUpdateTimerElapsed;
    _logUpdateTimer.AutoReset = true;
    _logUpdateTimer.Start();
}

/// <summary>
/// 添加 CLI 日志（高性能版本，批量处理）
/// </summary>
private void AddCliLog(string message, bool isError)
{
    if (string.IsNullOrWhiteSpace(message)) return;
    
    var timestamp = DateTime.Now.ToString("HH:mm:ss");
    var prefix = isError ? "[CLI:ERROR]" : "[CLI]";
    var logEntry = $"[{timestamp}] {prefix} {message}";
    
    lock (_pendingCliLogs)
    {
        _pendingCliLogs.Enqueue(logEntry);
    }
}

/// <summary>
/// 定时器回调：批量更新日志（每100ms一次）
/// </summary>
private void OnLogUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
{
    List<string> logsToAdd;
    
    lock (_pendingCliLogs)
    {
        if (_pendingCliLogs.Count == 0) return;
        
        logsToAdd = new List<string>(_pendingCliLogs);
        _pendingCliLogs.Clear();
    }
    
    Dispatcher.UIThread.Post(() =>
    {
        // 批量添加日志
        foreach (var log in logsToAdd)
        {
            _logBuffer.AppendLine(log);
            _currentLineCount++;
        }
        
        // 限制日志行数（只在必要时执行）
        if (_currentLineCount > MaxLogLines + 100)  // 留一些缓冲
        {
            var lines = _logBuffer.ToString().Split('\n');
            if (lines.Length > MaxLogLines)
            {
                _logBuffer.Clear();
                var keptLines = lines.Skip(lines.Length - MaxLogLines);
                foreach (var line in keptLines)
                {
                    _logBuffer.AppendLine(line);
                }
                _currentLineCount = MaxLogLines;
            }
        }
        
        LogText = _logBuffer.ToString();
        ScrollToBottomIfNeeded();
    });
}

/// <summary>
/// 滚动到底部（优化版本，缓存控件引用）
/// </summary>
private void ScrollToBottomIfNeeded()
{
    Dispatcher.UIThread.Post(() =>
    {
        try
        {
            // 缓存 ScrollViewer 引用
            if (_cachedScrollViewer == null)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow as MainWindow;
                    if (mainWindow?.Content is UserControl mainContent)
                    {
                        _cachedScrollViewer = mainContent.FindControl<ScrollViewer>("LogScrollViewer");
                    }
                }
            }
            
            if (_cachedScrollViewer != null)
            {
                var settingsView = _cachedScrollViewer.Parent as Views.SettingsView;
                if (settingsView?.ShouldAutoScroll ?? true)
                {
                    _cachedScrollViewer.ScrollToEnd();
                }
            }
        }
        catch { }
    });
}

// 记得在 Dispose 时停止定时器
public void Dispose()
{
    _logUpdateTimer?.Stop();
    _logUpdateTimer?.Dispose();
    // ... 其他清理
}
```

---

## ✅ Verification method

### Performance testing steps
1. Start the application
2. Observe the smoothness of the startup process
3. Check the log output delay (should be < 200ms)
4. Monitor memory usage (should be stable)

### Expected results
- ✅ The startup speed has returned to normal (comparable to before the CLI output function was added)
- ✅ The log output is smooth and there is no obvious lag.
- ✅ UI responsive
- ✅ The log content is complete and there is no loss.

---

## 📝 Summary

### Source of the problem
The current implementation updates the UI immediately for each log, resulting in:
- 🔴 **600 thread switches**
- 🔴 **200 string splits**
- 🔴 **200 control searches**
- 🔴 **200 UI redraws**

### Optimize core
**Batch processing + cache optimization + reducing unnecessary operations**
- ✅ 100ms batch update → reduce 95% operations
- ✅ Cache control references → avoid repeated searches
- ✅ Line counter → avoid frequent string splitting

### Expected improvement
- 🚀 **Boost speed increased by 10-20 times**
- 🚀 **UI response speed increased by 95%+**
- 🚀 **Almost no perceptible performance impact**

---

**Document Creation**: 2025-11-17
**Related documentation**: CLI_OUTPUT_IMPLEMENTATION_SUMMARY.md

