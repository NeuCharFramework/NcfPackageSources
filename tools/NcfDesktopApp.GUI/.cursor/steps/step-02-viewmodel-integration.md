[中文版](step-02-viewmodel-integration.cn.md)

# Step 02: Integrate CLI log output in MainWindowViewModel

## 📋 Mission Overview
Integrate CLI output processing in `ViewModels/MainWindowViewModel.cs` and connect the output callback of NcfService to the UI logging system.

## 🎯 Goal
- ✅ Register the output callback of NcfService
- ✅ Implement thread-safe log updates
- ✅ Differentiate between CLI output and application logs
- ✅ Maintain good performance and responsiveness

## 📂Involved documents
- `ViewModels/MainWindowViewModel.cs` - main modified file

## 🔧 Implementation steps

### 1. Add CLI log processing method

In the `MainWindowViewModel` class, find the existing `AddLog` method (around line 1054) and add the new method near it:
```csharp
/// <summary>
/// 添加 CLI 进程输出到日志
/// </summary>
private void AddCliLog(string message, bool isError)
{
    // 必须在 UI 线程上更新
    if (!Dispatcher.UIThread.CheckAccess())
    {
        Dispatcher.UIThread.Post(() => AddCliLog(message, isError));
        return;
    }
    
    var timestamp = DateTime.Now.ToString("HH:mm:ss");
    var prefix = isError ? "[CLI:ERROR]" : "[CLI]";
    var logEntry = $"[{timestamp}] {prefix} {message}";
    
    _logBuffer.AppendLine(logEntry);
    
    // 限制日志大小，保留最后1000行
    var lines = _logBuffer.ToString().Split('\n');
    if (lines.Length > 1000)
    {
        _logBuffer.Clear();
        _logBuffer.AppendLine(string.Join('\n', lines.Skip(lines.Length - 1000)));
    }
    
    LogText = _logBuffer.ToString();
}
```### 2. Modify the existing AddLog method (optional optimization)

To more clearly distinguish between application logs and CLI output, you can modify the existing `AddLog` method to add a prefix:
```csharp
private void AddLog(string message)
{
    var timestamp = DateTime.Now.ToString("HH:mm:ss");
    // 添加 [APP] 前缀以区分应用日志（可选）
    var logEntry = $"[{timestamp}] [APP] {message}";
    
    _logBuffer.AppendLine(logEntry);
    
    // 限制日志大小，保留最后1000行
    var lines = _logBuffer.ToString().Split('\n');
    if (lines.Length > 1000)
    {
        _logBuffer.Clear();
        _logBuffer.AppendLine(string.Join('\n', lines.Skip(lines.Length - 1000)));
    }
    
    LogText = _logBuffer.ToString();
}
```**Note**: If you don’t want to affect the existing log display, you can leave it as is without adding the `[APP]` prefix.

### 3. Register callback in StartNcfAsync method

Find the `StartNcfAsync` method (between about 400-500 lines) and register the callback before calling `_ncfService.StartNcfProcessAsync`:
```csharp
[RelayCommand]
private async Task StartNcfAsync()
{
    if (IsNcfStarting || IsNcfRunning) return;

    IsNcfStarting = true;
    AddLog("🚀 正在启动 NCF 站点...");
    
    try
    {
        // 注册 CLI 输出回调（在启动进程之前）
        _ncfService.OnProcessOutput = (output, isError) =>
        {
            AddCliLog(output, isError);
        };
        
        // 启动进程
        _currentNcfProcess = await _ncfService.StartNcfProcessAsync(
            NcfPort, 
            _cancellationTokenSource.Token
        );
        
        // ... 后续代码保持不变 ...
    }
    catch (Exception ex)
    {
        AddLog($"❌ 启动失败: {ex.Message}");
        IsNcfStarting = false;
    }
}
```### 4. Clean up callbacks in StopNcf method (optional)

When stopping the process, the callback can be cleaned up (although not required):
```csharp
[RelayCommand]
private void StopNcf()
{
    AddLog("🛑 正在停止 NCF 站点...");
    
    try
    {
        // 清理回调
        if (_ncfService != null)
        {
            _ncfService.OnProcessOutput = null;
        }
        
        // ... 现有的停止逻辑 ...
    }
    catch (Exception ex)
    {
        AddLog($"❌ 停止失败: {ex.Message}");
    }
}
```### 5. Performance optimization: batch update (optional advanced function)

