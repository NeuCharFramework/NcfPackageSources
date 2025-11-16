# macOS 和 Linux 可执行文件使用指南

## 📖 概述

在 Unix 系统（macOS 和 Linux）上，**可执行文件通常没有扩展名**，这是完全正常的行为。与 Windows 的 `.exe` 不同，Unix 可执行文件通过文件权限而不是扩展名来标识。

---

## 🍎 macOS 使用指南

### 可执行文件说明

发布后会生成：
```
publish-self-contained/osx-arm64/
└── NcfDesktopApp.GUI-osx-arm64  ← 可执行文件（无扩展名是正常的）
```

### 方法 1：直接运行可执行文件（简单测试）

```bash
# 1. 确保文件有可执行权限
chmod +x ./publish-self-contained/osx-arm64/NcfDesktopApp.GUI-osx-arm64

# 2. 运行
./publish-self-contained/osx-arm64/NcfDesktopApp.GUI-osx-arm64
```

**⚠️ 可能遇到的问题：**
- **Gatekeeper 阻止**：macOS 可能提示"无法验证开发者"
  - 解决方法：在"系统偏好设置 > 安全性与隐私"中点击"仍要打开"
  - 或者：右键点击文件，选择"打开"
- **代码签名问题**：未签名的应用可能被阻止

### 方法 2：创建 .app 包（推荐，标准 macOS 应用）

#### 选项 A：发布时自动创建

```bash
# 使用 --create-app 参数自动创建 .app 包
./build-tool/build-all-platforms-self-contained.sh \
    --clean \
    --single-file \
    --ready-to-run \
    -p osx-arm64 \
    --create-app
```

#### 选项 B：单独创建

```bash
# 1. 先发布
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64

# 2. 再创建 .app 包
./build-tool/create-macos-app.sh
```

**生成的 .app 包位置：**
```
macos-app/
├── NCF Desktop.app           ← 标准 macOS 应用（双击即可运行）
└── NCF Desktop.dmg           ← DMG 安装包（如果使用了 --create-dmg）
```

### .app 包的优势

| 特性 | 直接可执行文件 | .app 包 |
|------|--------------|---------|
| 运行方式 | 命令行 | 双击打开 |
| Finder 集成 | ❌ | ✅ |
| Dock 图标 | ❌ | ✅ |
| 应用图标 | ❌ | ✅ |
| Gatekeeper 友好 | ⚠️ | ✅ |
| 代码签名 | ⚠️ | ✅ |
| 推荐给最终用户 | ❌ | ✅ |

### 高级：代码签名和公证（发布生产版本）

```bash
# 1. 创建签名的 .app 包
./build-tool/create-macos-app.sh --sign --identity "Developer ID Application: Your Name"

# 2. 创建签名的 DMG
./build-tool/create-macos-app.sh --create-dmg --sign --identity "Developer ID Application: Your Name"

# 3. 公证应用（需要 Apple 开发者账号）
./build-tool/create-macos-app.sh --create-dmg --sign --notarize
```

---

## 🐧 Linux 使用指南

### 可执行文件说明

发布后会生成：
```
publish-self-contained/linux-x64/
└── NcfDesktopApp.GUI-linux-x64  ← 可执行文件（无扩展名是正常的）
```

### 运行方法

```bash
# 1. 确保文件有可执行权限
chmod +x ./publish-self-contained/linux-x64/NcfDesktopApp.GUI-linux-x64

# 2. 运行
./publish-self-contained/linux-x64/NcfDesktopApp.GUI-linux-x64
```

### 创建桌面快捷方式（可选）

创建 `.desktop` 文件：

```bash
# 创建桌面文件
cat > ~/.local/share/applications/ncf-desktop.desktop << 'EOF'
[Desktop Entry]
Type=Application
Name=NCF Desktop
Comment=NCF Desktop Application
Exec=/path/to/NcfDesktopApp.GUI-linux-x64
Icon=/path/to/icon.png
Terminal=false
Categories=Development;
EOF

# 设置权限
chmod +x ~/.local/share/applications/ncf-desktop.desktop
```

### Linux 发行版特殊说明

#### Ubuntu/Debian
```bash
# 安装依赖（如果需要）
sudo apt-get update
sudo apt-get install libicu-dev libssl-dev
```

