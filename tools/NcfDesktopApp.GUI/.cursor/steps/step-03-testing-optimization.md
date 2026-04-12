[中文版](step-03-testing-optimization.cn.md)

# Step 03: Test and optimize performance

## 📋 Mission Overview
Comprehensive testing of CLI output synchronization functionality to identify and resolve performance issues to ensure stable and reliable functionality.

## 🎯 Goal
- ✅ Verify the correctness of basic functions
- ✅ Test extreme scenarios (large output, abnormal exit, etc.)
- ✅ Identify and optimize performance bottlenecks
- ✅ Make sure the Chinese display is correct
- ✅ Verified cross-platform compatibility

## 📂Involved documents
- All related files (Services/NcfService.cs, ViewModels/MainWindowViewModel.cs)

## 🧪 Test Checklist

### 1. Basic functional testing

#### Test 1.1: Normal startup and log capture
**Steps**:
1. Start NcfDesktopApp
2. Click the "Start" button
3. Observe the log panel

**Expected results**:
- ✅ App logs starting with `[APP]` appear
- ✅ NCF startup log starting with `[CLI]` appears
- ✅ The log is updated in real time without obvious delay (< 1s)
- ✅ Logs are in the correct order (sorted by time)

#### Test 1.2: Error output capture
**Steps**:
1. Modify the NCF port to an occupied port (such as 80)
2. Start NCF
3. Observation log

**Expected results**:
- ✅ Error messages starting with `[CLI:ERROR]` appear
- ✅ Error message is displayed in full
- ✅ App does not crash

#### Test 1.3: Process exit handling
**Steps**:
1. Start NCF
2. Click the "Stop" button
3. Observation log

**Expected results**:
- ✅ Process exit message appears
- ✅ App status updated correctly
- ✅ No abnormalities or errors

---

### 2. Performance test

#### Test 2.1: Large amount of log output
**Steps**:
1. Start NCF (ASP.NET Core will output a lot of logs when it starts)
2. Observe UI response speed
3. Check CPU and memory usage

**Expected results**:
- ✅ UI is not stuck, buttons are clickable
- ✅ Log scrolling is smooth
- ✅ Reasonable CPU usage (< 20%)
- ✅ Memory stable (no continuous growth)

**Performance Benchmark**:
- 100 logs/second log output → UI responds normally
- 500 logs/second log output → may require batch update optimization

#### Test 2.2: Log line limit
**Steps**:
1. Run NCF for a long time (simulating a large number of logs)
2. Check the number of log lines

**Expected results**:
- ✅ The number of log lines does not exceed 1000 lines
- ✅ Old logs are deleted correctly
- ✅ Stable memory usage

#### Test 2.3: Quick start and stop
**Steps**:
1. Click the "Start" and "Stop" buttons 10 times in quick succession
2. Observe application status

**Expected results**:
- ✅ App does not crash
- ✅ No memory leaks
- ✅ Log display is normal

---

### 3. Chinese coding test

#### Test 3.1: Chinese log display
**Steps**:
1. Output Chinese logs in NCF application
2. Observe the UI display

**Expected results**:
- ✅ Chinese is displayed correctly, no garbled characters
- ✅ Emoji are displayed normally (if available)

**Repair** (if garbled characters appear):
```csharp
// 在 NcfService.cs 的 startInfo 中添加
startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
```---

### 4. Abnormal scenario testing

#### Test 4.1: Process exits abnormally
**Steps**:
1. Start NCF
2. Use Task Manager to force terminate the NCF process
3. Observe application reactions

**Expected results**:
- ✅ The application detected process exit
- ✅ Show exit message
- ✅ App does not crash
- ✅ Status updated correctly to "Stopped"

#### Test 4.2: Callback exception handling
**Steps**:
1. Simulate the callback to throw an exception (temporarily modify the code)
2. Start NCF

**Expected results**:
- ✅Exception is caught by catch
- ✅ Log recording warning messages
- ✅ App continues to run

#### Test 4.3: Thread Safety
**Steps**:
1. Trigger a large number of log outputs at the same time (simulating high concurrency)
2. Observe the log display

**Expected results**:
- ✅ No logs lost
- ✅ No disorder (timestamps are in correct order)
- ✅ No crashes or exceptions

---

### 5. Cross-platform testing

#### Test 5.1: Windows Platform
**Steps**:
1. Run the app on Windows
2. Perform all basic functional tests

**Expected results**:
- ✅ All functions are working properly

#### Test 5.2: macOS platform
**Steps**:
1. Run the app on macOS
2. Perform all basic functional tests

