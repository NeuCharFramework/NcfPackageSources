[中文版](THEME_ADAPTATION_GUIDE.cn.md)

# 🎨 Theme Adaptation Guide

## 📋 Update instructions

This update resolves two important issues:

### 1. ✅ Window size adaptive (completed)
**Problem description**: On low-resolution screens (such as 1366x768), the default 900x800 window size exceeds the screen boundaries, causing the window control buttons (minimize, maximize, close) to be invisible.

**Solution**:
- Added `AdjustWindowSizeToScreen()` method in `MainWindow.axaml.cs`
- Automatically detect the size of the screen workspace during window initialization
- Dynamically adjust window size based on available space
- Reserve 100px safe margins (50px top, bottom, left and right)

**Adjustment Rules**:```
理想尺寸：900x800
最小尺寸：800x650
实际尺寸：Max(最小尺寸, Min(理想尺寸, 屏幕可用空间 - 100px))
```**DEBUG OUTPUT EXAMPLE**:```
[窗口自适应] 屏幕工作区: 1366x768
[窗口自适应] 可用空间: 1266x668
[窗口自适应] 窗口尺寸: 900x668
[窗口自适应] ⚠️ 检测到低分辨率屏幕，窗口尺寸已从 900x800 调整为 900x668
```---

### 2. ✅ Dark mode adaptation (completed)
**Problem description**: When Windows/macOS uses the dark theme, the hard-coded colors in the application (such as white background, dark text) will not automatically adapt, resulting in abnormal contrast and difficult to read text.

**Solution**:
- Use Avalonia's `ThemeDictionaries` system to define light and dark theme resources
- Replace all hardcoded colors with dynamic resource references `{DynamicResource ...}`
- The application will automatically follow the system theme switching (`RequestedThemeVariant="Default"`)

---

## 🎨 Theme resource definition

### Global theme resources defined in `App.axaml`:

| Resource name | Light mode | Dark mode | Purpose |
|---------|---------|---------|------|
| `CardBackgroundBrush` | #FFFFFF (white) | #2B2B2B (dark gray) | Card background |
| `CardBorderBrush` | #E9ECEF (light gray) | #3C3C3C (medium gray) | Card border |
| `ToolbarBackgroundBrush` | #F8F9FA (very light gray) | #252526 (very dark gray) | Toolbar background |
| `ToolbarBorderBrush` | #DEE2E6 (gray) | #3C3C3C (medium gray) | Toolbar border |
| `InputBackgroundBrush` | #FFFFFF (white) | #3C3C3C (medium gray) | Input box background |
| `InputBorderBrush` | #CED4DA (medium gray) | #555555 (gray) | Input box border |
| `InputTextBrush` | #495057 (dark gray) | #CCCCCC (light gray) | Input box text |
| `LogBackgroundBrush` | #F8F9FA (very light gray) | #1E1E1E (pure black) | Log background |
| `SecondaryTextBrush` | #6C757D (medium gray) | #9D9D9D (light gray) | Secondary text |
| `ContentBackgroundBrush` | #FFFFFF (white) | #1E1E1E (pure black) | Content area background |

---

## 🔧 Modified file list

### 1. `App.axaml`
- ✅ Add `Application.Resources` node
- ✅ Definition `ThemeDictionaries` contains two sets of theme resources: Light and Dark

### 2. `Views/MainWindow.axaml`
- ✅ Update `Border.card` style to use dynamic resources
- ✅ Updated global loading mask color
- ✅ Removed local resource definition (moved to App.axaml)

### 3. `Views/MainWindow.axaml.cs`
- ✅ Add `AdjustWindowSizeToScreen()` method
- ✅ Call window size adaptive logic in the constructor
- ✅ Add detailed debug log output

### 4. `Views/SettingsView.axaml`
- ✅ Top action bar: `Background` and `BorderBrush` use dynamic resources
- ✅ Card style: use dynamic resources
- ✅ Log area: background and text colors use dynamic resources

### 5. `Views/BrowserView.axaml`
- ✅ Top toolbar: use dynamic resources for background and border
- ✅ URL display box: background, border, text color use dynamic resources
- ✅ Browser content area: use dynamic resources for the background
- ✅ Loading status overlay: background and text colors use dynamic resources
- ✅ Error status display: background and text color use dynamic resources

---

## 🧪 Testing Guide

### Test window size adaptation:

