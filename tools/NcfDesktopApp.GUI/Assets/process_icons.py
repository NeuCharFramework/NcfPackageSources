#!/usr/bin/env python3
"""
MacOS图标处理脚本
为图标添加边距留白和圆角效果，适配MacOS设计规范
"""

import os
from PIL import Image, ImageDraw
import sys

def add_rounded_corners_and_padding(image_path, output_path, corner_radius_ratio=0.18, padding_ratio=0.1):
    """
    为图片添加圆角和边距
    
    Args:
        image_path: 输入图片路径
        output_path: 输出图片路径
        corner_radius_ratio: 圆角半径占图片尺寸的比例 (MacOS建议18%)
        padding_ratio: 边距占图片尺寸的比例 (建议10%)
    """
    try:
        # 打开原始图片
        with Image.open(image_path) as img:
            # 确保是RGBA模式
            if img.mode != 'RGBA':
                img = img.convert('RGBA')
            
            # 获取原始尺寸
            original_size = img.size[0]  # 假设是正方形
            
            # 计算边距和内容区域大小
            padding = int(original_size * padding_ratio)
            content_size = original_size - 2 * padding
            
            # 缩放原始图片到内容区域大小
            img_resized = img.resize((content_size, content_size), Image.Resampling.LANCZOS)
            
            # 创建新的透明背景图片
            new_img = Image.new('RGBA', (original_size, original_size), (0, 0, 0, 0))
            
            # 将缩放后的图片粘贴到中心位置
            new_img.paste(img_resized, (padding, padding), img_resized)
            
            # 计算圆角半径
            corner_radius = int(original_size * corner_radius_ratio)
            
            # 创建圆角蒙版
            mask = Image.new('L', (original_size, original_size), 0)
            draw = ImageDraw.Draw(mask)
            draw.rounded_rectangle(
                [(0, 0), (original_size - 1, original_size - 1)],
                radius=corner_radius,
                fill=255
            )
            
            # 创建最终图片
            final_img = Image.new('RGBA', (original_size, original_size), (0, 0, 0, 0))
            final_img.paste(new_img, (0, 0))
            
            # 应用圆角蒙版
            final_img.putalpha(mask)
            
            # 保存结果
            final_img.save(output_path, 'PNG')
            print(f"✅ 处理完成: {os.path.basename(image_path)} -> {os.path.basename(output_path)}")
            
    except Exception as e:
        print(f"❌ 处理失败 {image_path}: {e}")

def process_all_icons():
    """处理所有需要的图标文件"""
    
    base_dir = os.path.dirname(os.path.abspath(__file__))
    iconset_dir = os.path.join(base_dir, "AppIcon.iconset")
    
    # 需要处理的文件列表 (尺寸大于128px的)
    files_to_process = [
        # NCF logo
        ("NCF-logo.png", "NCF-logo-rounded.png"),
        # AppIcon.iconset中大于128px的文件
        ("AppIcon.iconset/icon_128x128@2x.png", "AppIcon.iconset/icon_128x128@2x.png"),
        ("AppIcon.iconset/icon_256x256.png", "AppIcon.iconset/icon_256x256.png"),
        ("AppIcon.iconset/icon_256x256@2x.png", "AppIcon.iconset/icon_256x256@2x.png"),
        ("AppIcon.iconset/icon_512x512.png", "AppIcon.iconset/icon_512x512.png"),
        ("AppIcon.iconset/icon_512x512@2x.png", "AppIcon.iconset/icon_512x512@2x.png"),
    ]
    
    # 创建备份目录
    backup_dir = os.path.join(base_dir, "backup_originals")
    os.makedirs(backup_dir, exist_ok=True)
    
    print("🍎 开始处理MacOS图标...")
    print(f"📁 工作目录: {base_dir}")
    print(f"💾 备份目录: {backup_dir}")
    print()
    
    for input_file, output_file in files_to_process:
        input_path = os.path.join(base_dir, input_file)
        output_path = os.path.join(base_dir, output_file)
        
        # 检查输入文件是否存在
        if not os.path.exists(input_path):
            print(f"⚠️  文件不存在，跳过: {input_file}")
            continue
        
        # 备份原始文件
        backup_path = os.path.join(backup_dir, os.path.basename(input_file))
        if not os.path.exists(backup_path):
            import shutil
            shutil.copy2(input_path, backup_path)
            print(f"📋 已备份: {os.path.basename(input_file)}")
        
        # 处理图片
        add_rounded_corners_and_padding(input_path, output_path)
    
    print()
    print("🎉 所有图标处理完成!")
    print("📝 说明:")
    print("   - 原始文件已备份到 backup_originals/ 目录")
    print("   - 已为大于128px的图标添加了18%圆角和10%边距")
    print("   - 图标符合MacOS设计规范")
    print()
    print("🔄 重新生成.icns文件:")
    print("   请运行: iconutil -c icns AppIcon.iconset")

if __name__ == "__main__":
    # 检查PIL库
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print("❌ 错误: 需要安装Pillow库")
        print("请运行: pip install Pillow")
        sys.exit(1)
    
    process_all_icons()
