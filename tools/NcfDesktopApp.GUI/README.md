[中文版](README.cn.md)

# NCF Desktop App - GUI version

## 🖥️ Overview

A graphical user interface (GUI) version of the NCF desktop application that provides an intuitive, easy-to-use interface to manage the download, deployment, and operation of NeuChar Framework sites.

## ✨ Features

### 🎨 **Modern interface**
- Based on Avalonia UI framework, supports Windows, macOS, Linux
- Responsive design, supports window scaling
- Card layout, clear information hierarchy
- Modern colors and font design

### 📊 **Real-time status monitoring**
- **Running platform information**: Automatically detect the current operating system and architecture
- **Version Information**: Shows the latest available version and the currently installed version
- **Running Status**: Displays the current status of the application in real time
- **Site Address**: Displays the access address of the NCF site

### 📈 **Visualization of operation progress**
- **Progress Bar**: Show download and installation progress
- **Detailed Log**: Display operation log in real time
- **Status Tip**: clear status color indication

### ⚙️ **Flexible configuration options**
- **Auto-open browser**: Automatically open the browser after NCF starts
- **Automatically clean downloaded files**: You can choose whether to automatically delete downloaded ZIP files
- **Detailed information display**: Control the detail level of the log
- **System Tray Minimization**: Support minimizing to the system tray
- **Port range configuration**: Customize port scanning range

### 🔄 **Intelligent Operation Management**
- **One-click start/stop**: Simple operation interface
- **Connection Test**: Test network connection and GitHub API availability
- **Configuration Directory Access**: Quickly open the application configuration directory
- **Operation Cancel**: Supports canceling ongoing operations

## 🚀 **How to use**

### Start the application```bash
cd tools/NcfDesktopApp.GUI
dotnet run
```### Basic operation process

1. **Launch the application**
   - The application automatically detects platform information
   - Get the latest version information

2. **Configuration Options** (optional)
   - Adjust settings in the "Configuration Options" card
   - Set port ranges, browser options, and more

3. **Start NCF**
   - Click the "Start NCF" button
   - The application automatically:
     - Download the latest version (if needed)
     - Extract files (if needed)
     - Find available ports
     - Start NCF site
     - Wait for the site to be ready

4. **Visit site**
   - After the site is successfully launched, the access address will be displayed
   - If auto-open browser is enabled, the site will be opened automatically

5. **Stop NCF**
   - Click the "Stop NCF" button to stop the site

### Accessibility

- **Test Connection**: Verify network connection and GitHub API availability
- **Open Configuration Directory**: Quick access to the application data directory
- **Operation Log**: View detailed operation process

## 🎨 **Interface Preview**

### Main interface layout```
┌─────────────────────────────────────────┐
│              NCF 桌面应用程序              │
│        自动下载并运行最新的 NCF 站点        │
├─────────────────────────────────────────┤
│  状态信息                                │
│  ┌─────────────────────────────────────┐ │
│  │ 运行平台: macOS ARM64               │ │
│  │ 最新版本: v0.30.3-build9245        │ │
│  │ 当前状态: 运行中                    │ │
│  │ 站点地址: http://localhost:5001     │ │
│  └─────────────────────────────────────┘ │
│                                         │
│  操作进度                                │
│  ┌─────────────────────────────────────┐ │
│  │ NCF 运行中                          │ │
│  │ ████████████████████████████████    │ │
│  │                                     │ │
│  │ 操作日志:                           │ │
│  │ ┌─────────────────────────────────┐ │ │
│  │ │ [10:30:15] ✅ NCF 启动成功      │ │ │
│  │ │ [10:30:12] 🌐 站点已启动       │ │ │
│  │ └─────────────────────────────────┘ │ │
│  └─────────────────────────────────────┘ │
│                                         │
│  配置选项                                │
│  ┌─────────────────────────────────────┐ │
│  │ ☑ 自动打开浏览器                    │ │
│  │ ☐ 自动清理下载文件                  │ │
│  │ ☑ 检查更新时显示详细信息            │ │
│  │ ☐ 最小化到系统托盘                  │ │
│  │                                     │ │
│  │ 端口范围: [5001] 到 [5300]         │ │
│  └─────────────────────────────────────┘ │
├─────────────────────────────────────────┤
│        [测试连接] [打开配置目录] [停止 NCF] │
└─────────────────────────────────────────┘
```## 🔧 **Technical Architecture**

### Front-end framework
- **Avalonia UI 11.3.2**: Cross-platform UI framework
- **CommunityToolkit.Mvvm**: MVVM mode support

### Core Services
- **NcfService**: core business logic service
- **GitHub API integration**: automatically get the latest version
- **Cross-platform file management**: Intelligent path handling
- **Port Management**: Automatic port detection and allocation

### Data binding
- **ObservableProperty**: automatic property notification
- **AsyncRelayCommand**: Asynchronous command support
- **Two-way Binding**: Configuration options automatically saved

## 🌍 **Cross-platform support**

| Platform | Status | Special Features |
|------|------|----------|
| **Windows** | ✅ Fully supported | Shell integration, registry support |
| **macOS** | ✅ Fully supported | Dock integration, notification center |
| **Linux** | ✅ Fully supported | Desktop environment integration |

## 📋 **System Requirements**

- **.NET 8.0 Runtime**
- **OS**: Windows 10+, macOS 10.15+, Linux (modern distributions)
- **RAM**: At least 512MB available memory
- **Disk Space**: At least 1GB free space (for NCF files)
- **Network Connection**: used to download NCF package

## 🔧 **Develop and Build**

### Development environment```bash
# 安装依赖
dotnet restore

# 运行开发版本
dotnet run

# 构建Release版本
dotnet build -c Release
```### Platform specific releases```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained true

# macOS
dotnet publish -c Release -r osx-x64 --self-contained true
dotnet publish -c Release -r osx-arm64 --self-contained true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained true
```## 🐛 **Troubleshooting**

### FAQ

1. **Application cannot be started**
   - Make sure .NET 8.0 Runtime is installed
   - Check system compatibility

2. **Unable to obtain version information**
   - Check network connection
   - Verify GitHub API access

3. **NCF site startup failed**
   - Check whether the port is occupied
   - Make sure you have sufficient system permissions

4. **Abnormal interface display**
   - Update graphics card driver
   - Check system DPI settings

### Logs and Diagnostics
- Application logs are saved in the configuration directory
- Use the "Test Connection" feature to diagnose network problems
- View live log output for detailed status

## 📝 **Update Log**

### v1.0.0
- ✨ Initial GUI version released
- 🎨 Modern interface design
- 🔄 Complete NCF life cycle management
- ⚙️ Flexible configuration options
- 🌍 Cross-platform support

---

## 📞 **Support and Feedback**

If you encounter problems or have suggestions for improvements, please:

1. Check the log for detailed error information
2. Diagnose the problem using the "Test Connection" function
3. Report an issue in GitHub Issues
4. Provide complete system information and error logs

**Enjoy a more convenient NCF development experience! ** 🚀
