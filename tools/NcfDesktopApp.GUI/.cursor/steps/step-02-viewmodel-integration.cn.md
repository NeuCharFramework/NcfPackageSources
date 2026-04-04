# Step 02: 在 MainWindowViewModel 中集成 CLI 日志输出

## 📋 任务概述
在 `ViewModels/MainWindowViewModel.cs` 中集成 CLI 输出处理，将 NcfService 的输出回调连接到 UI 日志系统。

## 🎯 目标
- ✅ 注册 NcfService 的输出回调
- ✅ 实现线程安全的日志更新
- ✅ 区分 CLI 输出和应用日志
- ✅ 保持良好的性能和响应速度

## 📂 涉及文件
- `ViewModels/MainWindowViewModel.cs` - 主要修改文件

## 🔧 实现步骤

### 1. 添加 CLI 日志处理方法

在 `MainWindowViewModel` 类中，找到现有的 `AddLog` 方法（约 1054 行），在其附近添加新方法：

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
```

### 2. 修改现有 AddLog 方法（可选优化）

为了更清晰地区分应用日志和 CLI 输出，可以修改现有的 `AddLog` 方法添加前缀：

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
```

**注意**：如果不想影响现有日志显示，可以不添加 `[APP]` 前缀，保持原样。

### 3. 在 StartNcfAsync 方法中注册回调

找到 `StartNcfAsync` 方法（约 400-500 行之间），在调用 `_ncfService.StartNcfProcessAsync` **之前**注册回调：

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
```

### 4. 在 StopNcf 方法中清理回调（可选）

在停止进程时，可以清理回调（虽然不是必须的）：

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
```

### 5. 性能优化：批量更新（可选高级功能）

如果 CLI 输出非常频繁，可以实现批量更新机制：

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
```

**注意**：批量更新会增加复杂度，建议先实现简单版本，只有在性能确实有问题时再考虑。

## ✅ 验收标准

### 功能验收
- [ ] 启动 NCF 后，UI 日志中显示 CLI 输出
- [ ] CLI 正常输出显示为 `[CLI]` 前缀
- [ ] CLI 错误输出显示为 `[CLI:ERROR]` 前缀
- [ ] 应用日志和 CLI 输出混合显示，时间顺序正确
- [ ] 日志实时更新，延迟 < 1 秒

### 技术验收
- [ ] 使用 `Dispatcher.UIThread.Post` 确保线程安全
- [ ] 不阻塞 UI 线程
- [ ] 日志行数限制生效（1000 行）
- [ ] 异常处理完善

### 质量验收
- [ ] 代码风格与现有代码一致
- [ ] 性能良好，无明显卡顿
- [ ] 清理工作正确（停止时取消回调）

## 🔍 测试建议

1. **基本功能测试**
   - 启动 NCF，观察是否出现 CLI 日志
   - 验证 `[CLI]` 前缀显示正确
   - 验证应用日志（`[APP]` 或无前缀）和 CLI 日志混合显示

2. **性能测试**
   - 长时间运行 NCF，观察 UI 是否卡顿
   - 检查内存占用是否稳定
   - 验证日志行数限制生效

3. **错误处理测试**
   - 制造 NCF 启动错误，观察 stderr 捕获
   - 验证 `[CLI:ERROR]` 前缀显示

4. **并发测试**
   - 快速启动/停止多次，验证无异常
   - 检查回调是否正确清理

## 📝 注意事项

⚠️ **重要**：
- 必须使用 `Dispatcher.UIThread.Post`（异步）而非 `Invoke`（同步），避免死锁
- CLI 输出回调在后台线程执行，直接访问 UI 会崩溃
- 考虑性能：ASP.NET Core 启动时会有大量日志输出
- 日志行数限制很重要，避免内存无限增长

⚙️ **性能建议**：
- 简单场景：每条日志直接更新（当前方案）
- 高频输出：使用批量更新 + Timer（可选优化）
- 极端场景：考虑虚拟滚动或分页显示

## 🎨 UI 增强建议（可选，下一阶段）

1. **视觉区分**
   - 为 `[CLI]` 和 `[CLI:ERROR]` 添加不同颜色
   - 使用图标替代文本前缀

2. **过滤功能**
   - 添加下拉框：全部 / 应用日志 / CLI 输出
   - 实现日志搜索功能

3. **导出功能**
   - 添加"导出日志"按钮
   - 支持导出为 txt 文件

## 🔗 相关任务
- 上一步：[Step 01: 在 NcfService 中实现 CLI 输出捕获机制](./step-01-cli-capture.md)
- 下一步：[Step 03: 测试和优化性能](./step-03-testing-optimization.md)

