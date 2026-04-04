#NCF desktop application multi-platform publishing tool

## 🚀 Overview

This toolset provides automated scripts for building multiple platform versions of NCF desktop applications. Supports x64 and ARM64 architectures for Windows, macOS, and Linux.

## Common commands

### Build a single file self-contained version for all platforms

```bash
./build-tool/build-all-platforms-self-contained.sh --clean --single-file --ready-to-run
```

### MacOS single file self-contained version
```bash
./build-tool/create-macos-app.sh --create-dmg --clean
```

### 🍎 macOS special support

**NEW FEATURE**: Provide a double-click run solution specifically for macOS users!

- ✅ **Double click to run**: Create standards`.app`application package
- ✅ **DMG Installation Package**: Generate professional macOS installation package
- ✅ **Automatic signature processing**: Solve permission and security prompt issues
- ✅ **Universal Binary**: Supports both Intel and Apple Silicon Macs

If you encounter this on macOS:
- ❌ Unable to double-click to run executable file
- ❌ "zsh: killed" or permission error
- ❌ Need to create a distributable installation package

Please jump directly to the [🍎 macOS Special Features](#-macos-Special Features) section!

## 📁 File description

| document | platform | describe |
|------|------|------|
| `build-all-platforms.sh` | Unix/Linux/macOS | Bash shell script |
| `build-all-platforms.bat` | Windows | Windows batch file |
| `build-all-platforms.ps1` | Cross-platform | PowerShell script |
| `create-macos-app.sh` | macOS | macOS application package generation tool |

## 🎯 Supported platforms

- **Windows x64** (`win-x64`)
- **Windows ARM64** (`win-arm64`) 
- **macOS Intel** (`osx-x64`)
- **macOS Apple Silicon** (`osx-arm64`)
- **Linux x64** (`linux-x64`)
- **Linux ARM64** (`linux-arm64`)

## 💻 How to use

### 🍎 macOS Quick Start (double-click to run the solution)

**If you need to double-click to run an application on macOS**, follow these steps:

```bash
# 步骤 1：构建 macOS 可执行文件
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64    # Apple Silicon
./build-tool/build-all-platforms-self-contained.sh -p osx-x64     # Intel Mac

# 步骤 2：创建 .app 应用程序包和 DMG 安装包
./build-tool/create-macos-app.sh --create-dmg --clean

# 步骤 3：使用生成的文件
# - 双击 macos-app/NCF Desktop-Universal.app 直接运行
# - 双击 macos-app/NCF Desktop-1.0.0.dmg 进行安装
```

**One line of command to complete all operations**:
```bash
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64 && ./build-tool/build-all-platforms-self-contained.sh -p osx-x64 && ./build-tool/create-macos-app.sh --create-dmg --clean
```

### Self-contained publishing script series (recommended when the target machine does not have the .NET runtime installed)

- Bash: `build-tool/build-all-platforms-self-contained.sh`
- PowerShell: `build-tool/build-all-platforms-self-contained.ps1`
- Batch: `build-tool/build-all-platforms-self-contained.bat`

Example:

```bash
# Bash（macOS/Linux）
./build-tool/build-all-platforms-self-contained.sh --clean --single-file
./build-tool/build-all-platforms-self-contained.sh --platform win-x64
```

```powershell
# PowerShell（跨平台）
./build-tool/build-all-platforms-self-contained.ps1 -Clean -SingleFile
./build-tool/build-all-platforms-self-contained.ps1 -Platform osx-arm64
```

```cmd
REM Windows 批处理
build-tool\build-all-platforms-self-contained.bat --clean --single-file
build-tool\build-all-platforms-self-contained.bat --platform linux-x64
```

Note: The above self-contained script always uses`--self-contained true`Released to facilitate running on devices where dotnet-runtime is not installed.

### Ordinary release script (framework dependency)

```bash
# 发布所有平台
./build-tool/build-all-platforms.sh

# 清理并发布所有平台
./build-tool/build-all-platforms.sh --clean

# 只发布特定平台
./build-tool/build-all-platforms.sh --platform win-x64

# 创建自包含版本（包含 .NET 运行时）
./build-tool/build-all-platforms.sh --self-contained

# 创建单文件版本
./build-tool/build-all-platforms.sh --single-file

# 查看帮助
./build-tool/build-all-platforms.sh --help
```

### Windows (batch)

```cmd
REM 发布所有平台
build-tool\build-all-platforms.bat

REM 清理并发布所有平台
build-tool\build-all-platforms.bat /c

REM 只发布特定平台
build-tool\build-all-platforms.bat /p win-x64

REM 创建自包含版本
build-tool\build-all-platforms.bat --self-contained

REM 查看帮助
build-tool\build-all-platforms.bat /h
```

### PowerShell (cross-platform)

```powershell
# 发布所有平台
.\build-tool\build-all-platforms.ps1

# 清理并发布所有平台
.\build-tool\build-all-platforms.ps1 -Clean

# 只发布特定平台
.\build-tool\build-all-platforms.ps1 -Platform win-x64

# 创建自包含版本
.\build-tool\build-all-platforms.ps1 -SelfContained

# 创建单文件版本
.\build-tool\build-all-platforms.ps1 -SingleFile

# 详细输出
.\build-tool\build-all-platforms.ps1 -Verbose

# 查看帮助
.\build-tool\build-all-platforms.ps1 -Help
```

## ⚙️ Parameter description

### Common parameters

| parameter | Bash | Batch | PowerShell | describe |
|------|------|-------|------------|------|
| help | `--help` | `/h` | `-Help` | Show help information |
| clean up | `--clean` | `/c` | `-Clean` | Clean output directory before publishing |
| platform specific | `--platform <name>` | `/p <name>` | `-Platform <name>` | Only publish on designated platforms |
| self contained | `--self-contained` | `--self-contained` | `-SelfContained` | Contains .NET runtime |
| single file | `--single-file` | `--single-file` | `-SingleFile` | Create a single-file executable program |
| Skip restore | `--no-restore` | `--no-restore` | `-NoRestore` | Skip NuGet package restore |
| Verbose output | - | - | `-Verbose` | Show detailed build information |

## 📦 Output structure

After publishing is complete, the self-contained version file will be saved in`publish-self-contained`In the folder:

```
publish-self-contained/
├── win-x64/
├── win-arm64/
├── osx-x64/
├── osx-arm64/
├── linux-x64/
└── linux-arm64/
```

The normal (framework dependent) version is still saved in`publish`folder.

### 🍎 macOS application package output

use`create-macos-app.sh`The generated macOS application package is saved in`macos-app/`Folder:

```
macos-app/
├── NCF Desktop-osx-arm64.app     # ARM64 专用版本（Apple Silicon）
├── NCF Desktop-osx-x64.app       # x64 专用版本（Intel Mac）
├── NCF Desktop-Universal.app     # 通用版本（推荐使用）
└── NCF Desktop-1.0.0.dmg         # DMG 安装包（用于分发）
```

**Usage Suggestions**:
- **Personal use**: Double-click directly`NCF Desktop-Universal.app`
- **Distribute to Others**: Use`NCF Desktop-1.0.0.dmg`
- **Architecture specific**: use the corresponding ARM64 or x64 version

## 🔧 System Requirements

- **.NET 8.0 SDK** or higher
- **Sufficient disk space** (approximately 50-100 MB per platform)
- **Network Connection** (for NuGet package restore)

### Platform specific requirements

- **Windows**: Windows 10+ or ​​Windows Server 2016+
- **macOS**: macOS 10.15+ (Catalina)
- **Linux**: Modern Linux distribution, supports glibc 2.17+

## 🚨 Troubleshooting

### FAQ

1. **dotnet command not found**
- Make sure .NET 8.0 SDK is installed
- Check if the PATH environment variable contains dotnet

2. **Platform-specific build failed**
- Check if the target platform is supported
- Make sure the network connection is OK (for downloading the package)

3. **Permission Error (Linux/macOS)**
   ```bash
   chmod +x build-tool/build-all-platforms.sh
   chmod +x build-tool/create-macos-app.sh
   ```

4. **PowerShell execution policy error**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

### 🍎 macOS special problem solving

5. **macOS cannot run when double-clicked**
   ```bash
# Use the application package generation tool
   ./build-tool/create-macos-app.sh --create-dmg
   ```

6. **"zsh: killed" error**
- The new version has automatically solved this problem
- If you still encounter the problem, please update to the latest code and rebuild

7. **macOS security prompt "Cannot be opened because it is from an unidentified developer"**
- Right click on the application and select "Open"
- Or go to "System Preferences" > "Security & Privacy" > "General" to allow

8. **DMG creation failed**
- Make sure there is enough disk space
- Check if other programs are using related files
- Clean up temporary files and try again

9. **Application package signing issue**
   ```bash
# Manual re-signing (ad-hoc signing)
   codesign --force --deep --sign - "macos-app/NCF Desktop-Universal.app"
   
# Sign using a developer certificate (requires Apple developer account)
   codesign --force --deep --sign "Developer ID Application: Your Name" \
       "macos-app/NCF Desktop-Universal.app"
   ```
   
**Detailed Configuration Guide**: Please refer to [Apple Developer Account Configuration Guide](./APPLE_DEVELOPER_SETUP.md)

### Security warning

There may be errors during the build process regarding`System.Text.Json`Security warning for packages. This is a known issue and does not affect the normal operation of the application.

## 📈 Performance optimization

### Self-contained vs framework dependency

- **Framework dependency** (default): smaller file size, requires the target machine to have the .NET runtime installed
- **Self-Contained**: Larger file size, but works on machines without .NET installed

### Single file publishing

use`--single-file`Parameters can package the application into a single executable file, but:
- Startup time may be slightly longer
- Some reflex functions may be limited

## 🔍 Verification Release

After publishing is complete, it can be verified by:

1. **Check file structure**: Confirm that each platform folder contains necessary files
2. **Run the application**: Test the executable file on the corresponding platform
3. **View log**: Check the output information of the build script

## 📞Technical Support

If you encounter problems:

1. Check the build log for error messages
2. Confirm that the system meets the minimum requirements
3. Verify that the project configuration is correct
4. Check out the .NET official documentation for more help

---

## 🍎 macOS special features

### macOS application package generation tool

For the special needs of the macOS platform, a special application package generation tool is provided:

**Prerequisite**: Please run the self-contained release script first
```bash
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64    # Apple Silicon
./build-tool/build-all-platforms-self-contained.sh -p osx-x64     # Intel Mac
```

**Application package generation**:
```bash
# 基本使用：创建 .app 包
./build-tool/create-macos-app.sh

# 清理并创建应用程序包
./build-tool/create-macos-app.sh --clean

# 创建 .app 包并生成 DMG 安装包（推荐）
./build-tool/create-macos-app.sh --create-dmg

# 创建并签名应用程序包
./build-tool/create-macos-app.sh --sign

# 完整流程：清理、创建、签名、生成DMG
./build-tool/create-macos-app.sh --clean --sign --create-dmg

# 查看所有选项
./build-tool/create-macos-app.sh --help
```

**📘 Need to configure an Apple developer account? ** Please view the [Apple Developer Account Configuration Guide](./APPLE_DEVELOPER_SETUP.md)

### macOS application package features

- ✅ **Double-click to run**: Generate standard`.app`package, supports double-click startup
- ✅ **DMG Installation Package**: Create a professional macOS installation package
- ✅ **Code Signing**: Automatically handle ad-hoc signatures and support developer signatures
- ✅ **Universal Binary**: Automatically create universal packages supporting Intel and Apple Silicon
- ✅ **Permission handling**: Automatically set execution permissions and remove isolation attributes
- ✅ **Icon Conversion**: Automatically convert ICO icons to macOS format

### Usage process

1. **Build executable file**:
   ```bash
   ./build-tool/build-all-platforms-self-contained.sh -p osx-arm64
   ./build-tool/build-all-platforms-self-contained.sh -p osx-x64
   ```

2. **Create application package**:
   ```bash
   ./build-tool/create-macos-app.sh --create-dmg
   ```

3. **Installation and use**:
- double click`.dmg`File open installer
- Drag and drop the application to the Applications folder
- Double click the application icon to run

### Output file description

The generated file is saved in`macos-app/`Table of contents:

```
macos-app/
├── NCF Desktop-osx-arm64.app     # ARM64 版本应用程序包
├── NCF Desktop-osx-x64.app       # Intel 版本应用程序包
├── NCF Desktop-Universal.app     # 通用二进制版本（推荐）
└── NCF Desktop-1.0.0.dmg         # DMG 安装包
```

### Automated macOS processing

Starting with this release, the NCF desktop app adds automatic macOS processing capabilities:

- 🔧 **Automatic permission setting**: Automatically set executable permissions when decompressing
- 🛡️ **Isolation attribute removal**: Avoid Gatekeeper blocking startup
- ✍️ **Ad-hoc Signing**: Automatically perform code signing to avoid "corrupted" prompts
- 📋 **Signature Verification**: Make sure the application can run properly

---

## 🎯 Complete example: macOS application packaging

The following is the complete process from source code to double-click running application on macOS:

```bash
# 1. 克隆或下载项目（如果还没有）
cd /path/to/NcfDesktopApp.GUI

# 2. 赋予脚本执行权限
chmod +x build-tool/build-all-platforms-self-contained.sh
chmod +x build-tool/create-macos-app.sh

# 3. 构建 macOS 可执行文件
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64    # Apple Silicon
./build-tool/build-all-platforms-self-contained.sh -p osx-x64     # Intel Mac

# 4. 创建应用程序包和 DMG
./build-tool/create-macos-app.sh --create-dmg --clean

# 5. 查看生成的文件
ls -la macos-app/
# 输出：
# NCF Desktop-osx-arm64.app     # ARM64 版本
# NCF Desktop-osx-x64.app       # Intel 版本  
# NCF Desktop-Universal.app     # 通用版本（推荐）
# NCF Desktop-1.0.0.dmg         # DMG 安装包

# 6. 测试运行（任选其一）
open "macos-app/NCF Desktop-Universal.app"              # 命令行打开
# 或直接在 Finder 中双击 "NCF Desktop-Universal.app"
```

### Distribution suggestions

- **Personal use**: Use directly`NCF Desktop-Universal.app`
- **Distribute to Others**: Use`NCF Desktop-1.0.0.dmg`, the recipient only needs to double-click to install

---

**Tip**: It is recommended to run a single platform test when using it for the first time, and then build the full platform after confirming that the environment configuration is correct.

> If you want to execute a Bash script, please grant executable permissions first:`chmod +x build-tool/build-all-platforms-self-contained.sh`