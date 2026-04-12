# 🍎 macOS 应用程序包生成指南

## 🎯 解决问题

如果您在 macOS 上遇到以下问题：
- ❌ 无法双击运行 `NcfDesktopApp.GUI`
- ❌ 运行时出现 "zsh: killed" 错误
- ❌ 希望创建可分发的 DMG 安装包

本指南将为您提供完整的解决方案！

## 🚀 快速开始

### 步骤 1：构建自包含可执行文件

```bash
# 构建 macOS ARM64 版本（Apple Silicon）
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64

# 构建 macOS x64 版本（Intel）
./build-tool/build-all-platforms-self-contained.sh -p osx-x64
```

### 步骤 2：创建 macOS 应用程序包

```bash
# 创建 .app 包并生成 DMG 安装包
./build-tool/create-macos-app.sh --create-dmg --clean
```

### 步骤 3：安装和使用

1. 打开 `macos-app/` 目录
2. 双击 `NCF Desktop-1.0.0.dmg` 文件
3. 将应用程序拖拽到 Applications 文件夹
4. 双击应用程序图标运行

## 📋 详细功能说明

### 自动 macOS 优化

新增的自动处理功能会在 NCF 下载解压时自动：

- ✅ 设置正确的可执行权限
- ✅ 移除 macOS 隔离属性
- ✅ 执行 ad-hoc 代码签名
- ✅ 验证签名有效性

这意味着您不再需要手动运行 `chmod +x` 或处理权限问题！

### 应用程序包特性

生成的 `.app` 包具有以下特性：

- 🖱️ **双击运行**：像其他 macOS 应用一样双击启动
- 📱 **标准图标**：在 Dock 和 Finder 中显示应用图标
- 🔒 **安全签名**：通过代码签名避免安全警告
- 🌐 **通用兼容**：支持 Intel 和 Apple Silicon Mac
- 📦 **专业分发**：通过 DMG 安装包进行分发

## 🛠️ 高级用法

### 只创建 .app 包（不生成 DMG）

```bash
./build-tool/create-macos-app.sh
```

### 使用开发者签名

```bash
./build-tool/create-macos-app.sh --sign --create-dmg
```

### 指定签名身份

```bash
./build-tool/create-macos-app.sh --identity "Developer ID Application: Your Name" --create-dmg
```

### 清理重新生成

```bash
./build-tool/create-macos-app.sh --clean --create-dmg
```

## 📁 输出文件结构

运行完成后，您将在 `macos-app/` 目录中看到：

```
macos-app/
├── NCF Desktop-osx-arm64.app     # ARM64 专用版本
├── NCF Desktop-osx-x64.app       # Intel 专用版本  
├── NCF Desktop-Universal.app     # 通用版本（推荐）
└── NCF Desktop-1.0.0.dmg         # DMG 安装包
```

**推荐使用**：
- **个人使用**：直接双击 `NCF Desktop-Universal.app`
- **分发给他人**：使用 `NCF Desktop-1.0.0.dmg`

## 🔧 故障排除

### 首次运行遇到安全提示

如果首次运行时遇到"无法打开，因为它来自身份不明的开发者"：

1. 右键点击应用程序图标
2. 选择"打开"
3. 在弹出的对话框中点击"打开"

或者：

1. 打开"系统偏好设置" > "安全性与隐私"
2. 在"通用"标签页中点击"仍要打开"

### 签名验证失败

如果签名验证失败，可以手动重新签名：

```bash
codesign --force --deep --sign - "macos-app/NCF Desktop-Universal.app"
```

### DMG 创建失败

确保有足够的磁盘空间，并且没有其他程序正在使用相关文件。

## 🎉 总结

通过这套完整的解决方案，您现在可以：

1. ✅ **双击运行** NCF 桌面应用
2. ✅ **自动处理** macOS 权限和签名问题  
3. ✅ **创建专业** 的 DMG 安装包
4. ✅ **分发给他人** 无需技术背景的安装体验

享受您的 macOS NCF 桌面应用体验！ 🚀
