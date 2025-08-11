#!/bin/bash

# ====================================
# NCF 桌面应用 macOS 应用程序包生成工具
# ====================================

set -e  # 遇到错误时停止

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 配置
PROJECT_NAME="NcfDesktopApp.GUI"
SOLUTION_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="${SOLUTION_DIR}/publish"
OUTPUT_DIR="${SOLUTION_DIR}/macos-app"
BUILD_CONFIG="Release"

# 应用程序信息
APP_NAME="NCF Desktop"
APP_BUNDLE_ID="com.senparc.ncf.desktop"
APP_VERSION="1.0.0"
APP_COPYRIGHT="© 2025 Senparc NCF"
APP_DESCRIPTION="NCF Desktop Application"

# 函数：显示帮助信息
show_help() {
    echo -e "${BLUE}用法: $0 [选项]${NC}"
    echo ""
    echo "选项:"
    echo "  -h, --help              显示此帮助信息"
    echo "  -c, --clean             清理输出目录"
    echo "  --create-dmg            创建 DMG 安装包"
    echo "  --sign                  对应用程序进行代码签名"
    echo "  --notarize              公证应用程序 (需要Apple开发者账号)"
    echo "  --identity IDENTITY     指定代码签名身份"
    echo ""
    echo "示例:"
    echo "  $0                      # 创建基本的 .app 包"
    echo "  $0 --create-dmg         # 创建 .app 包并生成 DMG"
    echo "  $0 --sign              # 创建并签名 .app 包"
    echo "  $0 --create-dmg --sign  # 创建签名的 .app 包和 DMG"
    echo ""
    echo -e "${YELLOW}注意: 此工具需要先运行自包含发布脚本生成 macOS 可执行文件${NC}"
}

# 函数：显示横幅
show_banner() {
    echo -e "${BLUE}"
    echo "=================================================="
    echo "   NCF 桌面应用 macOS 应用程序包生成工具"
    echo "=================================================="
    echo -e "${NC}"
    echo "项目: $PROJECT_NAME"
    echo "应用名称: $APP_NAME"
    echo "Bundle ID: $APP_BUNDLE_ID"
    echo "版本: $APP_VERSION"
    echo "输出目录: $OUTPUT_DIR"
    echo ""
}

# 函数：清理输出目录
clean_output() {
    echo -e "${YELLOW}🧹 清理输出目录...${NC}"
    if [ -d "$OUTPUT_DIR" ]; then
        rm -rf "$OUTPUT_DIR"
        echo -e "${GREEN}✅ 输出目录已清理${NC}"
    else
        echo -e "${YELLOW}⚠️  输出目录不存在，跳过清理${NC}"
    fi
    echo ""
}

# 函数：检查前置条件
check_prerequisites() {
    echo -e "${BLUE}🔍 检查前置条件...${NC}"
    
    # 检查是否在macOS上运行
    if [[ "$OSTYPE" != "darwin"* ]]; then
        echo -e "${RED}❌ 此脚本只能在 macOS 系统上运行${NC}"
        exit 1
    fi
    
    # 检查自包含发布文件是否存在
    local osx_arm64_dir="$PUBLISH_DIR/osx-arm64"
    local osx_x64_dir="$PUBLISH_DIR/osx-x64"
    
    if [ ! -d "$osx_arm64_dir" ] && [ ! -d "$osx_x64_dir" ]; then
        echo -e "${RED}❌ 未找到 macOS 发布文件${NC}"
        echo -e "${YELLOW}请先运行自包含发布脚本:${NC}"
        echo -e "${YELLOW}  ./build-tool/build-all-platforms-self-contained.sh -p osx-arm64${NC}"
        echo -e "${YELLOW}  ./build-tool/build-all-platforms-self-contained.sh -p osx-x64${NC}"
        exit 1
    fi
    
    # 检查可执行文件
    if [ -d "$osx_arm64_dir" ] && [ ! -f "$osx_arm64_dir/$PROJECT_NAME" ]; then
        echo -e "${RED}❌ 未找到 macOS ARM64 可执行文件${NC}"
        exit 1
    fi
    
    if [ -d "$osx_x64_dir" ] && [ ! -f "$osx_x64_dir/$PROJECT_NAME" ]; then
        echo -e "${RED}❌ 未找到 macOS x64 可执行文件${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✅ 前置条件检查通过${NC}"
    echo ""
}