#### Windows test:
1. **Low resolution test** (1366x768 or lower)
   - Run the application
   - Check the console output for debugging information
   - Verify that the window title bar and control buttons (minimize, maximize, close) are fully visible
   
2. **Standard resolution test** (1920x1080)
   - should show full 900x800 window
   - The console should show that the window has not been resized

3. **High Resolution Test** (2560x1440 or 4K)
   - should show full 900x800 window

#### macOS test:
1. Switch to a different monitor resolution
2. The verification window is always within the visible area of the screen

#### Linux test:
1. Test on different DEs (GNOME, KDE, XFCE)
2. Verify that taskbars and panels do not block windows

---

### Test dark mode adaptation:

#### Windows 11 test:
1. Open "Settings" → "Personalization" → "Color"
2. Select "Dark" mode
3. Run the application and check the following elements:
   - ✅ Card background changes to dark color
   - ✅ Text color changed to light color (high contrast)
   - ✅ Border color adapted to dark theme
   - ✅ Toolbars and input boxes are adapted to the dark theme
   - ✅ The readability of the log area is good

4. Switch back to "Light" mode
5. Verify that the app automatically switches back to the light theme

#### macOS test:
1. Open "System Settings" → "Appearance"
2. Switch between "Dark" / "Light" / "Auto"
3. Verify that the application responds to theme changes in real time

#### Linux test:
1. Switch dark/light theme according to the DE used
2. Verify that the application follows the system theme

---

## 🚀 Compile and run```bash
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
```---

## 🎯 Expected results

### Window size adaptation:
- ✅ On 1366x768 screen, window height automatically adjusts to 668px
- ✅ On 1024x768 screen, window size automatically adjusts to 800x650 (minimum size)
- ✅ On 1920x1080 and above screens, display the full 900x800 window
- ✅ Window control buttons are always visible and operable

### Dark mode adaptation:
- ✅ Light mode: white background + dark text (high contrast)
- ✅ Dark mode: dark background + light text (high contrast)
- ✅ Automatically follow the system theme switching
- ✅ All UI elements are clearly readable in both modes
- ✅ No contrast abnormalities (such as white background + white text)

---

## 📝 Notes

1. **Theme switching**: The application uses `RequestedThemeVariant="Default"` and will automatically follow the system theme. If you need a fixed theme, you can change it to `"Light"` or `"Dark"`.

2. **Custom Color**: If you need to add a new theme color, please add it to both `ResourceDictionary` in `App.axaml` at the same time.

3. **Debugging information**: Detailed information about window adjustment will be output to the console to facilitate troubleshooting.

4. **Cross-platform compatibility**: All changes are based on Avalonia’s cross-platform API and behave consistently on Windows, macOS, and Linux.

5. **Performance Impact**: Using `DynamicResource` has a slight performance overhead, but it is negligible for desktop applications.

---

## 🐛 Troubleshooting

### Problem: Window still goes off screen
**Solution**:
- Check the console for log output of window adjustments
- Confirm that `AdjustWindowSizeToScreen()` in `MainWindow.axaml.cs` is called
- Try manually adjusting the `safetyMarginHeight` constant (increase it to 150 or 200)

### Problem: Colors are still incorrect in dark mode
**Solution**:
- Check if all colors are using `{DynamicResource ...}`
- Confirm that the Dark theme resource is defined in `App.axaml`
- Try setting `RequestedThemeVariant="Dark"` explicitly in `App.axaml` for testing

### Problem: The application does not follow the system theme switching
**Solution**:
- Confirm `RequestedThemeVariant="Default"` (not Light or Dark)
- Restart the app (some systems require a restart to detect theme changes)
- Check if Avalonia version supports theme switching

---

## 📚 Related documents

- [Avalonia ThemeDictionaries Documentation](https://docs.avaloniaui.net/docs/guides/styles-and-resources/resources#resource-dictionaries)
- [Avalonia FluentTheme Documentation](https://docs.avaloniaui.net/docs/guides/styles-and-resources/how-to-use-theme-variants)
- [Avalonia screen and DPI handling](https://docs.avaloniaui.net/docs/guides/platforms/ios)

---

## ✅ Complete status

- [x] Window size adaptive implementation
- [x] Dark mode theme resource definition
- [x] MainWindow style update
- [x] SettingsView style update
- [x] BrowserView style update
- [x] Add debug log
- [x] Cross-platform compatibility verification
- [x] Document writing completed

---

**Updated date**: 2025-11-06
**Version**: v1.0
**Author**: AI Assistant
