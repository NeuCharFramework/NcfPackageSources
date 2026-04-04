# 🍎 macOS application package generation guide

## 🎯 Solve the problem

If you encounter the following issues on macOS:
- ❌ Cannot double-click to run`NcfDesktopApp.GUI`
- ❌ "zsh: killed" error occurs when running
- ❌ Want to create a distributable DMG installation package

This guide will provide you with the complete solution!

## 🚀 Quick Start

### Step 1: Build a self-contained executable

```bash
# 构建 macOS ARM64 版本（Apple Silicon）
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64

# 构建 macOS x64 版本（Intel）
./build-tool/build-all-platforms-self-contained.sh -p osx-x64
```

### Step 2: Create a macOS application package

```bash
# 创建 .app 包并生成 DMG 安装包
./build-tool/create-macos-app.sh --create-dmg --clean
```

### Step 3: Install and use

1. open`macos-app/`Table of contents
2. Double-click`NCF Desktop-1.0.0.dmg`document
3. Drag the application to the Applications folder
4. Double-click the application icon to run

## 📋 Detailed function description

### Automatic macOS optimization

The new automatic processing function will automatically when downloading and decompressing NCF:

- ✅ Set correct executable permissions
- ✅ Remove macOS isolation attribute
- ✅ Perform ad-hoc code signing
- ✅ Verify signature validity

This means you no longer need to manually run`chmod +x`Or deal with permission issues!

### Application Package Properties

generated`.app`The package has the following features:

- 🖱️ **Double-click to run**: Double-click to launch like other macOS apps
- 📱 **Standard Icons**: Show app icons in Dock and Finder
- 🔒 **Security Signing**: Avoid security warnings with code signing
- 🌐 **Universally Compatible**: Supports Intel and Apple Silicon Mac
- 📦 **Professional Distribution**: Distribute via DMG installation package

## 🛠️ Advanced usage

### Only create .app package (do not generate DMG)

```bash
./build-tool/create-macos-app.sh
```

### Use developer signature

```bash
./build-tool/create-macos-app.sh --sign --create-dmg
```

### Specify signing identity

```bash
./build-tool/create-macos-app.sh --identity "Developer ID Application: Your Name" --create-dmg
```

### Clean and regenerate

```bash
./build-tool/create-macos-app.sh --clean --create-dmg
```

## 📁 Output file structure

After the run is complete, you will be in`macos-app/`Seen in the directory:

```
macos-app/
├── NCF Desktop-osx-arm64.app     # ARM64 专用版本
├── NCF Desktop-osx-x64.app       # Intel 专用版本  
├── NCF Desktop-Universal.app     # 通用版本（推荐）
└── NCF Desktop-1.0.0.dmg         # DMG 安装包
```

**Recommended use**:
- **Personal use**: Double-click directly`NCF Desktop-Universal.app`
- **Distribute to Others**: Use`NCF Desktop-1.0.0.dmg`

## 🔧 Troubleshooting

### Encountered security prompts when running for the first time

If you encounter "Cannot open because it is from an unidentified developer" on first run:

1. Right-click the application icon
2. Select "Open"
3. Click "Open" in the pop-up dialog box

or:

1. Open "System Preferences" > "Security & Privacy"
2. Click "Open Anyway" in the "General" tab

### Signature verification failed

If signature verification fails, you can re-sign manually:

```bash
codesign --force --deep --sign - "macos-app/NCF Desktop-Universal.app"
```

### DMG creation failed

Make sure there is enough disk space and that no other programs are using the files in question.

## 🎉 Summary

With this complete solution, you can now:

1. ✅ **Double-click to run** NCF desktop application
2. ✅ **Automatically handle** macOS permissions and signature issues
3. ✅ **Create professional** DMG installation package
4. ✅ **Distribute to others** No technical background required for installation experience

Enjoy your macOS NCF desktop app experience! 🚀