# 函数：从 PNG/ICO 生成 .icns
generate_icns_from_source() {
    local source_image=$1
    local output_icns=$2
    local workdir=$(dirname "$output_icns")/._icon_build
    rm -rf "$workdir" && mkdir -p "$workdir/AppIcon.iconset"

    # 如果 iconutil 存在，按规范生成
    if command -v iconutil &> /dev/null && command -v sips &> /dev/null; then
        local iconset="$workdir/AppIcon.iconset"
        sips -z 16 16     "$source_image" --out "$iconset/icon_16x16.png" &>/dev/null || true
        sips -z 32 32     "$source_image" --out "$iconset/icon_16x16@2x.png" &>/dev/null || true
        sips -z 32 32     "$source_image" --out "$iconset/icon_32x32.png" &>/dev/null || true
        sips -z 64 64     "$source_image" --out "$iconset/icon_32x32@2x.png" &>/dev/null || true
        sips -z 128 128   "$source_image" --out "$iconset/icon_128x128.png" &>/dev/null || true
        sips -z 256 256   "$source_image" --out "$iconset/icon_128x128@2x.png" &>/dev/null || true
        sips -z 256 256   "$source_image" --out "$iconset/icon_256x256.png" &>/dev/null || true
        sips -z 512 512   "$source_image" --out "$iconset/icon_256x256@2x.png" &>/dev/null || true
        sips -z 512 512   "$source_image" --out "$iconset/icon_512x512.png" &>/dev/null || true
        sips -z 1024 1024 "$source_image" --out "$iconset/icon_512x512@2x.png" &>/dev/null || true
        iconutil -c icns "$iconset" -o "$output_icns" &>/dev/null || true
    fi

    # 回退方案：尝试使用 sips 直接转换
    if [ ! -f "$output_icns" ] && command -v sips &> /dev/null; then
        sips -s format icns "$source_image" --out "$output_icns" &>/dev/null || true
    fi

    # 再回退：如果仍未生成，拷贝源文件（部分系统会识别 png 作为 icns，但不保证）
    if [ ! -f "$output_icns" ] && [ -f "$source_image" ]; then
        cp -f "$source_image" "$output_icns" 2>/dev/null || true
    fi
}

