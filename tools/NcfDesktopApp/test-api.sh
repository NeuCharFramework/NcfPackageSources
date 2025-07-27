#!/bin/bash

# GitHub API 测试脚本
echo "🧪 测试 GitHub API 调用和字段解析"
echo "=================================="

# 直接调用 GitHub API 查看返回的数据结构
echo "📡 直接调用 GitHub API..."
curl -s "https://api.github.com/repos/NeuCharFramework/NCF/releases/latest" | jq '{
  tag_name: .tag_name,
  name: .name,
  assets_count: (.assets | length),
  sample_asset: .assets[0] | {
    name: .name,
    browser_download_url: .browser_download_url,
    size: .size
  }
}'

echo ""
echo "🎯 查找 macOS ARM64 包..."
curl -s "https://api.github.com/repos/NeuCharFramework/NCF/releases/latest" | jq '.assets[] | select(.name | contains("osx-arm64")) | {
  name: .name,
  browser_download_url: .browser_download_url,
  size: .size
}'

echo ""
echo "✅ API 测试完成" 