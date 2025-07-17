#!/bin/bash

# NCF Desktop App Build Script
# 用于构建跨平台的 NCF 桌面应用程序

echo "🚀 NCF Desktop App 构建脚本"
echo "=============================="

# 检查 .NET SDK 是否安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到 .NET SDK"
    echo "请访问 https://dotnet.microsoft.com/download 下载并安装 .NET 8.0 SDK"
    exit 1
fi

# 获取 .NET 版本
DOTNET_VERSION=$(dotnet --version)
echo "✅ 检测到 .NET SDK 版本: $DOTNET_VERSION"

# 清理之前的构建
echo "🧹 清理之前的构建..."
rm -rf bin obj
dotnet clean > /dev/null 2>&1

# 恢复依赖包
echo "📦 恢复 NuGet 包..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ 恢复包失败"
    exit 1
fi

# 构建配置
BUILD_CONFIG="Release"
OUTPUT_DIR="./publish"

# 清理输出目录
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# 定义支持的平台
declare -a PLATFORMS=(
    "win-x64:Windows x64"
    "win-arm64:Windows ARM64" 
    "osx-x64:macOS x64 (Intel)"
    "osx-arm64:macOS ARM64 (Apple Silicon)"
    "linux-x64:Linux x64"
    "linux-arm64:Linux ARM64"
)

echo ""
echo "🔨 开始构建所有平台..."
echo ""

# 构建每个平台
for platform in "${PLATFORMS[@]}"; do
    IFS=':' read -r runtime_id description <<< "$platform"
    
    echo "🎯 构建 $description ($runtime_id)..."
    
    # 发布命令
    dotnet publish \
        -c $BUILD_CONFIG \
        -r $runtime_id \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=false \
        -o "$OUTPUT_DIR/$runtime_id" \
        > /dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        # 获取可执行文件大小
        if [[ "$runtime_id" == win-* ]]; then
            EXE_FILE="$OUTPUT_DIR/$runtime_id/NcfDesktopApp.exe"
        else
            EXE_FILE="$OUTPUT_DIR/$runtime_id/NcfDesktopApp"
        fi
        
        if [ -f "$EXE_FILE" ]; then
            SIZE=$(du -h "$EXE_FILE" | cut -f1)
            echo "   ✅ 成功 - 文件大小: $SIZE"
        else
            echo "   ❌ 失败 - 可执行文件未找到"
        fi
    else
        echo "   ❌ 构建失败"
    fi
done

echo ""
echo "📋 构建摘要"
echo "============"
echo "构建配置: $BUILD_CONFIG"
echo "输出目录: $OUTPUT_DIR"
echo ""
echo "📁 生成的文件:"

# 列出生成的文件
for platform in "${PLATFORMS[@]}"; do
    IFS=':' read -r runtime_id description <<< "$platform"
    
    if [[ "$runtime_id" == win-* ]]; then
        EXE_FILE="$OUTPUT_DIR/$runtime_id/NcfDesktopApp.exe"
    else
        EXE_FILE="$OUTPUT_DIR/$runtime_id/NcfDesktopApp"
    fi
    
    if [ -f "$EXE_FILE" ]; then
        SIZE=$(du -h "$EXE_FILE" | cut -f1)
        echo "   📄 $description: $EXE_FILE ($SIZE)"
        
        # 为非Windows平台设置执行权限
        if [[ "$runtime_id" != win-* ]]; then
            chmod +x "$EXE_FILE"
        fi
    fi
done

echo ""
echo "🎉 构建完成！"
echo ""
echo "💡 使用提示:"
echo "   - Windows: 双击 .exe 文件运行"
echo "   - macOS/Linux: 在终端中运行 ./NcfDesktopApp"
echo "   - 首次运行可能需要下载 NCF 站点文件 (~50MB)"
echo "" 