# 尝试在未安装 SetFile 的情况下，用 xattr 设置自定义图标标志位
set_custom_icon_flag() {
    local target_dir=$1
    if ! command -v xattr &> /dev/null; then
        return 1
    fi
    # 读取当前 FinderInfo（32 字节，64 个十六进制字符）
    local finfo_hex=$(xattr -px com.apple.FinderInfo "$target_dir" 2>/dev/null | tr -d ' \n')
    if [ -z "$finfo_hex" ] || [ ${#finfo_hex} -lt 64 ]; then
        finfo_hex="0000000000000000000000000000000000000000000000000000000000000000"
    fi
    # flags 位于第 9-10 个字节（从 0 开始的字节 8-9），每字节 2 位 hex，因此起始下标 16，长度 4
    local flags_hex=${finfo_hex:16:4}
    # 解析为整数并置上 kHasCustomIcon (0x0400)
    local flags=$((16#${flags_hex}))
    flags=$((flags | 0x0400))
    local new_flags_hex=$(printf "%04X" $flags)
    local new_hex="${finfo_hex:0:16}${new_flags_hex}${finfo_hex:20}"
    # 写回 FinderInfo（以十六进制形式）
    xattr -wx com.apple.FinderInfo "$new_hex" "$target_dir" >/dev/null 2>&1 || true
}

# 函数：创建应用程序包结构
create_app_bundle() {
    local arch=$1
    local source_dir="$PUBLISH_DIR/$arch"
    local app_bundle="$OUTPUT_DIR/$APP_NAME-$arch.app"
    
    echo -e "${BLUE}📦 创建 $arch 应用程序包...${NC}"
    
    # 创建 .app 目录结构
    mkdir -p "$app_bundle/Contents/MacOS"
    mkdir -p "$app_bundle/Contents/Resources"
    mkdir -p "$app_bundle/Contents/Frameworks"
    
    # 复制可执行文件和依赖
    echo -e "${YELLOW}  📋 复制应用程序文件...${NC}"
    cp -R "$source_dir"/* "$app_bundle/Contents/MacOS/"
    
    # 重命名主可执行文件为应用名称
    mv "$app_bundle/Contents/MacOS/$PROJECT_NAME" "$app_bundle/Contents/MacOS/$APP_NAME"
    
    # 创建 Info.plist
    create_info_plist "$app_bundle" "$arch"
    
    # 复制图标（如果存在）
    copy_app_icon "$app_bundle"
    
    # 设置可执行权限
    chmod +x "$app_bundle/Contents/MacOS/$APP_NAME"
    
    # 移除隔离属性并签名（如果需要）
    process_app_bundle "$app_bundle"
    
    echo -e "${GREEN}✅ $arch 应用程序包创建完成${NC}"
    echo ""
}

# 函数：创建 Info.plist
create_info_plist() {
    local app_bundle=$1
    local arch=$2
    local plist_file="$app_bundle/Contents/Info.plist"
    
    echo -e "${YELLOW}  📄 创建 Info.plist...${NC}"
    
    cat > "$plist_file" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$APP_BUNDLE_ID</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$APP_VERSION</string>
    <key>CFBundleVersion</key>
    <string>$APP_VERSION</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>$APP_COPYRIGHT</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.developer-tools</string>
    <key>NSAppTransportSecurity</key>
    <dict>
        <key>NSAllowsArbitraryLoads</key>
        <true/>
    </dict>
    <key>LSRequiresNativeExecution</key>
    <true/>
</dict>
</plist>
EOF
}

# 函数：复制应用图标
copy_app_icon() {
    local app_bundle=$1
    # Prefer Assets/NCF-logo.png, then project root NCF-logo.png. Fallback to legacy Avalonia icon if missing
    local icon_source="$SOLUTION_DIR/Assets/NCF-logo.png"
    if [ ! -f "$icon_source" ]; then
        icon_source="$SOLUTION_DIR/NCF-logo.png"
    fi
    if [ ! -f "$icon_source" ]; then
        icon_source="$SOLUTION_DIR/Assets/avalonia-logo.ico"
    fi
    
    if [ -f "$icon_source" ]; then
        echo -e "${YELLOW}  🎨 处理应用图标...${NC}"
        mkdir -p "$app_bundle/Contents/Resources"
        generate_icns_from_source "$icon_source" "$app_bundle/Contents/Resources/AppIcon.icns"
        if [ -f "$app_bundle/Contents/Resources/AppIcon.icns" ]; then
            echo -e "${GREEN}  ✅ 已生成 AppIcon.icns${NC}"
        else
            echo -e "${YELLOW}  ⚠️  图标转换失败，应用将使用默认图标${NC}"
        fi
    else
        echo -e "${YELLOW}  ⚠️  未找到应用图标文件${NC}"
    fi
}

# 函数：处理应用程序包（签名和权限）
process_app_bundle() {
    local app_bundle=$1
    
    echo -e "${YELLOW}  🔧 处理应用程序包...${NC}"
    
    # 递归移除隔离属性
    find "$app_bundle" -type f -exec xattr -d com.apple.quarantine {} \; 2>/dev/null || true
    
    # 如果需要签名
    if [ "$SIGN_APP" = true ]; then
        sign_app_bundle "$app_bundle"
    else
        # 执行 ad-hoc 签名
        codesign --force --deep --sign - "$app_bundle" 2>/dev/null || {
            echo -e "${YELLOW}  ⚠️  Ad-hoc 签名失败，但应用程序仍可运行${NC}"
        }
    fi
}

# 函数：代码签名
sign_app_bundle() {
    local app_bundle=$1
    
    echo -e "${YELLOW}  ✍️  对应用程序进行代码签名...${NC}"
    
    if [ -n "$SIGNING_IDENTITY" ]; then
        # 使用指定的签名身份
        if codesign --force --deep --sign "$SIGNING_IDENTITY" "$app_bundle"; then
            echo -e "${GREEN}  ✅ 代码签名成功${NC}"
        else
            echo -e "${RED}  ❌ 代码签名失败${NC}"
            exit 1
        fi
    else
        # 查找可用的签名身份
        local identities=$(security find-identity -v -p codesigning | grep "Developer ID Application" | head -1 | cut -d '"' -f 2)
        
        if [ -n "$identities" ]; then
            echo -e "${BLUE}  🔍 找到签名身份: $identities${NC}"
            if codesign --force --deep --sign "$identities" "$app_bundle"; then
                echo -e "${GREEN}  ✅ 代码签名成功${NC}"
            else
                echo -e "${RED}  ❌ 代码签名失败${NC}"
                exit 1
            fi
        else
            echo -e "${YELLOW}  ⚠️  未找到有效的签名身份，执行 ad-hoc 签名${NC}"
            codesign --force --deep --sign - "$app_bundle"
        fi
    fi
}

# 函数：创建通用二进制文件
create_universal_app() {
    local arm64_app="$OUTPUT_DIR/$APP_NAME-osx-arm64.app"
    local x64_app="$OUTPUT_DIR/$APP_NAME-osx-x64.app"
    local universal_app="$OUTPUT_DIR/$APP_NAME-Universal.app"
    
    if [ -d "$arm64_app" ] && [ -d "$x64_app" ]; then
        echo -e "${BLUE}🔄 创建通用二进制文件应用程序包...${NC}"
        
        # 复制 ARM64 版本作为基础
        cp -R "$arm64_app" "$universal_app"
        
        # 使用 lipo 创建通用二进制文件
        lipo -create \
            "$arm64_app/Contents/MacOS/$APP_NAME" \
            "$x64_app/Contents/MacOS/$APP_NAME" \
            -output "$universal_app/Contents/MacOS/$APP_NAME"
        
        # 重新签名
        if [ "$SIGN_APP" = true ]; then
            sign_app_bundle "$universal_app"
        else
            codesign --force --deep --sign - "$universal_app" 2>/dev/null || true
        fi
        
        echo -e "${GREEN}✅ 通用二进制文件应用程序包创建完成${NC}"
        echo ""
    else
        echo -e "${YELLOW}⚠️  需要同时存在 ARM64 和 x64 版本才能创建通用包${NC}"
    fi
}

# 函数：创建 DMG
create_dmg() {
    echo -e "${BLUE}💿 创建 DMG 安装包...${NC}"
    
    local dmg_temp_dir="$OUTPUT_DIR/dmg-temp"
    local dmg_file="$OUTPUT_DIR/$APP_NAME-$APP_VERSION.dmg"
    
    # 清理临时目录
    rm -rf "$dmg_temp_dir"
    mkdir -p "$dmg_temp_dir"
    
    # 决定使用哪个应用程序包
    local app_to_package=""
    if [ -d "$OUTPUT_DIR/$APP_NAME-Universal.app" ]; then
        app_to_package="$OUTPUT_DIR/$APP_NAME-Universal.app"
        echo -e "${BLUE}  📦 使用通用二进制版本${NC}"
    elif [ -d "$OUTPUT_DIR/$APP_NAME-osx-arm64.app" ]; then
        app_to_package="$OUTPUT_DIR/$APP_NAME-osx-arm64.app"
        echo -e "${BLUE}  📦 使用 ARM64 版本${NC}"
    elif [ -d "$OUTPUT_DIR/$APP_NAME-osx-x64.app" ]; then
        app_to_package="$OUTPUT_DIR/$APP_NAME-osx-x64.app"
        echo -e "${BLUE}  📦 使用 x64 版本${NC}"
    else
        echo -e "${RED}❌ 未找到可用的应用程序包${NC}"
        return 1
    fi
    
    # 复制应用程序包到临时目录
    cp -R "$app_to_package" "$dmg_temp_dir/$APP_NAME.app"
    
    # 创建应用程序文件夹的符号链接
    ln -s /Applications "$dmg_temp_dir/Applications"

    # 设置卷图标（使 .dmg 文件显示自定义图标）
    # 优先使用已生成的 VolumeIcon.icns，备用 AppIcon.icns
    echo -e "${YELLOW}  🎨 设置 DMG 卷图标...${NC}"
    local volume_icon_source="$SOLUTION_DIR/Assets/VolumeIcon.icns"
    if [ ! -f "$volume_icon_source" ]; then
        volume_icon_source="$SOLUTION_DIR/Assets/AppIcon.icns"
    fi
    
    if [ -f "$volume_icon_source" ]; then
        # 复制图标到临时目录
        cp -f "$volume_icon_source" "$dmg_temp_dir/.VolumeIcon.icns" 2>/dev/null || true
        echo -e "${GREEN}  ✅ 已设置卷图标: $(basename "$volume_icon_source")${NC}"
    else
        echo -e "${YELLOW}  ⚠️  未找到卷图标文件，DMG 将使用默认图标${NC}"
    fi
    
    # 创建 .DS_Store 文件以设置窗口布局（可选）
    create_dmg_layout "$dmg_temp_dir"
    
    # 创建 DMG（先创建可写 DMG，设置卷图标，再压缩）
    if command -v hdiutil &> /dev/null; then
        echo -e "${YELLOW}  🔄 生成 DMG 文件...${NC}"
        local rw_dmg="$dmg_temp_dir/pack-temp.dmg"
        [ -f "$rw_dmg" ] && rm -f "$rw_dmg"
        [ -f "$dmg_file" ] && rm -f "$dmg_file"

        # 先创建可写 DMG
        hdiutil create -srcfolder "$dmg_temp_dir" -volname "$APP_NAME" -fs HFS+ -format UDRW -ov "$rw_dmg" >/dev/null

        # 挂载设置卷图标
        local mount_point="$OUTPUT_DIR/_dmg_mount"
        mkdir -p "$mount_point"
        if hdiutil attach -readwrite -noverify -noautoopen -mountpoint "$mount_point" "$rw_dmg" >/dev/null; then
            # 拷贝卷图标到根目录（已命名为 .VolumeIcon.icns）
            if [ -f "$dmg_temp_dir/.VolumeIcon.icns" ]; then
                cp -f "$dmg_temp_dir/.VolumeIcon.icns" "$mount_point/.VolumeIcon.icns" 2>/dev/null || true
            fi
            # 标记自定义图标
            if command -v SetFile &> /dev/null; then
                SetFile -a C "$mount_point" 2>/dev/null || true
            else
                set_custom_icon_flag "$mount_point" || echo -e "${YELLOW}  ⚠️  未找到 SetFile，已尝试使用 xattr 设置自定义图标标志${NC}"
            fi
            hdiutil detach "$mount_point" -quiet || true
            rmdir "$mount_point" 2>/dev/null || true
        fi

        # 转换为压缩 DMG
        hdiutil convert "$rw_dmg" -format UDZO -imagekey zlib-level=9 -o "$dmg_file" >/dev/null
        rm -f "$rw_dmg"

        # 清理临时目录
        rm -rf "$dmg_temp_dir"
        
        # 为 DMG 文件本身设置图标
        if [ -f "$dmg_file" ] && [ -f "$volume_icon_source" ]; then
            echo -e "${YELLOW}  🎨 为 DMG 文件设置图标...${NC}"
            # 使用 Rez（如果可用）或 DeRez/Rez 为 DMG 文件设置图标
            if command -v sips &> /dev/null && command -v DeRez &> /dev/null && command -v Rez &> /dev/null; then
                # 方法1：使用 macOS 开发工具
                local icon_rsrc="$(dirname "$dmg_file")/temp_icon.rsrc"
                sips -i "$volume_icon_source" >/dev/null 2>&1 || true
                DeRez -only icns "$volume_icon_source" > "$icon_rsrc" 2>/dev/null || true
                if [ -f "$icon_rsrc" ]; then
                    Rez -append "$icon_rsrc" -o "$dmg_file" >/dev/null 2>&1 || true
                    rm -f "$icon_rsrc"
                fi
                # 设置自定义图标标志
                if command -v SetFile &> /dev/null; then
                    SetFile -a C "$dmg_file" 2>/dev/null || true
                fi
            fi
            
            # 方法2：使用 osascript（AppleScript）作为备用方案
            if [ ! -f "$dmg_file" ] || ! xattr -l "$dmg_file" 2>/dev/null | grep -q "com.apple.FinderInfo"; then
                osascript - <<EOF >/dev/null 2>&1 || true
tell application "Finder"
    set dmgFile to POSIX file "$dmg_file" as alias
    set iconFile to POSIX file "$volume_icon_source" as alias
    set icon of dmgFile to (read iconFile as «class icns»)
end tell
EOF
            fi
            echo -e "${GREEN}  ✅ DMG 文件图标设置完成${NC}"
        fi
        
        if [ -f "$dmg_file" ]; then
            local dmg_size=$(ls -lh "$dmg_file" | awk '{print $5}')
            echo -e "${GREEN}✅ DMG 创建成功: $APP_NAME-$APP_VERSION.dmg ($dmg_size)${NC}"
        else
            echo -e "${RED}❌ DMG 创建失败${NC}"
            return 1
        fi
    else
        echo -e "${RED}❌ 未找到 hdiutil 工具${NC}"
        return 1
    fi
    echo ""
}

# 函数：创建DMG布局
create_dmg_layout() {
    local dmg_dir=$1
    
    # 创建隐藏的背景图片目录（可选）
    # mkdir -p "$dmg_dir/.background"
    
    # 这里可以添加自定义的背景图片和窗口布局
    # 目前使用默认布局
}

# 函数：显示总结
show_summary() {
    echo -e "${BLUE}📊 应用程序包生成总结${NC}"
    echo "======================================"
    
    if [ -d "$OUTPUT_DIR" ]; then
        echo -e "${BLUE}📁 输出位置: $OUTPUT_DIR${NC}"
        echo ""
        
        # 列出生成的文件
        for file in "$OUTPUT_DIR"/*.app; do
            if [ -d "$file" ]; then
                local app_name=$(basename "$file")
                local app_size=$(du -sh "$file" | cut -f1)
                echo -e "${GREEN}✅ $app_name ($app_size)${NC}"
            fi
        done
        
        # 列出 DMG 文件
        for file in "$OUTPUT_DIR"/*.dmg; do
            if [ -f "$file" ]; then
                local dmg_name=$(basename "$file")
                local dmg_size=$(ls -lh "$file" | awk '{print $5}')
                echo -e "${GREEN}✅ $dmg_name ($dmg_size)${NC}"
            fi
        done
        
        echo ""
        echo -e "${YELLOW}💡 使用说明:${NC}"
        echo -e "${YELLOW}  • 双击 .app 文件直接运行应用程序${NC}"
        echo -e "${YELLOW}  • 双击 .dmg 文件安装应用程序${NC}"
        echo -e "${YELLOW}  • 首次运行可能需要在系统偏好设置中允许${NC}"
        echo -e "${YELLOW}  • 如果出现权限问题，右键点击选择"打开"${NC}"
    else
        echo -e "${RED}❌ 未找到输出文件${NC}"
    fi
    echo ""
}

# 解析命令行参数
CLEAN=false
CREATE_DMG=false
SIGN_APP=false
NOTARIZE_APP=false
SIGNING_IDENTITY=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -c|--clean)
            CLEAN=true
            shift
            ;;
        --create-dmg)
            CREATE_DMG=true
            shift
            ;;
        --sign)
            SIGN_APP=true
            shift
            ;;
        --notarize)
            NOTARIZE_APP=true
            SIGN_APP=true  # 公证需要先签名
            shift
            ;;
        --identity)
            SIGNING_IDENTITY="$2"
            SIGN_APP=true
            shift 2
            ;;
        *)
            echo -e "${RED}未知选项: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# 主程序开始
START_TIME=$(date +%s)

show_banner
check_prerequisites

if [ "$CLEAN" = true ]; then
    clean_output
fi

# 创建输出目录
mkdir -p "$OUTPUT_DIR"

# 创建应用程序包
if [ -d "$PUBLISH_DIR/osx-arm64" ]; then
    create_app_bundle "osx-arm64"
fi

if [ -d "$PUBLISH_DIR/osx-x64" ]; then
    create_app_bundle "osx-x64"
fi

# 创建通用二进制文件（如果两个架构都存在）
create_universal_app

# 创建 DMG（如果请求）
if [ "$CREATE_DMG" = true ]; then
    create_dmg
fi

# 显示总结
END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

show_summary

echo -e "${BLUE}⏱️  总耗时: ${DURATION}秒${NC}"
echo -e "${GREEN}🎉 macOS 应用程序包生成完成！${NC}"

# 提供下一步说明
echo ""
echo -e "${BLUE}📝 下一步操作建议:${NC}"
echo -e "${YELLOW}1. 测试应用程序包: 双击 .app 文件${NC}"
echo -e "${YELLOW}2. 如需分发: 使用 --create-dmg 选项${NC}"
echo -e "${YELLOW}3. 如需签名: 使用 --sign 选项${NC}"
echo -e "${YELLOW}4. 首次运行遇到安全提示时，请到"系统偏好设置 > 安全性与隐私"中允许${NC}"
