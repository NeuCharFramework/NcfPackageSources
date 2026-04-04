# Guide to using macOS and Linux executables

## 📖 Overview

On Unix systems (macOS and Linux), executable files typically do not have an extension, which is completely normal behavior. with Windows`.exe`Unlike Unix executable files, they are identified by file permissions rather than by extension.

---

## 🍎 macOS User Guide

### Executable file description

After publishing it will generate:
```
publish-self-contained/osx-arm64/
└── NcfDesktopApp.GUI-osx-arm64  ← 可执行文件（无扩展名是正常的）
```

### Method 1: Run the executable file directly (simple test)

```bash
# 1. 确保文件有可执行权限
chmod +x ./publish-self-contained/osx-arm64/NcfDesktopApp.GUI-osx-arm64

# 2. 运行
./publish-self-contained/osx-arm64/NcfDesktopApp.GUI-osx-arm64
```

**⚠️Possible problems:**
- **Gatekeeper Block**: macOS may prompt "Unable to verify developer"
- Workaround: Click "Open Anyway" in "System Preferences > Security & Privacy"
- Or: Right-click the file and select "Open"
- **Code Signing Issue**: Unsigned apps may be blocked

### Method 2: Create a .app package (recommended, standard macOS app)

#### Option A: Create automatically when publishing

```bash
# 使用 --create-app 参数自动创建 .app 包
./build-tool/build-all-platforms-self-contained.sh \
    --clean \
    --single-file \
    --ready-to-run \
    -p osx-arm64 \
    --create-app
```

#### Option B: Create separately

```bash
# 1. 先发布
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64

# 2. 再创建 .app 包
./build-tool/create-macos-app.sh
```

**Generated .app package location:**
```
macos-app/
├── NCF Desktop.app           ← 标准 macOS 应用（双击即可运行）
└── NCF Desktop.dmg           ← DMG 安装包（如果使用了 --create-dmg）
```

### Advantages of .app package

| characteristic | Direct executable file | .app package |
|------|--------------|---------|
| Operation mode | command line | Double click to open |
| Finder integration | ❌ | ✅ |
| dock icon | ❌ | ✅ |
| application icon | ❌ | ✅ |
| Gatekeeper friendly | ⚠️ | ✅ |
| code signing | ⚠️ | ✅ |
| Recommended to end users | ❌ | ✅ |

### Advanced: Code Signing and Notarization (Release to Production)

```bash
# 1. 创建签名的 .app 包
./build-tool/create-macos-app.sh --sign --identity "Developer ID Application: Your Name"

# 2. 创建签名的 DMG
./build-tool/create-macos-app.sh --create-dmg --sign --identity "Developer ID Application: Your Name"

# 3. 公证应用（需要 Apple 开发者账号）
./build-tool/create-macos-app.sh --create-dmg --sign --notarize
```

---

## 🐧 Linux User Guide

### Executable file description

After publishing it will generate:
```
publish-self-contained/linux-x64/
└── NcfDesktopApp.GUI-linux-x64  ← 可执行文件（无扩展名是正常的）
```

### Run method

```bash
# 1. 确保文件有可执行权限
chmod +x ./publish-self-contained/linux-x64/NcfDesktopApp.GUI-linux-x64

# 2. 运行
./publish-self-contained/linux-x64/NcfDesktopApp.GUI-linux-x64
```

### Create desktop shortcut (optional)

create`.desktop`document:

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

### Special instructions for Linux distributions

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

## 🔧 FAQ

### Q1: Why does the file have no extension?
**A:** Unix systems (macOS/Linux) identify executable files by file permissions rather than by extension. This is standard behavior.

### Q2: There is no response when double-clicking a file?
**A:** 
- **macOS**: Run using Terminal, or create .app package
- **Linux**: Right click → Properties → Permissions → Check "Allow execution as a program", then double-click

### Q3: macOS prompts "Unable to verify developer"?
**A:** 
1. Right-click the file → select "Open"
2. Or: System Preferences → Security & Privacy → Click "Open Anyway"
3. Or: Use code signing

### Q4: How to distribute macOS applications?
**A:** 
1. **Development Test**: Distribute executable files directly
2. **Internal Distribution**: Create .app package
3. **Public Release**: Create signed DMG and notarize it

### Q5: Is there a missing dependency prompt on Linux?
**A:** Install system dependencies:
```bash
# Ubuntu/Debian
sudo apt-get install libicu-dev libssl-dev

# Fedora/RHEL
sudo dnf install icu openssl
```

### Q6: Can I create a macOS .app package on Windows?
**A:** No. Creating .app packages requires running on a macOS system as macOS-specific tools are required.

---

## 📋 Quick Reference

### Release command quick check

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

### Run command quick check

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

## 🎯 Recommended workflow

### Development stage
```bash
# 快速测试，所有平台
./build-tool/build-all-platforms-self-contained.sh --clean -p osx-arm64
./publish-self-contained/osx-arm64/NcfDesktopApp.GUI-osx-arm64
```

### Release phase (macOS)
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

### Release phase (Linux)
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

## 📚 Related documents

- **Build Script**:`build-tool/README.md`
- **macOS App Packaging**:`build-tool/create-macos-app.sh --help`
- **Single file publishing fix**:`build-tool/SINGLE_FILE_FIX.md`
- **Version update function**:`VERSION_UPDATE_FEATURE.md`

---

## 🔗 External resources

- [.NET Publishing Documentation](https://docs.microsoft.com/zh-cn/dotnet/core/deploying/)
- [macOS Code Signing](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)
- [Linux Desktop File Specification](https://specifications.freedesktop.org/desktop-entry-spec/latest/)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)

---

**Last updated**: 2025-11-16
**Applicable version**: NCF Desktop App v1.0.0+

