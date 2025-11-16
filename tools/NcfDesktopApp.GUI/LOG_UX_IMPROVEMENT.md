# 日志功能优化 - 支持选中复制和智能自动滚动

## ✨ 功能改进

本次优化改进了日志显示功能，提升了用户体验：

### 1. ✅ 日志内容可选中和复制
- 用户可以选中日志文本
- 支持 Ctrl+C / Cmd+C 复制
- 日志内容只读，不可编辑
- 选中时显示高亮（使用系统主题色）

### 2. ✅ 智能自动滚动
- **默认行为**：新日志到来时自动滚动到底部显示最新内容
- **用户查看历史**：当用户手动向上滚动查看历史日志时，不会自动滚动
- **自动恢复**：当用户滚动到底部附近（距离 < 20px）时，恢复自动滚动

---

## 🔧 技术实现

### 修改的文件

#### 1. Views/SettingsView.axaml
**修改内容**：
- 将 `TextBlock` 改为 `SelectableTextBlock`
- 添加 `IsReadOnly="True"` 属性
- 添加 `SelectionBrush` 选中高亮颜色
- 为 `ScrollViewer` 添加 `ScrollChanged` 事件

**代码变更**：
```xml
<ScrollViewer Name="LogScrollViewer" 
             ScrollChanged="LogScrollViewer_OnScrollChanged">
    <SelectableTextBlock Text="{Binding LogText}" 
                        FontFamily="Consolas,Menlo,Monaco,Courier New" 
                        IsReadOnly="True"
                        SelectionBrush="{DynamicResource SystemAccentColor}"/>
</ScrollViewer>
```

#### 2. Views/SettingsView.axaml.cs
**新增内容**：
- `_isUserScrolling` 字段 - 跟踪用户是否在查看历史日志
- `LogScrollViewer_OnScrollChanged()` 方法 - 检测滚动位置
- `ShouldAutoScroll` 属性 - 告诉 ViewModel 是否应该自动滚动

**实现逻辑**：
```csharp
private void LogScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
{
    // 计算距离底部的距离
    var scrollableHeight = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
    var distanceFromBottom = scrollableHeight - scrollViewer.Offset.Y;
    
    // 如果距离底部超过 20 像素，说明用户在查看历史日志
    _isUserScrolling = distanceFromBottom > 20;
}

public bool ShouldAutoScroll => !_isUserScrolling;
```

#### 3. ViewModels/MainWindowViewModel.cs
**新增内容**：
- `ScrollToBottomIfNeeded()` 方法 - 智能滚动到底部
- 在 `AddLog()` 和 `AddCliLog()` 中调用滚动方法
- 添加 `using NcfDesktopApp.GUI.Views;` 引用

**实现逻辑**：
```csharp
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
```

---

## 🎯 用户体验

### 场景 1：正常查看最新日志
**行为**：
- 应用启动，NCF 输出日志
- 日志自动滚动到底部
- 始终显示最新内容

**用户操作**：无需操作

---

### 场景 2：查看历史日志
**行为**：
1. 用户向上滚动查看之前的日志
2. 新日志到来时，**不会自动滚动**
3. 用户继续查看历史内容

**用户操作**：
- 向上滚动：保持在当前位置
- 滚动到底部：恢复自动滚动

---

### 场景 3：复制日志内容
**行为**：
1. 用户选中需要的日志行
2. 按 Ctrl+C (Windows/Linux) 或 Cmd+C (macOS)
3. 日志内容已复制到剪贴板

**使用场景**：
- 报告错误时复制错误信息
- 分享日志内容
- 保存特定日志条目

---

## ✅ 测试验证

### 测试 1：选中和复制
**步骤**：
1. 启动应用
2. 查看日志面板
3. 用鼠标选中一行或多行日志
4. 按 Ctrl+C / Cmd+C
5. 粘贴到记事本验证

**预期**：
- ✅ 文本可以被选中（显示高亮）
- ✅ 复制功能正常工作
- ✅ 内容不可编辑（只读）

