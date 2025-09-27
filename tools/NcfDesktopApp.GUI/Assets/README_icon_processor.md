# macOS 图标处理工具

> 高质量的 macOS 应用图标处理器，支持圆角、边距、白色背景等效果

## ✨ 功能特性

- 🍎 **符合 macOS 设计规范**：18% 圆角半径，10% 边距
- 🎨 **多种背景选项**：透明背景 / 白色背景
- 🔍 **智能检查验证**：自动检测圆角效果
- 💾 **安全备份机制**：自动备份原始文件
- ⚙️ **高度可配置**：自定义圆角、边距参数
- 🚀 **批量处理**：一次处理所有需要的图标尺寸

## 📦 安装依赖

```bash
pip install Pillow
```

## 🚀 快速开始

```bash
# 基本用法（透明背景）
python3 macos_icon_processor.py

# 白色背景版本（推荐）
python3 macos_icon_processor.py --white-background

# 仅检查圆角效果
python3 macos_icon_processor.py --check-only

# 从备份恢复
python3 macos_icon_processor.py --restore-from-backup
```

## 📋 完整参数列表

| 参数 | 描述 | 示例 |
|------|------|------|
| `--white-background` | 使用白色背景（默认透明） | `--white-background` |
| `--check-only` | 仅检查圆角效果，不处理文件 | `--check-only` |
| `--restore-from-backup` | 从备份恢复原始文件 | `--restore-from-backup` |
| `--custom-corner-radius` | 自定义圆角半径比例 (0.0-0.5) | `--custom-corner-radius 0.2` |
| `--custom-padding` | 自定义边距比例 (0.0-0.3) | `--custom-padding 0.15` |
| `--no-backup` | 不创建备份文件 | `--no-backup` |
| `--no-icns` | 不生成 .icns 文件 | `--no-icns` |
| `--quiet` | 静默模式（减少输出） | `--quiet` |
| `--base-dir` | 指定工作目录 | `--base-dir /path/to/assets` |

## 📁 处理的文件

工具会自动处理以下大于 128px 的图标文件：

- `NCF-logo.png` → `NCF-logo-rounded.png`
- `AppIcon.iconset/icon_128x128@2x.png` (256×256)
- `AppIcon.iconset/icon_256x256.png` (256×256)
- `AppIcon.iconset/icon_256x256@2x.png` (512×512)
- `AppIcon.iconset/icon_512x512.png` (512×512)
- `AppIcon.iconset/icon_512x512@2x.png` (1024×1024)

## 🔄 典型工作流程

### 1. 初次处理
```bash
# 处理图标并生成白色背景版本
python3 macos_icon_processor.py --white-background

# 检查处理结果
python3 macos_icon_processor.py --check-only
```

### 2. 调整参数
```bash
# 自定义 20% 圆角和 15% 边距
python3 macos_icon_processor.py --white-background \
    --custom-corner-radius 0.2 \
    --custom-padding 0.15
```

### 3. 恢复原始文件
```bash
# 从备份恢复
python3 macos_icon_processor.py --restore-from-backup

# 重新处理
python3 macos_icon_processor.py --white-background
```

## 📊 输出示例

```
🔄 开始处理macOS图标 (白色背景)...
ℹ️ 工作目录: /path/to/Assets
ℹ️ 备份目录: /path/to/Assets/backup_originals

📋 已备份: NCF-logo.png
✅ 处理完成: NCF-logo.png -> NCF-logo-rounded.png
📋 已备份: icon_256x256.png
✅ 处理完成: icon_256x256.png -> icon_256x256.png

ℹ️ 处理完成: 6/6 个文件成功
ℹ️ 说明:
ℹ️    - 使用白色背景匹配Logo背景色
ℹ️    - 已添加18%圆角和10%边距
ℹ️    - 图标符合macOS设计规范

🔄 生成.icns文件...
✅ AppIcon.icns生成成功
```

## 🔧 技术细节

### 处理算法
1. **图像预处理**：转换为 RGBA 模式
2. **尺寸计算**：按比例计算边距和内容区域
3. **图像缩放**：使用 Lanczos 高质量重采样
4. **背景合成**：白色背景或透明背景
5. **圆角应用**：创建圆角蒙版并应用
6. **文件保存**：PNG 格式输出

### 设计规范
- **圆角半径**：18%（符合 macOS Human Interface Guidelines）
- **边距比例**：10%（确保图标不会触及边缘）
- **背景色彩**：`#FFFFFF` 白色或完全透明
- **图像质量**：使用 Lanczos 算法保证最佳质量

## 🛠️ 故障排除

### 常见问题

**Q: 提示 "需要安装Pillow库"**
```bash
pip install Pillow
```

**Q: iconutil 命令不可用**
- 这是正常的，iconutil 仅在 macOS 系统上可用
- 在非 macOS 系统上工具会跳过 .icns 生成

**Q: 处理后的图标看起来模糊**
- 确保原始图标质量足够高
- 尝试减少边距比例：`--custom-padding 0.05`

**Q: 需要恢复原始文件**
```bash
python3 macos_icon_processor.py --restore-from-backup
```

### 退出代码
- `0`：成功
- `1`：失败或用户中断

## 📄 许可证

MIT License - 由 NCF Team 开发维护

## 🔗 相关文档

- [macOS Human Interface Guidelines](https://developer.apple.com/design/human-interface-guidelines/macos/icons-and-images/app-icon/)
- [Pillow 图像处理库](https://pillow.readthedocs.io/)






