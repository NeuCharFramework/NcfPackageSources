# NCF Desktop App - Browser Integration Instructions

## 🚀 Current implementation status

### ✅ Function completed

1. **Dual Page Architecture**
- **Settings Page**: Complete NCF configuration and management interface
- **Browser Page**: Built-in browser interface (currently implemented as a placeholder)

2. **Page switching function**
- Use TabControl to switch between pages
- The settings page and browser page can be freely switched
- Intelligent navigation: Automatically switch to the browser page after NCF is started

3. **Hybrid User Experience**
- Automatically display the browser page after NCF is started
- Settings page is still available for configuration management
- Integrated user interface

4. **External browser integration**
- "Open in external browser" button
- Cross-platform browser launch support
- Downgrade scheme ensures functionality is available

### 🔄 Current architecture

```
MainWindow (TabControl)
├── ⚙️ 设置中心 (SettingsView)
│   ├── 状态信息
│   ├── 操作进度
│   ├── 配置选项
│   └── 操作按钮
└── 🌐 NCF 应用 (BrowserView)
    ├── 导航工具栏
    ├── 浏览器占位符 (暂时)
    └── 外部浏览器按钮
```

## 🚧 WebView Integration Plan

### Phase 1: Basic WebView (in progress)

**Problem Analysis**:
- `WebViewControl-Avalonia`There is a compatibility issue in the current environment
- Requires native library support (libEGL.dylib, libGLESv2.dylib, etc.)
- Cross-platform support for complexity

**Solution**:
1. **Evaluate Alternatives**
- Avalonia Accelerate WebView (Business Solutions)
- Other open source WebView components
- Custom WebView implementation

2. **Progressive Integration**
- Current: placeholder + external browser
- Short term: Simple WebView integration
- Long term: full built-in browser functionality

### Phase 2: Enhanced Functions

**Planning Features**:
- Complete navigation functions (forward, back, refresh)
- Address bar display and interaction
- Page loading status display
- Error handling and retry mechanism

### Phase 3: Advanced Features

**Extended functions**:
- Developer tools integration
- Bookmarks and history
- Screenshot and print pages
- JavaScript interaction

## 💡 Instructions for use

### Steps to use the current version

1. **Launch the application**
   ```bash
   cd tools/NcfDesktopApp.GUI
   dotnet run
   ```

2. **Configure NCF**
- Configure NCF parameters on the "Settings Center" page
- Set port ranges, download options, and more

3. **Start NCF**
- Click the "Start NCF" button
- The application will automatically switch to the browser page

4. **Visit NCF site**
- Click the "Open in external browser" button
- Access NCF in the system default browser

5. **Page switching**
- Use the top tabs to switch between settings and browser pages
- The browser page will only be enabled after NCF is ready

### Features

- **Smart Navigation**: Automatically switch to the browser page after NCF startup is completed
- **Status Synchronization**: Display NCF running status and site address in real time
- **Cross-platform compatibility**: Supports Windows, macOS, Linux
- **Graceful Downgrade**: use external browser when built-in browser is not available

## 🔧 Technical implementation

### Core components

```csharp
// 主窗口：TabControl 管理页面切换
MainWindow.axaml
├── TabControl
    ├── SettingsView    // 设置页面
    └── BrowserView     // 浏览器页面

// ViewModel：状态管理和命令处理
MainWindowViewModel.cs
├── 页面切换命令
├── 浏览器状态管理
├── NCF集成逻辑
└── 外部浏览器启动

// 浏览器视图：占位符实现
BrowserView.axaml
├── 导航工具栏
├── 状态显示
└── 外部浏览器集成
```

### Key Features

- **MVVM Architecture**: clear view-model separation
- **Responsive Design**: Real-time status updates and UI responsiveness
- **Cross-platform support**: unified user experience
- **Modular design**: easy to expand and maintain

## 📋 Future plans

### Short term goals (1-2 weeks)
- [ ] Solve WebViewControl-Avalonia compatibility issue
- [ ] Implement basic built-in browser functions
- [ ] Improve navigation control functions

### Mid-term goal (1 month)
- [ ] Add full WebView functionality
- [ ] Implement JavaScript interaction
- [ ] Optimize performance and user experience

### Long-term goals (3 months)
- [ ] Advanced browser features
- [ ] Developer Tools Integration
- [ ] Plugin and extension support

## 🤝 Contribute

Contributions of code and ideas are welcome! in particular:
- Alternative to WebView component
- Cross-platform compatibility improvements
- User experience optimization suggestions

---

**Updated date**: 2025-01-04
**Version**: v1.0-preview
**Status**: Function complete, WebView integration in progress