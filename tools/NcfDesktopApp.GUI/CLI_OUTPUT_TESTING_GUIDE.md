[中文版](CLI_OUTPUT_TESTING_GUIDE.cn.md)

# CLI output real-time synchronization function - Testing Guide

## 📋 Test Overview

This guide is used to verify the functionality of Senparc.Web CLI output to be synchronized to the UI log in real time.

**Function description**:
- ✅ Capture the standard output (stdout) of the NCF process in real time
- ✅ Capture the error output (stderr) of the NCF process in real time
- ✅ Displayed in UI log panel with `[CLI]` and `[CLI:ERROR]` prefixes
- ✅ Display mixed with application logs, arranged in chronological order

---

## ✅ Basic functional test list

### Test 1: Normal startup and log display

**Test steps**:
1. Start NcfDesktopApp
2. Click the "Start NCF" button
3. Observe the log panel on the right

**Expected results**:```
[HH:mm:ss] 🚀 开始启动 NCF...
[HH:mm:ss] 🌐 使用端口: 5001
[HH:mm:ss] 🚀 NCF 进程已启动 (PID: 12345)
[HH:mm:ss] [CLI] info: Microsoft.Hosting.Lifetime[14]
[HH:mm:ss] [CLI]       Now listening on: http://localhost:5001
[HH:mm:ss] [CLI] info: Microsoft.Hosting.Lifetime[0]
[HH:mm:ss] [CLI]       Application started. Press Ctrl+C to shut down.
[HH:mm:ss] ✅ NCF 站点已启动: http://localhost:5001
```**Verification Point**:
- [ ] see application logs (without prefix or with emoji)
- [ ] see CLI logs (prefixed with `[CLI]`)
- [ ] Logs are displayed mixed in chronological order
- [ ] Log updated in real time without obvious delay (< 1 second)
- [ ] ASP.NET Core startup information is displayed correctly

---

### Test 2: Chinese log display

**Test steps**:
1. Start the NCF application
2. Add Chinese log output in the NCF code, for example:```csharp
   _logger.LogInformation("测试中文日志：你好世界！");
   ```3. Trigger the log output
4. Observe the UI log panel

**Expected results**:```
[HH:mm:ss] [CLI] info: YourNamespace.YourClass[0]
[HH:mm:ss] [CLI]       测试中文日志：你好世界！
```**Verification Point**:
- [ ] Chinese display is normal, no garbled characters
- [ ] Emoji are displayed normally (if available)

---

### Test 3: Error output capture

**Test steps**:
1. Modify the NCF port to an occupied port (such as 80 or 443)
2. Try to start NCF
3. Observe the log panel

**Expected results**:```
[HH:mm:ss] 🚀 开始启动 NCF...
[HH:mm:ss] [CLI:ERROR] Unhandled exception. System.IO.IOException: Failed to bind to address http://localhost:80
[HH:mm:ss] [CLI:ERROR]    at ...
[HH:mm:ss] ❌ 启动失败: ...
```**Verification Point**:
- [ ] Error output is shown with the `[CLI:ERROR]` prefix
- [ ] App does not crash
- [ ] The error message is displayed in full

---

### Test 4: Process stops processing

**Test steps**:
1. Start NCF
2. Wait for startup to complete
3. Click the "Stop NCF" button
4. Observation log

**Expected results**:```
[HH:mm:ss] 🛑 正在停止 NCF 进程...
[HH:mm:ss] [CLI] --- 进程已退出 ---
[HH:mm:ss] 🔪 已使用 taskkill 终止进程树 (PID: 12345)
[HH:mm:ss] ✅ NCF 已停止
```**Verification Point**:
- [ ] Display process exit message
- [ ] App status updates correctly
- [ ] No exceptions or errors

---

### Test 5: Abnormal process exit

**Test steps**:
1. Start NCF
2. Use Task Manager (Windows) or Activity Monitor (macOS) to force terminate the NCF process
3. Observe application reactions

**Expected results**:
- [ ] Application detects process exit
- [ ] Show "Process has exited" message
- [ ] App does not crash
- [ ] status updated correctly

---

## 🚀 Performance Test Checklist

### Test 6: Massive log output

**Test scenario**: NCF will output a large number of logs (about 50-100) when it starts.

**Verification Point**:
- [ ] UI is not stuck, buttons are clickable
- [ ] Log scrolling is smooth
- [ ] CPU usage is normal (< 20%)
- [ ] Memory usage is stable

**Performance Benchmark**:
- Normal log output (< 100 entries/second): UI should be completely smooth
- If you find lag, please report the specific scenario