**Expected results**:
- ✅ All functions are working properly
- ✅ Self-contained executable files start normally

#### Test 5.3: Linux platform (if supported)
**Steps**:
1. Run the application on Linux
2. Perform all basic functional tests

**Expected results**:
- ✅ All functions are working properly

---

## 🔧 Optimization suggestions

### Optimization 1: Batch update (if performance is insufficient)

**Trigger condition**: UI freezes when log output frequency > 200 entries/second

**Implementation plan**:
Refer to the batch update code in `step-02-viewmodel-integration.md`.

**Performance improvements**:
- UI update frequency: each log → every 200ms
-CPU usage: reduced by 50-70%
- UI fluency: significantly improved

### Optimization 2: Virtual scrolling (if there are too many log lines)

**Trigger condition**: The scrolling is stuck when the number of log lines > 5000 lines

**Implementation plan**:
- Using Avalonia's `VirtualizingStackPanel`
- Only render log lines in the visible area
- Need to change the log from string to ObservableCollection<LogEntry>

### Optimization 3: Log compression

**Trigger condition**: Memory usage > 100MB

**Implementation plan**:
```csharp
// 压缩重复日志
private string CompressLogs(string logText)
{
    var lines = logText.Split('\n');
    var compressed = new List<string>();
    string lastLine = null;
    int repeatCount = 0;
    
    foreach (var line in lines)
    {
        if (line == lastLine)
        {
            repeatCount++;
        }
        else
        {
            if (repeatCount > 1)
            {
                compressed.Add($"    (上一行重复 {repeatCount} 次)");
            }
            compressed.Add(line);
            lastLine = line;
            repeatCount = 1;
        }
    }
    
    return string.Join('\n', compressed);
}
```---

## ✅ Acceptance Criteria

### Functional completeness
- [ ] All basic functional tests passed
- [ ] All abnormal scenario tests passed
- [ ] Cross-platform compatibility verification passed

### Performance indicators
- [ ] Normal log output (< 100 items/second) UI smooth
- [ ] Large amount of log output (200-500 entries/second) available
- [ ] Stable memory usage (< 150MB, including NCF process)
- [ ] The limit on the number of log lines takes effect

### User experience
- [ ] Chinese display is normal
- [ ] Logs are updated in real time without obvious delay
- [ ] The interface responds quickly and has no lag.
- [ ] Clear and friendly error messages

---

## 📝 Test report template
```markdown
## CLI 输出同步功能测试报告

**测试日期**：2025-11-16
**测试平台**：Windows 11 / macOS 15 / Linux
**测试人员**：[姓名]

### 测试结果概览
- 基本功能：✅ 通过 / ⚠️ 部分通过 / ❌ 未通过
- 性能测试：✅ 通过 / ⚠️ 部分通过 / ❌ 未通过
- 异常场景：✅ 通过 / ⚠️ 部分通过 / ❌ 未通过

### 发现的问题
1. [问题描述]
   - 严重程度：🔴 高 / 🟡 中 / 🟢 低
   - 复现步骤：...
   - 预期结果：...
   - 实际结果：...

### 性能数据
- 平均日志输出频率：XX 条/秒
- UI 响应时间：XX ms
- CPU 占用：XX%
- 内存占用：XX MB

### 建议
- [优化建议或改进方向]
```---

## 🐛 FAQ Troubleshooting

### Problem 1: The log is not displayed
**Possible reasons**:
- callback not registered correctly
- Dispatcher call error
- The process did not start correctly

**Troubleshooting steps**:
1. Check whether `OnProcessOutput` is assigned a value
2. Check whether `BeginOutputReadLine()` is called
3. Add a breakpoint to verify whether the callback is triggered

### Problem 2: Chinese garbled characters
**Possible reasons**:
- Encoding setting error

**Solution**:
```csharp
startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
```### Problem 3: UI stuck
**Possible reasons**:
- The log is updated too frequently
- Perform time-consuming operations on the UI thread

**Solution**:
- Implement batch update mechanism
- Use `Post` instead of `Invoke`

### Problem 4: Memory continues to grow
**Possible reasons**:
- The limit on the number of log lines does not take effect
- Events are not properly unsubscribed

**Solution**:
- Verify log truncation logic
- Cleanup callback when stopped

---

## 🔗 Related tasks
- Previous step: [Step 02: Integrate CLI log output in MainWindowViewModel](./step-02-viewmodel-integration.md)
- Next step: Complete core functions, optional UI enhancement (TASK-04, TASK-05)
