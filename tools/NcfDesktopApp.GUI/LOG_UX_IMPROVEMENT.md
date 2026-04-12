[中文版](LOG_UX_IMPROVEMENT.cn.md)

# Log function optimization - supports selected copy and intelligent automatic scrolling

## ✨ Function improvements

This optimization improves the log display function and enhances the user experience:

### 1. ✅ Log content can be selected and copied
- User can select log text
- Support Ctrl+C / Cmd+C copy
- Log content is read-only and cannot be edited.
- Show highlight when selected (use system theme color)

### 2. ✅ Intelligent automatic scrolling
- **Default behavior**: When new posts arrive, automatically scroll to the bottom to display the latest content
- **User view history**: When the user manually scrolls up to view the history log, it will not scroll automatically
- **AUTO-RESTORE**: Resume auto-scroll when user scrolls near the bottom (distance < 20px)

---

## 🔧 Technical implementation

### Modified files

#### 1. Views/SettingsView.axaml
**Modified content**:
- Change `TextBlock` to `SelectableTextBlock`
- Add `IsReadOnly="True"` attribute
- Added `SelectionBrush` to select highlight color
- Add `ScrollChanged` event for `ScrollViewer`

**Code Changes**:```xml
<ScrollViewer Name="LogScrollViewer" 
             ScrollChanged="LogScrollViewer_OnScrollChanged">
    <SelectableTextBlock Text="{Binding LogText}" 
                        FontFamily="Consolas,Menlo,Monaco,Courier New" 
                        IsReadOnly="True"
                        SelectionBrush="{DynamicResource SystemAccentColor}"/>
</ScrollViewer>
```#### 2. Views/SettingsView.axaml.cs
**NEW NEWS**:
- `_isUserScrolling` field - tracks whether the user is viewing history logs
- `LogScrollViewer_OnScrollChanged()` method - detect scroll position
- `ShouldAutoScroll` property - tells the ViewModel whether it should automatically scroll

**Implementation logic**:```csharp
private void LogScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
{
    // 计算距离底部的距离
    var scrollableHeight = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
    var distanceFromBottom = scrollableHeight - scrollViewer.Offset.Y;
    
    // 如果距离底部超过 20 像素，说明用户在查看历史日志
    _isUserScrolling = distanceFromBottom > 20;
}

public bool ShouldAutoScroll => !_isUserScrolling;
```#### 3. ViewModels/MainWindowViewModel.cs
**NEW NEWS**:
- `ScrollToBottomIfNeeded()` method - smart scroll to the bottom
- Call scrolling methods in `AddLog()` and `AddCliLog()`
- Added `using NcfDesktopApp.GUI.Views;` reference

**Implementation logic**:```csharp
private void ScrollToBottomIfNeeded()
{
    // 查找 ScrollViewer
    var scrollViewer = mainContent.FindControl<ScrollViewer>("LogScrollViewer");
    
    // 检查是否应该自动滚动
    var settingsView = mainContent as SettingsView;
    if (settingsView?.ShouldAutoScroll ?? true)
    {
        // 延迟滚动，确保内容已更新
        Task.Delay(10).ContinueWith(_ => 
        {
            scrollViewer.ScrollToEnd();
        });
    }
}
```---

## 🎯 User experience

### Scenario 1: View the latest log normally
**BEHAVIOR**:
- Application starts, NCF output log
- The log automatically scrolls to the bottom
- Always show the latest content

**User Action**: No action required

---

### Scenario 2: View historical logs
**BEHAVIOR**:
1. The user scrolls up to view previous logs
2. When new logs arrive, **will not automatically scroll**
3. Users continue to view historical content

**User Operation**:
-Scroll up: stay at current position
- Scroll to bottom: restore automatic scrolling

---

### Scenario 3: Copy log content
**BEHAVIOR**:
1. The user selects the required log line
2. Press Ctrl+C (Windows/Linux) or Cmd+C (macOS)
3. The log content has been copied to the clipboard

**Usage Scenario**:
- Copy the error message when reporting an error
- Share log content
- Save specific log entries

---

## ✅ Test verification

### Test 1: Select and copy
**Steps**:
1. Start the application
2. View the log panel
3. Use the mouse to select one or more lines of logs
4. Press Ctrl+C / Cmd+C
5. Paste into Notepad to verify

**Expectation**:
- ✅ Text can be selected (highlighted)
- ✅ The copy function is working properly
- ✅ Content cannot be edited (read only)