---

### Test 7: Log line limit

**Test steps**:
1. Run NCF for a long time (or trigger large amounts of log output)
2. Observe the log panel

**Verification Point**:
- [ ] The log will not grow indefinitely
- [ ] Old logs are automatically deleted (retaining the latest 1000 lines)
- [ ] Memory usage is stable

---

### Test 8: Quick start and stop test

**Test steps**:
1. Click the "Start" and "Stop" buttons quickly and continuously 5-10 times
2. Observe application status

**Verification Point**:
- [ ] App does not crash
- [ ] No memory leaks
- [ ] Log display is normal
- [ ] No "ghost processes" (stopped but still logging output)

---

## 🔍 Boundary condition testing

### Test 9: Fallback startup scenario

**Background**: NCF has three startup branches (self-contained → rollback → secondary rollback)

**Test steps**:
1. Delete or rename `Senparc.Web.exe` (Windows) or `Senparc.Web` (macOS/Linux)
2. Make sure `Senparc.Web.dll` exists
3. Start NCF

**Expected results**:
- [ ] The application automatically falls back to `dotnet` mode to start
- [ ] Log shows "fallback to dotnet mode"
- [ ] CLI output is still captured normally
- [ ] fully functional

---

### Test 10: Cross-platform testing

**Windows**:
- [ ] All basic functions are normal
- [ ] Chinese display is normal
- [ ] Use `taskkill` to stop the process

**macOS**:
- [ ] All basic functions are normal
- [ ] Self-contained executables start normally
- [ ] Rollback starts normally

**Linux** (if supported):
- [ ] All basic functions are normal
- [ ] Permissions are handled correctly

---

## 📊 Test result record

### Test environment

- **OS**:_______________ (Example: Windows 11/macOS 15/Ubuntu 22.04)
- **.NET Version**:_______________ (Example: .NET 8.0.1)
- **NCF Version**:_______________ (Example: v2.3.0)
- **Test Date**:_______________

### Summary of test results

| Test items | Status | Notes |
|-------|------|------|
| 1. Normal startup and log display | ☐ Pass ☐ Fail | |
| 2. Chinese log display | ☐ Pass ☐ Fail | |
| 3. Error output capture | ☐ Pass ☐ Fail | |
| 4. The process stops processing | ☐ Pass ☐ Fail | |
| 5. Abnormal process exit | ☐ Pass ☐ Fail | |
| 6. A large amount of log output | ☐ Pass ☐ Fail | |
| 7. Log line limit | ☐ Pass ☐ Fail | |
| 8. Quick start and stop test | ☐ Pass ☐ Fail | |
| 9. Fallback startup scenario | ☐ Pass ☐ Fail | |
| 10. Cross-platform testing | ☐ Pass ☐ Fail | |

### Issues found

**Question 1**:
- **Severity**: ☐ High ☐ Medium ☐ Low
- **Description**:
- **Reproduction Steps**:
- **Expected results**:
- **Actual results**:

**Question 2**:
_(If you have more questions, please continue to add)_

---

## 🐛 Known limitations and notes

1. **Log timestamp**:
   - The timestamp of the CLI log is the time the UI received the log
   - There may be a slight delay (< 1 second) from the actual NCF output time

2. **Log order**:
   - at extremely high frequenciesUnder output, the order of stdout and stderr may be slightly different from the original output
   - This is a limitation of the operating system process mechanism and does not affect functionality

3. **Performance considerations**:
   - The current implementation is suitable for normal usage scenarios (< 100 items/second)
   - If you encounter extreme high-frequency output that causes lag, you can consider batch update optimization.

---

## 📞Feedback

If problems are found during testing, please provide the following information:

1. **Test environment**: operating system, .NET version
2. **Reproduction Steps**: Detailed steps
3. **Expected Behavior**: How it should work
4. **Actual Behavior**: What actually happened
5. **Log screenshots**: If possible, provide log screenshots
6. **Error message**: Complete error message or stack trace

---

## ✅ Acceptance Criteria

Features can be considered "done" by the following criteria:

- ✅ All basic functional tests (tests 1-5) passed
- ✅ Performance test (Test 6-8) passed, no obvious performance issues
- ✅ Cross-platform test passed on at least one platform
- ✅ Chinese display is normal, no garbled characters
- ✅ The application is stable and has no crashes

Optional enhancements (not mandatory):
- Log filtering function
- Color-coded CLI and application logs
- Log search function

---

**Test Completion Date**:_______________
**Tester**:_______________
**Overall rating**: ☐ Excellent ☐ Good ☐ Needs improvement