#### Fedora/RHEL/CentOS
```bash
# 安装依赖（如果需要）
sudo dnf install icu libicu-devel openssl
```

#### Arch Linux
```bash
# 安装依赖（如果需要）
sudo pacman -S icu openssl
```

---

## 🔧 常见问题

### Q1: 为什么文件没有扩展名？
**A:** Unix 系统（macOS/Linux）通过文件权限而不是扩展名来识别可执行文件。这是标准行为。

### Q2: 双击文件没有反应？
**A:** 
- **macOS**: 使用终端运行，或创建 .app 包
- **Linux**: 右键 → 属性 → 权限 → 勾选"允许作为程序执行"，然后双击

### Q3: macOS 提示"无法验证开发者"？
**A:** 
1. 右键点击文件 → 选择"打开"
2. 或：系统偏好设置 → 安全性与隐私 → 点击"仍要打开"
3. 或：使用代码签名

### Q4: 如何分发 macOS 应用？
**A:** 
1. **开发测试**: 直接分发可执行文件
2. **内部分发**: 创建 .app 包
3. **公开发布**: 创建签名的 DMG 并公证

### Q5: Linux 上提示缺少依赖？
**A:** 安装系统依赖：
```bash
# Ubuntu/Debian
sudo apt-get install libicu-dev libssl-dev

# Fedora/RHEL
sudo dnf install icu openssl
```

### Q6: 可以在 Windows 上创建 macOS .app 包吗？
**A:** 不行。创建 .app 包需要在 macOS 系统上运行，因为需要 macOS 特定的工具。

---

## 📋 快速参考

### 发布命令速查

```bash
# Windows（生成 .exe 文件）
./build-tool/build-all-platforms-self-contained.sh -p win-x64 --single-file

# macOS（生成可执行文件）
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64 --single-file

# macOS（生成 .app 包）- 推荐！
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64 --single-file --create-app

# Linux（生成可执行文件）
./build-tool/build-all-platforms-self-contained.sh -p linux-x64 --single-file

# 所有平台
./build-tool/build-all-platforms-self-contained.sh --clean --single-file --ready-to-run
```

### 运行命令速查

```bash
# Windows
.\NcfDesktopApp.GUI-win-x64.exe

# macOS（直接运行）
./NcfDesktopApp.GUI-osx-arm64

# macOS（.app 包）
open "NCF Desktop.app"

# Linux
./NcfDesktopApp.GUI-linux-x64
```

---

## 🎯 推荐工作流程

### 开发阶段
```bash
# 快速测试，所有平台
./build-tool/build-all-platforms-self-contained.sh --clean -p osx-arm64
./publish-self-contained/osx-arm64/NcfDesktopApp.GUI-osx-arm64
```

### 发布阶段（macOS）
```bash
# 1. 创建优化的单文件版本和 .app 包
./build-tool/build-all-platforms-self-contained.sh \
    --clean \
    --single-file \
    --ready-to-run \
    -p osx-arm64 \
    --create-app

# 2. 如果需要 DMG（在 macOS 上）
./build-tool/create-macos-app.sh --create-dmg

# 3. 如果需要签名和公证（生产环境）
./build-tool/create-macos-app.sh --create-dmg --sign --notarize
```

### 发布阶段（Linux）
```bash
# 创建优化的单文件版本
./build-tool/build-all-platforms-self-contained.sh \
    --clean \
    --single-file \
    --ready-to-run \
    -p linux-x64

# 打包为 tar.gz（便于分发）
cd publish-self-contained/linux-x64
tar -czf NcfDesktopApp-linux-x64.tar.gz *
```

---

## 📚 相关文档

- **构建脚本**: `build-tool/README.md`
- **macOS 应用打包**: `build-tool/create-macos-app.sh --help`
- **单文件发布修复**: `build-tool/SINGLE_FILE_FIX.md`
- **版本更新功能**: `VERSION_UPDATE_FEATURE.md`

---

## 🔗 外部资源

- [.NET 发布文档](https://docs.microsoft.com/zh-cn/dotnet/core/deploying/)
- [macOS 代码签名](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)
- [Linux 桌面文件规范](https://specifications.freedesktop.org/desktop-entry-spec/latest/)
- [Avalonia UI 文档](https://docs.avaloniaui.net/)

---

**最后更新**: 2025-11-16  
**适用版本**: NCF Desktop App v1.0.0+

