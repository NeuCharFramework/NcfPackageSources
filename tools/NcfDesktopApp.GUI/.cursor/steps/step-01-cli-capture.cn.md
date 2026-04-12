# Step 01: 在 NcfService 中实现 CLI 输出捕获机制

## 📋 任务概述
在 `Services/NcfService.cs` 中实现 CLI 进程输出的实时捕获，通过事件回调将输出传递给上层。

## 🎯 目标
- ✅ 捕获 Senparc.Web 进程的 stdout 和 stderr 输出
- ✅ 通过回调机制将输出传递给 ViewModel
- ✅ 优雅处理进程退出和异常情况
- ✅ 设置正确的控制台编码

## 📂 涉及文件
- `Services/NcfService.cs` - 主要修改文件

## 🔧 实现步骤

### 1. 定义输出回调委托

在 `NcfService.cs` 文件顶部添加委托定义：

```csharp
// 在 namespace 内，class 外部定义
public delegate void ProcessOutputHandler(string output, bool isError);
```

### 2. 添加输出回调属性

在 `NcfService` 类中添加回调属性：

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

### 3. 修改 StartNcfProcessAsync 方法

在 `StartNcfProcessAsync` 方法中，找到进程启动后的位置（约 350 行附近），添加输出捕获逻辑：

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

### 4. 处理回退启动场景

在回退启动（fallback）的代码块中也添加相同的输出捕获逻辑。有三个地方需要添加：

1. **第一次回退**（约 372-410 行）
2. **第二次回退**（约 418-450 行）

每个启动代码块后都需要添加相同的事件处理代码。

**代码复用建议**：提取为私有方法

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

然后在所有 `Process.Start()` 之后调用：

```csharp
var process = Process.Start(startInfo);
AttachProcessOutputHandlers(process);
```

### 5. 设置控制台编码（可选）

如果遇到中文乱码，在 `startInfo` 中添加：

```csharp
startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
```

## ✅ 验收标准

### 功能验收
- [ ] 进程启动后可以收到 stdout 输出
- [ ] 进程启动后可以收到 stderr 输出
- [ ] 回调正确传递输出内容和错误标识
- [ ] 进程退出时触发退出消息
- [ ] 三个启动分支（自包含/回退/二次回退）都能正确捕获

### 技术验收
- [ ] 使用异步事件机制，不阻塞主线程
- [ ] 异常处理完善，不会因输出异常导致崩溃
- [ ] 中文输出显示正常（无乱码）
- [ ] 代码复用良好，避免重复

### 质量验收
- [ ] 添加了必要的 try-catch 保护
- [ ] 日志记录关键操作
- [ ] 代码风格与现有代码一致

## 🔍 测试建议

1. **正常启动测试**：启动 NCF 应用，观察是否收到 ASP.NET Core 的启动日志
2. **错误输出测试**：制造一个启动错误（如端口占用），观察是否捕获错误信息
3. **进程退出测试**：停止 NCF 进程，验证是否收到退出消息
4. **中文输出测试**：在 NCF 中输出中文日志，验证显示正确

## 📝 注意事项

⚠️ **重要**：
- `OutputDataReceived` 和 `ErrorDataReceived` 事件在后台线程触发
- 不要在事件回调中直接访问 UI 元素
- 必须调用 `BeginOutputReadLine()` 和 `BeginErrorReadLine()` 才能触发事件
- 进程退出后事件会自动停止，无需手动取消订阅

## 🔗 相关任务
- 下一步：[Step 02: 在 MainWindowViewModel 中集成 CLI 日志输出](./step-02-viewmodel-integration.md)

