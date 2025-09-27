# NCF 桌面应用多平台发布工具

## 🚀 概述

此工具集提供了为 NCF 桌面应用构建多个平台版本的自动化脚本。支持 Windows、macOS 和 Linux 的 x64 和 ARM64 架构。

## 常用命令

### 构建所有平台单一文件自包含版本

```bash
./build-tool/build-all-platforms-self-contained.sh --clean --single-file --ready-to-run
```

### MacOS 单一文件自包含版本
```bash
./build-tool/create-macos-app.sh --create-dmg --clean
```

### 🍎 macOS 特别支持

**新增功能**：专为 macOS 用户提供双击运行解决方案！

- ✅ **双击运行**：创建标准 `.app` 应用程序包
- ✅ **DMG 安装包**：生成专业的 macOS 安装包
- ✅ **自动签名处理**：解决权限和安全提示问题
- ✅ **通用二进制**：同时支持 Intel 和 Apple Silicon Mac

如果您在 macOS 上遇到：
- ❌ 无法双击运行可执行文件
- ❌ "zsh: killed" 或权限错误
- ❌ 需要创建可分发的安装包

请直接跳转到 [🍎 macOS 专项功能](#-macos-专项功能) 部分！

## 📁 文件说明

| 文件 | 平台 | 描述 |
|------|------|------|
| `build-all-platforms.sh` | Unix/Linux/macOS | Bash shell 脚本 |
| `build-all-platforms.bat` | Windows | Windows 批处理文件 |
| `build-all-platforms.ps1` | 跨平台 | PowerShell 脚本 |
| `create-macos-app.sh` | macOS | macOS 应用程序包生成工具 |

## 🎯 支持的平台

- **Windows x64** (`win-x64`)
- **Windows ARM64** (`win-arm64`) 
- **macOS Intel** (`osx-x64`)
- **macOS Apple Silicon** (`osx-arm64`)
- **Linux x64** (`linux-x64`)
- **Linux ARM64** (`linux-arm64`)

## 💻 使用方法

### 🍎 macOS 快速开始（双击运行解决方案）

**如果您需要在 macOS 上双击运行应用程序**，请按以下步骤操作：

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

**一行命令完成所有操作**：
```bash
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64 && ./build-tool/build-all-platforms-self-contained.sh -p osx-x64 && ./build-tool/create-macos-app.sh --create-dmg --clean
```

### 自包含发布脚本系列（推荐在目标机器未安装 .NET 运行时时使用）

- Bash: `build-tool/build-all-platforms-self-contained.sh`
- PowerShell: `build-tool/build-all-platforms-self-contained.ps1`
- Batch: `build-tool/build-all-platforms-self-contained.bat`

示例：

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

说明：上述自包含脚本始终使用 `--self-contained true` 发布，便于在未安装 dotnet-runtime 的设备上运行。

### 普通发布脚本（框架依赖）

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

### Windows (批处理)

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

### PowerShell (跨平台)

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

## ⚙️ 参数说明

### 通用参数

| 参数 | Bash | Batch | PowerShell | 描述 |
|------|------|-------|------------|------|
| 帮助 | `--help` | `/h` | `-Help` | 显示帮助信息 |
| 清理 | `--clean` | `/c` | `-Clean` | 发布前清理输出目录 |
| 特定平台 | `--platform <名称>` | `/p <名称>` | `-Platform <名称>` | 只发布指定平台 |
| 自包含 | `--self-contained` | `--self-contained` | `-SelfContained` | 包含 .NET 运行时 |
| 单文件 | `--single-file` | `--single-file` | `-SingleFile` | 创建单文件可执行程序 |
| 跳过还原 | `--no-restore` | `--no-restore` | `-NoRestore` | 跳过 NuGet 包还原 |
| 详细输出 | - | - | `-Verbose` | 显示详细构建信息 |

## 📦 输出结构

发布完成后，自包含版本文件将保存在 `publish-self-contained` 文件夹中：

```
publish-self-contained/
├── win-x64/
├── win-arm64/
├── osx-x64/
├── osx-arm64/
├── linux-x64/
└── linux-arm64/
```

普通（框架依赖）版本仍保存在 `publish` 文件夹。

### 🍎 macOS 应用程序包输出

使用 `create-macos-app.sh` 生成的 macOS 应用程序包保存在 `macos-app/` 文件夹：

```
macos-app/
├── NCF Desktop-osx-arm64.app     # ARM64 专用版本（Apple Silicon）
├── NCF Desktop-osx-x64.app       # x64 专用版本（Intel Mac）
├── NCF Desktop-Universal.app     # 通用版本（推荐使用）
└── NCF Desktop-1.0.0.dmg         # DMG 安装包（用于分发）
```

**使用建议**：
- **个人使用**：直接双击 `NCF Desktop-Universal.app` 
- **分发给他人**：使用 `NCF Desktop-1.0.0.dmg`
- **特定架构**：使用对应的 ARM64 或 x64 版本

## 🔧 系统要求

- **.NET 8.0 SDK** 或更高版本
- **足够的磁盘空间**（每个平台约 50-100 MB）
- **网络连接**（用于 NuGet 包还原）

### 平台特定要求

- **Windows**: Windows 10+ 或 Windows Server 2016+
- **macOS**: macOS 10.15+ (Catalina)
- **Linux**: 现代 Linux 发行版，支持 glibc 2.17+

## 🚨 故障排除

### 常见问题

1. **找不到 dotnet 命令**
   - 确保已安装 .NET 8.0 SDK
   - 检查 PATH 环境变量是否包含 dotnet

2. **特定平台构建失败**
   - 检查目标平台是否受支持
   - 确保网络连接正常（用于下载包）

3. **权限错误 (Linux/macOS)**
   ```bash
   chmod +x build-tool/build-all-platforms.sh
   chmod +x build-tool/create-macos-app.sh
   ```

4. **PowerShell 执行策略错误**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

### 🍎 macOS 专项问题解决

5. **macOS 双击无法运行**
   ```bash
   # 使用应用程序包生成工具
   ./build-tool/create-macos-app.sh --create-dmg
   ```

6. **"zsh: killed" 错误**
   - 新版本已自动解决此问题
   - 如仍遇到，请更新到最新代码并重新构建

7. **macOS 安全提示"无法打开，因为来自身份不明的开发者"**
   - 右键点击应用程序，选择"打开"
   - 或到"系统偏好设置" > "安全性与隐私" > "通用"中允许

8. **DMG 创建失败**
   - 确保有足够磁盘空间
   - 检查是否有其他程序正在使用相关文件
   - 清理临时文件后重试

9. **应用程序包签名问题**
   ```bash
   # 手动重新签名
   codesign --force --deep --sign - "macos-app/NCF Desktop-Universal.app"
   ```

### 安全警告

构建过程中可能出现关于 `System.Text.Json` 包的安全警告。这是已知问题，不影响应用程序的正常运行。

## 📈 性能优化

### 自包含 vs 框架依赖

- **框架依赖** (默认): 文件更小，需要目标机器安装 .NET 运行时
- **自包含**: 文件更大，但可在未安装 .NET 的机器上运行

### 单文件发布

使用 `--single-file` 参数可以将应用程序打包为单个可执行文件，但：
- 启动时间可能略长
- 某些反射功能可能受限

## 🔍 验证发布

发布完成后，可以通过以下方式验证：

1. **检查文件结构**：确认每个平台文件夹都包含必要文件
2. **运行应用程序**：在对应平台上测试可执行文件
3. **查看日志**：检查构建脚本的输出信息

## 📞 技术支持

如果遇到问题：

1. 检查构建日志中的错误信息
2. 确认系统满足最低要求
3. 验证项目配置是否正确
4. 查看 .NET 官方文档获取更多帮助

---

## 🍎 macOS 专项功能

### macOS 应用程序包生成工具

针对 macOS 平台的特殊需求，提供了专门的应用程序包生成工具：

**前置条件**：请先运行自包含发布脚本
```bash
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64    # Apple Silicon
./build-tool/build-all-platforms-self-contained.sh -p osx-x64     # Intel Mac
```

**应用程序包生成**：
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

### macOS 应用程序包特性

- ✅ **双击运行**：生成标准的 `.app` 包，支持双击启动
- ✅ **DMG 安装包**：创建专业的 macOS 安装包
- ✅ **代码签名**：自动处理 ad-hoc 签名，支持开发者签名
- ✅ **通用二进制**：自动创建支持 Intel 和 Apple Silicon 的通用包
- ✅ **权限处理**：自动设置执行权限和移除隔离属性
- ✅ **图标转换**：自动将 ICO 图标转换为 macOS 格式

### 使用流程

1. **构建可执行文件**：
   ```bash
   ./build-tool/build-all-platforms-self-contained.sh -p osx-arm64
   ./build-tool/build-all-platforms-self-contained.sh -p osx-x64
   ```

2. **创建应用程序包**：
   ```bash
   ./build-tool/create-macos-app.sh --create-dmg
   ```

3. **安装和使用**：
   - 双击 `.dmg` 文件打开安装器
   - 将应用程序拖拽到 Applications 文件夹
   - 双击应用程序图标运行

### 输出文件说明

生成的文件保存在 `macos-app/` 目录：

```
macos-app/
├── NCF Desktop-osx-arm64.app     # ARM64 版本应用程序包
├── NCF Desktop-osx-x64.app       # Intel 版本应用程序包
├── NCF Desktop-Universal.app     # 通用二进制版本（推荐）
└── NCF Desktop-1.0.0.dmg         # DMG 安装包
```

### 自动化 macOS 处理

从此版本开始，NCF 桌面应用增加了自动 macOS 处理功能：

- 🔧 **自动权限设置**：解压时自动设置可执行权限
- 🛡️ **隔离属性移除**：避免 Gatekeeper 阻止启动
- ✍️ **Ad-hoc 签名**：自动执行代码签名避免"已损坏"提示
- 📋 **签名验证**：确保应用程序可以正常运行

---

## 🎯 完整示例：macOS 应用程序打包

以下是在 macOS 上从源码到可双击运行应用程序的完整流程：

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

### 分发建议

- **个人使用**：直接使用 `NCF Desktop-Universal.app`
- **分发给他人**：使用 `NCF Desktop-1.0.0.dmg`，接收者只需双击安装即可

---

**提示**: 建议在首次使用时先运行单个平台测试，确认环境配置正确后再进行全平台构建。

> 如需执行 Bash 脚本，请先赋予可执行权限：`chmod +x build-tool/build-all-platforms-self-contained.sh`