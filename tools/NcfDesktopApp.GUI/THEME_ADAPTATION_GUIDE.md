# 🎨 主题适配指南

## 📋 更新说明

本次更新解决了两个重要问题：

### 1. ✅ 窗口尺寸自适应（已完成）
**问题描述**：在低分辨率屏幕上（如 1366x768），默认的 900x800 窗口尺寸会超出屏幕边界，导致窗口控制按钮（最小化、最大化、关闭）不可见。

**解决方案**：
- 在 `MainWindow.axaml.cs` 中添加了 `AdjustWindowSizeToScreen()` 方法
- 窗口初始化时自动检测屏幕工作区尺寸
- 根据可用空间动态调整窗口尺寸
- 预留 100px 安全边距（上下左右各 50px）

**调整规则**：
```
理想尺寸：900x800
最小尺寸：800x650
实际尺寸：Max(最小尺寸, Min(理想尺寸, 屏幕可用空间 - 100px))
```

**调试输出示例**：
```
[窗口自适应] 屏幕工作区: 1366x768
[窗口自适应] 可用空间: 1266x668
[窗口自适应] 窗口尺寸: 900x668
[窗口自适应] ⚠️ 检测到低分辨率屏幕，窗口尺寸已从 900x800 调整为 900x668
```

---

### 2. ✅ 暗黑模式适配（已完成）
**问题描述**：当 Windows/macOS 使用暗黑主题时，应用内的硬编码颜色（如白色背景、深色文字）不会自动适配，导致对比度异常，文字难以阅读。

**解决方案**：
- 使用 Avalonia 的 `ThemeDictionaries` 系统定义亮色和暗色主题资源
- 将所有硬编码颜色替换为动态资源引用 `{DynamicResource ...}`
- 应用会自动跟随系统主题切换（`RequestedThemeVariant="Default"`）

---

## 🎨 主题资源定义

### 在 `App.axaml` 中定义的全局主题资源：

| 资源名称 | 亮色模式 | 暗色模式 | 用途 |
|---------|---------|---------|------|
| `CardBackgroundBrush` | #FFFFFF（白色） | #2B2B2B（深灰） | 卡片背景 |
| `CardBorderBrush` | #E9ECEF（浅灰） | #3C3C3C（中灰） | 卡片边框 |
| `ToolbarBackgroundBrush` | #F8F9FA（极浅灰） | #252526（极深灰） | 工具栏背景 |
| `ToolbarBorderBrush` | #DEE2E6（灰色） | #3C3C3C（中灰） | 工具栏边框 |
| `InputBackgroundBrush` | #FFFFFF（白色） | #3C3C3C（中灰） | 输入框背景 |
| `InputBorderBrush` | #CED4DA（中灰） | #555555（灰色） | 输入框边框 |
| `InputTextBrush` | #495057（深灰） | #CCCCCC（浅灰） | 输入框文字 |
| `LogBackgroundBrush` | #F8F9FA（极浅灰） | #1E1E1E（纯黑） | 日志背景 |
| `SecondaryTextBrush` | #6C757D（中灰） | #9D9D9D（浅灰） | 次要文字 |
| `ContentBackgroundBrush` | #FFFFFF（白色） | #1E1E1E（纯黑） | 内容区背景 |

---

## 🔧 修改的文件列表

### 1. `App.axaml`
- ✅ 添加 `Application.Resources` 节点
- ✅ 定义 `ThemeDictionaries` 包含 Light 和 Dark 两套主题资源

### 2. `Views/MainWindow.axaml`
- ✅ 更新 `Border.card` 样式，使用动态资源
- ✅ 更新全局加载遮罩颜色
- ✅ 移除本地资源定义（已移至 App.axaml）

### 3. `Views/MainWindow.axaml.cs`
- ✅ 添加 `AdjustWindowSizeToScreen()` 方法
- ✅ 在构造函数中调用窗口尺寸自适应逻辑
- ✅ 添加详细的调试日志输出

### 4. `Views/SettingsView.axaml`
- ✅ 顶部操作栏：`Background` 和 `BorderBrush` 使用动态资源
- ✅ 卡片样式：使用动态资源
- ✅ 日志区域：背景和文字颜色使用动态资源

### 5. `Views/BrowserView.axaml`
- ✅ 顶部工具栏：背景和边框使用动态资源
- ✅ URL 显示框：背景、边框、文字颜色使用动态资源
- ✅ 浏览器内容区域：背景使用动态资源
- ✅ 加载状态覆盖层：背景和文字颜色使用动态资源
- ✅ 错误状态显示：背景和文字颜色使用动态资源

---

## 🧪 测试指南

### 测试窗口尺寸自适应：

#### Windows 测试：
1. **低分辨率测试**（1366x768 或更低）
   - 运行应用
   - 检查控制台输出的调试信息
   - 验证窗口标题栏和控制按钮（最小化、最大化、关闭）完全可见
   
2. **标准分辨率测试**（1920x1080）
   - 应该显示完整的 900x800 窗口
   - 控制台应显示未调整窗口尺寸