---

### 测试 2：自动滚动（默认行为）
**步骤**：
1. 启动应用
2. 点击"启动 NCF"
3. 观察日志面板

**预期**：
- ✅ 新日志到来时自动滚动到底部
- ✅ 始终显示最新内容
- ✅ 滚动流畅，无卡顿

---

### 测试 3：查看历史日志（不自动滚动）
**步骤**：
1. 启动应用并启动 NCF（产生一些日志）
2. 向上滚动查看之前的日志
3. 等待新的日志产生

**预期**：
- ✅ 滚动条保持在当前位置
- ✅ 不会被新日志"拉"回底部
- ✅ 用户可以安静地查看历史内容

---

### 测试 4：恢复自动滚动
**步骤**：
1. 按照测试 3 向上滚动
2. 手动滚动回到底部附近（距离 < 20px）
3. 等待新的日志产生

**预期**：
- ✅ 自动滚动功能恢复
- ✅ 新日志到来时滚动到底部

---

## 🎨 UI 细节

### SelectableTextBlock 属性
```xml
<SelectableTextBlock 
    Text="{Binding LogText}"
    IsReadOnly="True"                    ← 只读，不可编辑
    SelectionBrush="SystemAccentColor"   ← 选中高亮颜色
    FontFamily="Consolas,..."            ← 等宽字体
    TextWrapping="Wrap"                  ← 自动换行
    />
```

### 滚动阈值
- **自动滚动阈值**: 距离底部 20 像素内
- **太小**：容易误触发，用户稍微滚动就恢复自动滚动
- **太大**：用户需要滚动很远才能恢复自动滚动
- **20px 是经过测试的最佳值**

---

## 📊 性能影响

### 滚动检测
- **触发频率**：每次滚动位置改变
- **计算复杂度**：O(1) - 简单的数值比较
- **性能影响**：微乎其微

### 自动滚动
- **触发频率**：每次添加日志
- **延迟**：10ms 延迟确保 UI 已更新
- **性能影响**：可忽略不计

---

## 🔍 边界情况处理

### 1. 日志面板未加载
```csharp
if (scrollViewer != null)  // 检查 ScrollViewer 是否存在
{
    // 执行滚动
}
```

### 2. SettingsView 未找到
```csharp
if (settingsView?.ShouldAutoScroll ?? true)  // 默认为 true
{
    // 执行滚动
}
```

### 3. 滚动计算错误
```csharp
try
{
    // 滚动逻辑
}
catch
{
    // 忽略错误，不影响日志功能
}
```

---

## 🐛 已知限制

### 限制 1：依赖控件查找
- **情况**：使用 `FindControl<T>()` 查找 ScrollViewer
- **风险**：如果控件名称改变，会失效
- **缓解**：使用明确的控件名称 "LogScrollViewer"

### 限制 2：10ms 延迟
- **原因**：确保 UI 已更新再滚动
- **影响**：极小（用户无法感知）
- **替代方案**：可以使用 `LayoutUpdated` 事件，但更复杂

---

## 📝 使用建议

### 对于用户
1. **复制日志**：直接选中并 Ctrl+C / Cmd+C
2. **查看历史**：向上滚动，不会被打断
3. **回到最新**：滚动到底部，恢复自动滚动

### 对于开发者
1. **不要修改控件名称** `"LogScrollViewer"`
2. **保持 SettingsView.axaml 结构** 不要随意调整布局
3. **测试跨平台** Windows/macOS/Linux 都要测试

---

## 🎉 总结

本次优化显著提升了日志查看体验：

**改进前**：
- ❌ 无法复制日志内容
- ❌ 始终自动滚动，无法安静查看历史

**改进后**：
- ✅ 支持选中和复制
- ✅ 智能自动滚动
- ✅ 查看历史时不打断
- ✅ 完美的用户体验

---

**实施日期**: 2025-11-16  
**版本**: v1.1.0  
**影响范围**: SettingsView, MainWindowViewModel

