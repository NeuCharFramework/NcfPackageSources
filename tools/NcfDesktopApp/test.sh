#!/bin/bash

# NCF Desktop App 测试脚本
echo "🧪 NCF Desktop App 测试脚本"
echo "============================="

# 检查 .NET 是否安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 未安装"
    exit 1
fi

echo "✅ .NET SDK 已安装: $(dotnet --version)"

# 检查项目是否能构建
echo "🔨 测试项目构建..."
dotnet build > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "✅ 项目构建成功"
else
    echo "❌ 项目构建失败"
    exit 1
fi

# 测试GitHub API连接
echo "🌐 测试GitHub API连接..."
curl -s --max-time 10 "https://api.github.com/repos/NeuCharFramework/NCF/releases/latest" > /dev/null

if [ $? -eq 0 ]; then
    echo "✅ GitHub API 连接正常"
else
    echo "⚠️  GitHub API 连接失败 (可能是网络问题)"
fi

# 测试运行应用程序（只检测平台）
echo "🖥️  测试平台检测..."
echo "当前系统信息:"
echo "  操作系统: $(uname -s)"
echo "  架构: $(uname -m)"

# 显示应该下载的包名
case "$(uname -s)" in
    Darwin)
        case "$(uname -m)" in
            x86_64) echo "  建议下载: ncf-osx-x64-*.zip" ;;
            arm64) echo "  建议下载: ncf-osx-arm64-*.zip" ;;
            *) echo "  未知架构" ;;
        esac
        ;;
    Linux)
        case "$(uname -m)" in
            x86_64) echo "  建议下载: ncf-linux-x64-*.zip" ;;
            aarch64) echo "  建议下载: ncf-linux-arm64-*.zip" ;;
            *) echo "  未知架构" ;;
        esac
        ;;
    MINGW*|CYGWIN*|MSYS*)
        case "$(uname -m)" in
            x86_64) echo "  建议下载: ncf-win-x64-*.zip" ;;
            *) echo "  建议下载: ncf-win-x64-*.zip" ;;
        esac
        ;;
    *)
        echo "  未知操作系统"
        ;;
esac

echo ""
echo "🎉 基础测试完成！"
echo ""
echo "💡 下一步:"
echo "   1. 运行 'dotnet run' 来启动应用程序"
echo "   2. 或运行 './build.sh' 来构建所有平台的可执行文件"
echo "   3. 首次运行会下载约50MB的NCF站点文件"
echo "" 