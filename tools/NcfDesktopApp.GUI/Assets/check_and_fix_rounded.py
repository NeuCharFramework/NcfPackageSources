#!/usr/bin/env python3
"""
检查并修复MacOS图标圆角处理
"""

import os
from PIL import Image, ImageDraw
import sys

def create_rounded_rectangle_mask(size, radius):
    """创建圆角矩形蒙版"""
    mask = Image.new('L', size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle(
        [(0, 0), (size[0] - 1, size[1] - 1)],
        radius=radius,
        fill=255
    )
    return mask

def add_rounded_corners_and_white_padding_fixed(image_path, output_path, corner_radius_ratio=0.18, padding_ratio=0.1):
    """
    正确处理圆角和白色背景边距
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
            
            # 创建白色背景图片
            white_bg = Image.new('RGBA', (original_size, original_size), (255, 255, 255, 255))
            
            # 将缩放后的图片粘贴到白色背景中心位置
            white_bg.paste(img_resized, (padding, padding), img_resized)
            
            # 计算圆角半径
            corner_radius = int(original_size * corner_radius_ratio)
            
            # 创建圆角蒙版
            mask = create_rounded_rectangle_mask((original_size, original_size), corner_radius)
            
            # 创建最终的透明背景图片
            final_img = Image.new('RGBA', (original_size, original_size), (0, 0, 0, 0))
            
            # 应用圆角蒙版到白色背景图片
            white_bg_with_alpha = white_bg.copy()
            white_bg_with_alpha.putalpha(mask)
            
            # 粘贴到最终图片
            final_img.paste(white_bg_with_alpha, (0, 0), white_bg_with_alpha)
            
            # 保存结果
            final_img.save(output_path, 'PNG')
            print(f"✅ 圆角处理完成: {os.path.basename(image_path)} -> {os.path.basename(output_path)}")
            
            return True
            
    except Exception as e:
        print(f"❌ 处理失败 {image_path}: {e}")
        return False

def check_image_corners(image_path):
    """检查图片是否有圆角"""
    try:
        with Image.open(image_path) as img:
            if img.mode != 'RGBA':
                print(f"⚠️  {os.path.basename(image_path)}: 不是RGBA模式")
                return False
            
            # 检查四个角的透明度
            width, height = img.size
            corners = [
                (0, 0),  # 左上
                (width-1, 0),  # 右上
                (0, height-1),  # 左下
                (width-1, height-1)  # 右下
            ]
            
            corner_alpha_values = []
            for x, y in corners:
                alpha = img.getpixel((x, y))[3] if len(img.getpixel((x, y))) > 3 else 255
                corner_alpha_values.append(alpha)
            
            # 如果四个角都是透明的，说明有圆角
            all_transparent = all(alpha < 128 for alpha in corner_alpha_values)
            
            if all_transparent:
                print(f"✅ {os.path.basename(image_path)}: 有圆角效果")
                return True
            else:
                print(f"❌ {os.path.basename(image_path)}: 没有圆角效果 (角落alpha值: {corner_alpha_values})")
                return False
                
    except Exception as e:
        print(f"❌ 检查失败 {image_path}: {e}")
        return False

def main():
    """主函数"""
    base_dir = os.path.dirname(os.path.abspath(__file__))
    
    # 需要检查和处理的文件
    files_to_check = [
        "NCF-logo-rounded.png",
        "AppIcon.iconset/icon_128x128@2x.png",
        "AppIcon.iconset/icon_256x256.png",
        "AppIcon.iconset/icon_256x256@2x.png",
        "AppIcon.iconset/icon_512x512.png",
        "AppIcon.iconset/icon_512x512@2x.png",
    ]
    
    print("🔍 检查图标圆角效果...")
    print()
    
    needs_fixing = []
    
    # 检查所有文件
    for file_path in files_to_check:
        full_path = os.path.join(base_dir, file_path)
        if os.path.exists(full_path):
            if not check_image_corners(full_path):
                needs_fixing.append(file_path)
        else:
            print(f"⚠️  文件不存在: {file_path}")
    
    print()
    
    if needs_fixing:
        print(f"🛠️  需要修复 {len(needs_fixing)} 个文件...")
        print()
        
        # 修复需要处理的文件
        backup_dir = os.path.join(base_dir, "backup_originals")
        
        for file_path in needs_fixing:
            # 确定原始文件路径
            if file_path == "NCF-logo-rounded.png":
                original_file = "NCF-logo.png"
                source_path = os.path.join(backup_dir, original_file)
                if not os.path.exists(source_path):
                    source_path = os.path.join(base_dir, original_file)
            else:
                original_file = os.path.basename(file_path)
                source_path = os.path.join(backup_dir, original_file)
            
            target_path = os.path.join(base_dir, file_path)
            
            if os.path.exists(source_path):
                add_rounded_corners_and_white_padding_fixed(source_path, target_path)
            else:
                print(f"⚠️  找不到原始文件: {source_path}")
        
        print()
        print("🔄 重新生成.icns文件...")
        return True
    else:
        print("🎉 所有图标都已正确应用圆角效果!")
        return False

if __name__ == "__main__":
    # 检查PIL库
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print("❌ 错误: 需要安装Pillow库")
        sys.exit(1)
    
    needs_regenerate = main()
    
    if needs_regenerate:
        print("请运行: iconutil -c icns AppIcon.iconset")