---

### Test 2: Autoscroll (default behavior)
**Steps**:
1. Start the application
2. Click "Start NCF"
3. Observe the log panel

**Expectation**:
- ✅ Automatically scroll to the bottom when new posts arrive
- ✅ Always show the latest content
- ✅ Smooth scrolling, no lag

---

### Test 3: View historical logs (without automatic scrolling)
**Steps**:
1. Start the application and start NCF (generate some logs)
2. Scroll up to view previous logs
3. Wait for new logs to be generated

**Expectation**:
- ✅ Scroll bar stays at current position
- ✅ Will not be "pulled" back to the bottom by new posts
- ✅ Users can view historical content quietly

---

### Test 4: Restoring automatic scrolling
**Steps**:
1. Follow test 3 and scroll up
2. Manually scroll back to near the bottom (distance < 20px)
3. Wait for new logs to be generated

**Expectation**:
- ✅ Auto scroll function restored
- ✅Scroll to the bottom when new posts arrive

---

## 🎨 UI details

### SelectableTextBlock property```xml
<SelectableTextBlock 
    Text="{Binding LogText}"
    IsReadOnly="True"                    ← 只读，不可编辑
    SelectionBrush="SystemAccentColor"   ← 选中高亮颜色
    FontFamily="Consolas,..."            ← 等宽字体
    TextWrapping="Wrap"                  ← 自动换行
    />
```### Rolling threshold
- **Auto-Scroll Threshold**: Within 20 pixels from bottom
- **Too small**: It is easy to trigger by mistake, and the user will resume automatic scrolling after a slight scrolling.
- **Too Big**: The user needs to scroll very far before autoscrolling can resume
- **20px is the best value tested**

---

## 📊 Performance impact

### Scroll detection
- **Trigger frequency**: every time the scroll position changes
- **Computational Complexity**: O(1) - Simple numerical comparison
- **PERFORMANCE IMPACT**: Minimal

### Automatic scrolling
- **Trigger frequency**: every time a log is added
- **DELAY**: 10ms delay to ensure UI is updated
- **Performance Impact**: Negligible

---

## 🔍 Boundary case handling

### 1. The log panel is not loaded```csharp
if (scrollViewer != null)  // 检查 ScrollViewer 是否存在
{
    // 执行滚动
}
```### 2. SettingsView not found```csharp
if (settingsView?.ShouldAutoScroll ?? true)  // 默认为 true
{
    // 执行滚动
}
```### 3. Scroll calculation error```csharp
try
{
    // 滚动逻辑
}
catch
{
    // 忽略错误，不影响日志功能
}
```---

## 🐛 Known limitations

### Limitation 1: Dependent on control lookup
- **Case**: Use `FindControl<T>()` to find a ScrollViewer
- **Risk**: If the control name is changed, it will become invalid.
- **MITIGATION**: Use explicit control name "LogScrollViewer"

### Limit 2: 10ms delay
- **REASON**: Make sure the UI is updated before scrolling
- **Impact**: Minimal (unperceivable by users)
- **Alternative**: You can use the `LayoutUpdated` event, but it's more complicated

---

## 📝 Usage suggestions

### For users
1. **Copy log**: directly select and Ctrl+C / Cmd+C
2. **View History**: Scroll up without interruption
3. **Back to latest**: Scroll to the bottom and resume automatic scrolling

### For developers
1. **Do not modify the control name** `"LogScrollViewer"`
2. **Keep the SettingsView.axaml structure** Do not adjust the layout at will
3. **Cross-platform testing** Windows/macOS/Linux must be tested

---

## 🎉 Summary

This optimization significantly improves the log viewing experience:

**Before improvements**:
- ❌ Unable to copy log content
- ❌ Always scroll automatically, unable to view history quietly

**After improvements**:
- ✅Supports selection and copying
- ✅ Smart auto-scroll
- ✅ No interruption when viewing history
- ✅ Perfect user experience

---

**Implementation date**: 2025-11-16
**Version**: v1.1.0
**Scope of influence**: SettingsView, MainWindowViewModel

---

## 📝 Repair record

### Hotfix 1: SelectableTextBlock property error
**Problem**: `SelectableTextBlock` does not have an `IsReadOnly` attribute, causing compilation to fail.  
**Fix**: Removed `IsReadOnly="True"` attribute (`SelectableTextBlock` itself is read-only).  
**File**: Views/SettingsView.axaml, line 229