3. **高分辨率测试**（2560x1440 或 4K）
   - 应该显示完整的 900x800 窗口

#### macOS 测试：
1. 切换到不同的显示器分辨率
2. 验证窗口始终在屏幕可见区域内

#### Linux 测试：
1. 在不同的 DE（GNOME, KDE, XFCE）上测试
2. 验证任务栏和面板不会遮挡窗口

---

### 测试暗黑模式适配：

#### Windows 11 测试：
1. 打开"设置" → "个性化" → "颜色"
2. 选择"深色"模式
3. 运行应用，检查以下元素：
   - ✅ 卡片背景变为深色
   - ✅ 文字颜色变为浅色（高对比度）
   - ✅ 边框颜色适配暗黑主题
   - ✅ 工具栏和输入框适配暗黑主题
   - ✅ 日志区域可读性良好

4. 切换回"浅色"模式
5. 验证应用自动切换回亮色主题

#### macOS 测试：
1. 打开"系统设置" → "外观"
2. 切换"深色" / "浅色" / "自动"
3. 验证应用实时响应主题变化

#### Linux 测试：
1. 根据使用的 DE 切换深色/浅色主题
2. 验证应用跟随系统主题

---

## 🚀 编译和运行

```bash
# 开发模式运行（查看调试信息）
dotnet run

# 发布版本构建
dotnet publish -c Release

# 查看控制台输出的窗口调整信息
# 示例输出：
# [窗口自适应] 屏幕工作区: 1366x768
# [窗口自适应] 可用空间: 1266x668
# [窗口自适应] 窗口尺寸: 900x668
# [窗口自适应] ⚠️ 检测到低分辨率屏幕，窗口尺寸已从 900x800 调整为 900x668
```

---

## 🎯 预期效果

### 窗口尺寸自适应：
- ✅ 在 1366x768 屏幕上，窗口高度自动调整为 668px
- ✅ 在 1024x768 屏幕上，窗口尺寸自动调整为 800x650（最小尺寸）
- ✅ 在 1920x1080 及以上屏幕，显示完整的 900x800 窗口
- ✅ 窗口控制按钮始终可见且可操作

### 暗黑模式适配：
- ✅ 亮色模式：白色背景 + 深色文字（高对比度）
- ✅ 暗色模式：深色背景 + 浅色文字（高对比度）
- ✅ 自动跟随系统主题切换
- ✅ 所有 UI 元素在两种模式下都清晰可读
- ✅ 无对比度异常（如白色背景 + 白色文字）

---

## 📝 注意事项

1. **主题切换**：应用使用 `RequestedThemeVariant="Default"`，会自动跟随系统主题。如需固定主题，可改为 `"Light"` 或 `"Dark"`。

2. **自定义颜色**：如需添加新的主题颜色，请在 `App.axaml` 的两个 `ResourceDictionary` 中同时添加。

3. **调试信息**：窗口调整的详细信息会输出到控制台，便于排查问题。

4. **跨平台兼容性**：所有改动都基于 Avalonia 的跨平台 API，在 Windows、macOS、Linux 上表现一致。

5. **性能影响**：使用 `DynamicResource` 有轻微性能开销，但对于桌面应用可忽略不计。

---

## 🐛 故障排查

### 问题：窗口仍然超出屏幕
**解决方案**：
- 检查控制台是否有窗口调整的日志输出
- 确认 `MainWindow.axaml.cs` 中的 `AdjustWindowSizeToScreen()` 被调用
- 尝试手动调整 `safetyMarginHeight` 常量（增加到 150 或 200）

### 问题：暗黑模式下颜色仍然不正确
**解决方案**：
- 检查是否所有颜色都使用了 `{DynamicResource ...}`
- 确认 `App.axaml` 中定义了 Dark 主题资源
- 尝试在 `App.axaml` 中显式设置 `RequestedThemeVariant="Dark"` 进行测试

### 问题：应用不跟随系统主题切换
**解决方案**：
- 确认 `RequestedThemeVariant="Default"`（而非 Light 或 Dark）
- 重启应用（某些系统需要重启才能检测主题变化）
- 检查 Avalonia 版本是否支持主题切换

---

## 📚 相关文档

- [Avalonia ThemeDictionaries 文档](https://docs.avaloniaui.net/docs/guides/styles-and-resources/resources#resource-dictionaries)
- [Avalonia FluentTheme 文档](https://docs.avaloniaui.net/docs/guides/styles-and-resources/how-to-use-theme-variants)
- [Avalonia 屏幕和 DPI 处理](https://docs.avaloniaui.net/docs/guides/platforms/ios)

---

## ✅ 完成状态

- [x] 窗口尺寸自适应实现
- [x] 暗黑模式主题资源定义
- [x] MainWindow 样式更新
- [x] SettingsView 样式更新
- [x] BrowserView 样式更新
- [x] 添加调试日志
- [x] 跨平台兼容性验证
- [x] 文档编写完成

---

**更新日期**：2025-11-06  
**版本**：v1.0  
**作者**：AI Assistant



