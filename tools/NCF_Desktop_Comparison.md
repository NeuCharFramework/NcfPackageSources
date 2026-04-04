# NCF desktop application version comparison

## Overview

The NCF desktop application now provides two versions: **Console version** and **GUI version** to meet different user needs and usage scenarios.

## 📊 Function comparison table

| Features | console version | GUI version | Remark |
|---------|-----------|---------|------|
| **🚀 Basic functions** |
| Automatically download NCF | ✅ | ✅ | Both support GitHub API |
| Cross-platform file extraction | ✅ | ✅ | Intelligent path processing |
| Smart port detection | ✅ | ✅ | 5001-5300 port range |
| NCF process management | ✅ | ✅ | Start/Stop/Monitor |
| health check mechanism | ✅ | ✅ | Wait for site to be ready |
| **🎨 User Interface** |
| command line interface | ✅ | ❌ | Suitable for developers |
| Graphical user interface | ❌ | ✅ | Suitable for ordinary users |
| Real-time progress display | 📝 text | 📊 Progress bar | The GUI version is more intuitive |
| Log display | 📝 Console | 📋Scroll panel | GUI version supports history viewing |
| **⚙️ Configuration Management** |
| Command line parameters | ✅ | ❌ | --test, --auto-clean, etc. |
| Configuration file | ✅ | ✅ | appsettings.json |
| Graphical interface configuration | ❌ | ✅ | Live adjustment options |
| Configure persistence | ✅ | ✅ | Both support saving settings |
| **🔧 Advanced Features** |
| test mode | ✅ | ❌ | --test parameter |
| Cross-platform testing | ✅ | ❌ | CrossPlatformTest |
| Connection test | ❌ | ✅ | GUI-specific functions |
| Configure directory access | ❌ | ✅ | Open a folder with one click |
| **🌍 Platform support** |
| Windows | ✅ | ✅ | Fully supported |
| macOS | ✅ | ✅ | Fully supported |
| Linux | ✅ | ✅ | Fully supported |
| Server environment | ✅ | ❌ | Console version is suitable for GUI-less environments |

## 🎯 Recommended usage scenarios

### 🖥️ **Console version suitable for:**

1. **Developers and Technical Users**
- Familiar with command line operations
- Needs to be automated in a script
- Server environment deployment

2. **Automation and CI/CD**
- Build script integration
- Automated testing environment
- Batch operations

3. **Resource-constrained environment**
- Minimize memory usage
- Server without graphical interface
- Docker container environment

4. **Debugging and Diagnostics**
- Use test mode to verify platform compatibility
- Quickly test connections and configurations
- Detailed command line output

### 🎨 **GUI version is suitable for:**

1. **General users and non-technical users**
- Not familiar with command line operations
- Like visual interface
- Occasionally use NCF

2. **Daily development work**
- Need to monitor status in real time
- Frequent configuration adjustments
- Visual progress tracking

3. **Demonstration and Training**
- Demonstrate NCF capabilities to customers
- Train new team members
- Rapid prototyping demonstration

4. **Desktop environment integration**
- System tray integration
- Desktop notifications
- File association

## 🚀 **Performance comparison**

| index | console version | GUI version | difference |
|------|-----------|---------|------|
| **Startup time** | ~500ms | ~2s | GUI needs more initialization |
| **Memory usage** | ~50MB | ~150MB | GUI framework overhead |
| **CPU usage** | smallest | low-medium | UI rendering requirements |
| **Network Efficiency** | same | same | Shared core logic |
| **File Operation** | same | same | SharedNcfService |

## 🔧 **Technical architecture differences**

### Console version
```
┌─────────────────┐
│   Program.cs    │  ← 单文件架构
│  ┌───────────┐  │
│  │ Core Logic │  │  ← 集成的业务逻辑
│  └───────────┘  │
│  ┌───────────┐  │
│  │ Platform  │  │  ← 平台特定代码
│  │ Detection │  │
│  └───────────┘  │
└─────────────────┘
```

### GUI version
```
┌─────────────────┐
│  MainWindow     │  ← XAML界面
├─────────────────┤
│ MainWindowVM    │  ← MVVM数据绑定
├─────────────────┤
│  NcfService     │  ← 分离的业务逻辑
├─────────────────┤
│ Platform Layer  │  ← 跨平台抽象
└─────────────────┘
```

## 📋 **Command Reference**

### Console version
```bash
# 基本运行
dotnet run

# 测试模式（跨平台兼容性测试）
dotnet run --test

# 强制自动清理
dotnet run --auto-clean

# 显示帮助
dotnet run --help
```

### GUI version
```bash
# 启动GUI应用
dotnet run

# 发布到特定平台
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r osx-arm64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
```

## 🔮 **Future Development Direction**

### Console version
- [ ] More command line parameter options
- [ ] JSON output format support
- [ ] Docker image optimization
- [ ] Profile Template

### GUI version
- [ ] System tray support
- [ ] theme switching function
- [ ] Multi-language support
- [ ] Richer visualization charts
- [ ] plug-in system

## ✅ **Select Suggestions**

### 🤔 **How ​​to choose the right version? **

**Select console version if you:**
- Be a developer or system administrator
- Need to be used in scripts or CI/CD
- Deploy in server environment
- Pay attention to resource usage efficiency
- Like command line operation

**Select GUI version if you:**
- Are a product manager, designer or business user
- Occasionally use NCF for demonstration or testing
- Love the visual interface and real-time feedback
- Requires frequent adjustment of configuration options
- Work in a desktop environment

### 💡 **Pro Tip**

1. **Mixed use**: Use the console version during development and the GUI version during demonstration
2. **Team collaboration**: The technical team uses the console version, and the business team uses the GUI version
3. **Environment distinction**: The server environment uses the console version, and local development uses the GUI version.

---

**Both versions share the same core logic and cross-platform compatibility, choose the version that best suits your workflow! ** 🎯