If the CLI output is very frequent, a batch update mechanism can be implemented:
```csharp
private readonly Queue<(string message, bool isError)> _cliLogQueue = new();
private readonly Timer _logFlushTimer;
private readonly object _logLock = new();

public MainWindowViewModel()
{
    // ... 现有构造函数代码 ...
    
    // 初始化日志刷新定时器（每 200ms 刷新一次）
    _logFlushTimer = new Timer(FlushCliLogs, null, 200, 200);
}

private void AddCliLog(string message, bool isError)
{
    lock (_logLock)
    {
        _cliLogQueue.Enqueue((message, isError));
    }
}

private void FlushCliLogs(object? state)
{
    List<(string, bool)> logsToFlush;
    
    lock (_logLock)
    {
        if (_cliLogQueue.Count == 0) return;
        logsToFlush = new List<(string, bool)>(_cliLogQueue);
        _cliLogQueue.Clear();
    }
    
    Dispatcher.UIThread.Post(() =>
    {
        foreach (var (message, isError) in logsToFlush)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var prefix = isError ? "[CLI:ERROR]" : "[CLI]";
            var logEntry = $"[{timestamp}] {prefix} {message}";
            _logBuffer.AppendLine(logEntry);
        }
        
        // 限制日志大小
        var lines = _logBuffer.ToString().Split('\n');
        if (lines.Length > 1000)
        {
            _logBuffer.Clear();
            _logBuffer.AppendLine(string.Join('\n', lines.Skip(lines.Length - 1000)));
        }
        
        LogText = _logBuffer.ToString();
    });
}
```**Note**: Batch updates will increase complexity. It is recommended to implement a simple version first and only consider it if there are real performance problems.

## ✅ Acceptance Criteria

### Function acceptance
- [ ] CLI output shown in UI log after starting NCF
- [ ] CLI normal output is shown with `[CLI]` prefix
- [ ] CLI error output is shown with the `[CLI:ERROR]` prefix
- [ ] Application logs and CLI output are mixed and displayed in correct chronological order
- [ ] Log updated in real time, delay < 1 second

### Technical acceptance
- [ ] Use `Dispatcher.UIThread.Post` to ensure thread safety
- [ ] Do not block the UI thread
- [ ] The limit on the number of log lines takes effect (1000 lines)
- [ ] Improved exception handling

### Quality acceptance
- [ ] Code style is consistent with existing code
- [ ] Good performance, no obvious lag
- [ ] cleanup works correctly (cancel callback on stop)

## 🔍 Testing suggestions

1. **Basic Function Test**
   - Start NCF and observe whether the CLI log appears
   - Verify that the `[CLI]` prefix is displayed correctly
   - Verify that application logs (`[APP]` or no prefix) and CLI logs are mixed

2. **Performance Test**
   - Run NCF for a long time and observe whether the UI is stuck.
   - Check whether memory usage is stable
   - Verify that the limit on the number of log lines takes effect

3. **Error handling test**
   - Make NCF startup error, observe stderr capture
   - Verify that `[CLI:ERROR]` prefix is displayed

4. **Concurrency Test**
   - Quickly start/stop multiple times and verify that there are no abnormalities
   - Check if callbacks are cleaned up correctly

## 📝 Notes

⚠️ **Important**:
- Must use `Dispatcher.UIThread.Post` (asynchronous) instead of `Invoke` (synchronous) to avoid deadlock
- CLI output callback is executed in the background thread, and direct access to the UI will crash
- Consider performance: ASP.NET Core will have a lot of log output when it starts
- The limit on the number of log lines is important to avoid unlimited memory growth

⚙️ **Performance Suggestions**:
- Simple scenario: each log is updated directly (current solution)
- High frequency output: use batch update + Timer (optional optimization)
- Extreme scenarios: consider virtual scrolling or paging display

## 🎨 UI enhancement suggestions (optional, next phase)

1. **Visual distinction**
   - Add different colors for `[CLI]` and `[CLI:ERROR]`
   - Use icons to replace text prefixes

2. **Filter function**
   - Added drop-down boxes: All/Application Log/CLI Output
   - Implement log search function

3. **Export function**
   - Added "Export Log" button
   - Support export to txt file

## 🔗 Related tasks
- Previous step: [Step 01: Implement CLI output capture mechanism in NcfService](./step-01-cli-capture.md)
- Next step: [Step 03: Testing and optimizing performance](./step-03-testing-optimization.md)
