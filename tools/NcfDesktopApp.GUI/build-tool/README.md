# NCF 桌面应用多平台发布工具

## 🚀 概述

此工具集提供了为 NCF 桌面应用构建多个平台版本的自动化脚本。支持 Windows、macOS 和 Linux 的 x64 和 ARM64 架构。

## 📁 文件说明

| 文件 | 平台 | 描述 |
|------|------|------|
| `build-all-platforms.sh` | Unix/Linux/macOS | Bash shell 脚本 |
| `build-all-platforms.bat` | Windows | Windows 批处理文件 |
| `build-all-platforms.ps1` | 跨平台 | PowerShell 脚本 |

## 🎯 支持的平台

- **Windows x64** (`win-x64`)
- **Windows ARM64** (`win-arm64`) 
- **macOS Intel** (`osx-x64`)
- **macOS Apple Silicon** (`osx-arm64`)
- **Linux x64** (`linux-x64`)
- **Linux ARM64** (`linux-arm64`)

## 💻 使用方法

### Unix/Linux/macOS (Bash)

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

发布完成后，所有平台的文件将保存在 `publish` 文件夹中：

```
publish/
├── win-x64/           # Windows x64 版本
├── win-arm64/         # Windows ARM64 版本
├── osx-x64/           # macOS Intel 版本
├── osx-arm64/         # macOS Apple Silicon 版本
├── linux-x64/         # Linux x64 版本
└── linux-arm64/       # Linux ARM64 版本
```

每个平台文件夹包含：
- 主程序可执行文件
- 必要的 .NET 库文件
- 应用程序资源文件
- 配置文件

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
   ```

4. **PowerShell 执行策略错误**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
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

**提示**: 建议在首次使用时先运行单个平台测试，确认环境配置正确后再进行全平台构建。