# NCF 桌面应用程序

> 🚀 一个跨平台的桌面应用程序，自动下载并运行最新版本的 NCF (NeuChar Framework) 站点。

## ✨ 功能特性

- 🔍 **智能平台检测** - 自动检测当前操作系统和架构
- 📥 **自动下载更新** - 从 GitHub Releases 自动下载最新版本
- 🎯 **精确匹配** - 根据系统平台自动选择对应的发布包
- 📦 **自动解压安装** - 智能解压并配置 NCF 运行环境
- 🌐 **一键启动** - 自动启动 NCF 站点并打开浏览器
- 💾 **版本管理** - 智能检测版本变化，只在需要时下载
- 🛡️ **安全存储** - 文件存储在用户的本地应用数据目录

## 🖥️ 支持的平台

| 操作系统 | 架构 | 发布包名称 |
|---------|------|-----------|
| Windows | x64 | `ncf-win-x64-*.zip` |
| Windows | ARM64 | `ncf-win-arm64-*.zip` |
| macOS | x64 (Intel) | `ncf-osx-x64-*.zip` |
| macOS | ARM64 (Apple Silicon M1/M2/M3/M4) | `ncf-osx-arm64-*.zip` |
| Linux | x64 | `ncf-linux-x64-*.zip` |
| Linux | ARM64 | `ncf-linux-arm64-*.zip` |

## 📋 系统要求

- **.NET 8.0 Runtime** 或更高版本
- **Internet 连接** (首次下载时需要)
- **可用磁盘空间** 约 100MB (用于 NCF 站点文件)

### Windows 用户
- Windows 10 版本 1809 或更高版本
- Windows 11 (推荐)

### macOS 用户
- macOS 10.15 (Catalina) 或更高版本
- macOS 12.0 (Monterey) 或更高版本 (推荐)

### Linux 用户
- Ubuntu 18.04+ / CentOS 7+ / Debian 9+ 或其他主流 Linux 发行版
- 支持 glibc 2.17 或更高版本

## 🚀 快速开始

### 开发环境运行

1. **克隆项目并进入目录**
   ```bash
   cd tools/NcfDesktopApp
   ```

2. **还原依赖包**
   ```bash
   dotnet restore
   ```

3. **运行应用程序**
   ```bash
   dotnet run
   ```

### 发布独立可执行文件

#### Windows x64
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

#### Windows ARM64
```bash
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true
```

#### macOS x64 (Intel Mac)
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

#### macOS ARM64 (Apple Silicon)
```bash
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

#### Linux x64
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

#### Linux ARM64
```bash
dotnet publish -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true
```

发布后的可执行文件将位于 `bin/Release/net8.0/{runtime}/publish/` 目录中。

## 📁 文件结构

应用程序会在用户的本地应用数据目录创建以下结构：

```
📁 %LocalAppData%/NcfDesktopApp (Windows)
📁 ~/.local/share/NcfDesktopApp (Linux)  
📁 ~/Library/Application Support/NcfDesktopApp (macOS)
├── 📁 Runtime/           # NCF 站点运行时文件
│   └── 📁 Senparc.Web/   # NCF 主站点目录
├── 📁 Downloads/         # 临时下载目录
└── 📄 version.txt        # 当前版本信息
```

## 🔧 使用说明

### 首次运行

1. **启动应用程序**
   - 应用会自动检测当前系统平台
   - 从 GitHub 获取最新的 NCF 版本信息
   - 下载适合当前平台的发布包
   - 自动解压并配置运行环境

2. **自动启动 NCF 站点**
   - 解压完成后，应用会自动启动 NCF 站点
   - 默认运行在 `http://localhost:5000`
   - 自动打开默认浏览器

### 后续运行

- 应用会检查本地版本与最新版本
- 如果有新版本，会自动下载更新
- 如果已是最新版本，直接启动现有的 NCF 站点

### 退出应用

- 在控制台窗口按任意键即可退出应用程序
- 退出后 NCF 站点进程也会停止

## 🛠️ 技术架构

- **主框架**: .NET 8.0
- **依赖注入**: Microsoft.Extensions.Hosting
- **日志记录**: Microsoft.Extensions.Logging
- **HTTP 客户端**: HttpClient (GitHub API 调用)
- **文件压缩**: System.IO.Compression
- **跨平台支持**: System.Runtime.InteropServices
- **JSON 序列化**: System.Text.Json

## 🐛 故障排除

### 常见问题

1. **"无法检测到 .NET Runtime"**
   - 确保已安装 .NET 8.0 Runtime
   - 下载地址: https://dotnet.microsoft.com/download

2. **"下载失败"**
   - 检查网络连接
   - 确认可以访问 GitHub
   - 检查防火墙设置

3. **"端口 5000 被占用"**
   - 关闭占用端口 5000 的其他应用程序
   - 或修改 NCF 配置使用其他端口

4. **macOS "应用程序已损坏"错误**
   ```bash
   xattr -d com.apple.quarantine NcfDesktopApp
   ```

5. **Linux 权限问题**
   ```bash
   chmod +x NcfDesktopApp
   ```

### 日志查看

应用程序使用控制台日志输出，运行时会显示详细的操作步骤和错误信息。

## 🔐 安全说明

- 应用程序只从官方 GitHub Releases 下载文件
- 所有文件存储在用户的本地应用数据目录
- 不会修改系统文件或注册表
- 不会收集或发送用户数据

## 📄 许可证

本项目遵循与 NCF 项目相同的许可证。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request 来改进这个应用程序！

## 📞 支持

如有问题，请在 [NCF GitHub 项目](https://github.com/NeuCharFramework/NCF) 中创建 Issue。 