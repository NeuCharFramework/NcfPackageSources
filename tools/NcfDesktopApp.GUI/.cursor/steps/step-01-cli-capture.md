# Step 01: Implement CLI output capture mechanism in NcfService

## 📋 Mission Overview
exist`Services/NcfService.cs`Real-time capture of CLI process output is implemented, and the output is passed to the upper layer through event callbacks.

## 🎯 Goal
- ✅ Capture stdout and stderr output of Senparc.Web process
- ✅ Pass output to ViewModel through callback mechanism
- ✅ Handle process exits and exceptions gracefully
- ✅ Set correct console encoding

## 📂Involved documents
- `Services/NcfService.cs`- Mainly modified files

## 🔧 Implementation steps

### 1. Define the output callback delegate

exist`NcfService.cs`Add the delegate definition at the top of the file:

```csharp
// 在 namespace 内，class 外部定义
public delegate void ProcessOutputHandler(string output, bool isError);
```

### 2. Add output callback attribute

exist`NcfService`Add callback attributes to the class:

```csharp
public class NcfService
{
    // ... 现有字段 ...
    
    /// <summary>
    /// CLI 进程输出回调（参数：输出内容, 是否为错误输出）
    /// </summary>
    public ProcessOutputHandler? OnProcessOutput { get; set; }
}
```

### 3. Modify StartNcfProcessAsync method

exist`StartNcfProcessAsync`In the method, find the position after the process is started (around line 350) and add output capture logic:

```csharp
public async Task<Process> StartNcfProcessAsync(int port, CancellationToken cancellationToken = default)
{
    // ... 现有的进程启动代码 ...
    
    // 已设置 RedirectStandardOutput = true 和 RedirectStandardError = true
    
    var process = Process.Start(startInfo);
    
    if (process != null)
    {
        // 设置输出编码（避免中文乱码）
        try
        {
            process.StandardOutput.BaseStream.ReadTimeout = 1000;
            process.StandardError.BaseStream.ReadTimeout = 1000;
        }
        catch { /* 某些进程可能不支持超时设置 */ }
        
        // 捕获标准输出
        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                try
                {
                    OnProcessOutput?.Invoke(args.Data, false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"处理进程输出时出错: {ex.Message}");
                }
            }
        };
        
        // 捕获错误输出
        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                try
                {
                    OnProcessOutput?.Invoke(args.Data, true);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"处理进程错误输出时出错: {ex.Message}");
                }
            }
        };
        
        // 开始异步读取
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        // 注册进程退出事件（可选，用于清理）
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) =>
        {
            try
            {
                OnProcessOutput?.Invoke("--- 进程已退出 ---", false);
            }
            catch { }
        };
    }
    
    // ... 后续的回退逻辑也需要同样处理 ...
    
    return process;
}
```

### 4. Handle rollback startup scenario

Also add the same output capture logic in the fallback startup (fallback) code block. There are three places that need to be added:

1. **First rollback** (about lines 372-410)
2. **Second rollback** (about lines 418-450)

The same event handling code needs to be added after each startup code block.

**Code Reuse Recommendation**: Extract to private method

```csharp
/// <summary>
/// 为进程附加输出捕获
/// </summary>
private void AttachProcessOutputHandlers(Process process)
{
    if (process == null) return;
    
    try
    {
        process.StandardOutput.BaseStream.ReadTimeout = 1000;
        process.StandardError.BaseStream.ReadTimeout = 1000;
    }
    catch { }
    
    process.OutputDataReceived += (sender, args) =>
    {
        if (!string.IsNullOrEmpty(args.Data))
        {
            try
            {
                OnProcessOutput?.Invoke(args.Data, false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"处理进程输出时出错: {ex.Message}");
            }
        }
    };
    
    process.ErrorDataReceived += (sender, args) =>
    {
        if (!string.IsNullOrEmpty(args.Data))
        {
            try
            {
                OnProcessOutput?.Invoke(args.Data, true);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"处理进程错误输出时出错: {ex.Message}");
            }
        }
    };
    
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    
    process.EnableRaisingEvents = true;
    process.Exited += (sender, args) =>
    {
        try
        {
            OnProcessOutput?.Invoke("--- 进程已退出 ---", false);
        }
        catch { }
    };
}
```

then in all`Process.Start()`Then call:

```csharp
var process = Process.Start(startInfo);
AttachProcessOutputHandlers(process);
```

### 5. Set console encoding (optional)

If you encounter Chinese garbled characters, go to`startInfo`Add in:

```csharp
startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
```

## ✅ Acceptance Criteria

### Function acceptance
- [ ] You can receive stdout output after the process is started.
- [ ] stderr output can be received after the process is started
- [ ] callback correctly passes output content and error identifier
- [ ] Trigger exit message when process exits
- [ ] All three startup branches (self-contained/rollback/secondary rollback) can be captured correctly

### Technical acceptance
- [ ] Use asynchronous event mechanism without blocking the main thread
- [ ] Exception handling is perfect and will not cause crashes due to output exceptions.
- [ ] Chinese output is displayed normally (no garbled characters)
- [ ] Good code reuse to avoid duplication

### Quality acceptance
- [ ] Added necessary try-catch protection
- [ ] Logging key operations
- [ ] Code style is consistent with existing code

## 🔍 Testing suggestions

1. **Normal startup test**: Start the NCF application and observe whether the startup log of ASP.NET Core is received
2. **Error output test**: Create a startup error (such as port occupation) and observe whether the error information is captured
3. **Process Exit Test**: Stop the NCF process and verify whether the exit message is received
4. **Chinese output test**: Output the Chinese log in NCF and verify that the display is correct

## 📝 Notes

⚠️ **Important**:
- `OutputDataReceived`and`ErrorDataReceived`The event is triggered on a background thread
- Do not access UI elements directly in event callbacks
- must be called`BeginOutputReadLine()`and`BeginErrorReadLine()`to trigger the event
- The event will automatically stop after the process exits, no need to manually unsubscribe

## 🔗 Related tasks
- Next step: [Step 02: Integrate CLI log output in MainWindowViewModel](./step-02-viewmodel-integration.md)

