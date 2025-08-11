#!/usr/bin/env python3
"""
从 AppIcon.iconset 创建 Windows ICO 文件
"""
import os
import sys

try:
    from PIL import Image
    
    def create_ico_from_iconset():
        # 使用 iconset 中的图片创建 ICO
        iconset_dir = "Assets/AppIcon.iconset"
        ico_path = "Assets/app-icon.ico"
        
        if not os.path.exists(iconset_dir):
            print(f"❌ iconset 目录不存在: {iconset_dir}")
            return False
            
        # ICO 需要的标准尺寸和对应的文件
        ico_sources = [
            ("icon_16x16.png", 16),
            ("icon_32x32.png", 32), 
            ("icon_128x128.png", 128),
            ("icon_256x256.png", 256)
        ]
        
        images = []
        for filename, size in ico_sources:
            file_path = os.path.join(iconset_dir, filename)
            if os.path.exists(file_path):
                img = Image.open(file_path)
                img = img.convert('RGBA')
                if img.size != (size, size):
                    img = img.resize((size, size), Image.LANCZOS)
                images.append(img)
                print(f"✅ 添加 {filename} ({size}x{size})")
        
        if not images:
            print("❌ 未找到可用的图标文件")
            return False
            
        # 保存为 ICO
        images[0].save(ico_path, format='ICO', sizes=[(img.width, img.height) for img in images])
        print(f"✅ 成功创建 {ico_path}")
        return True
        
except ImportError:
    print("❌ 需要安装 Pillow: pip install Pillow")
    def create_ico_from_iconset():
        return False

if __name__ == "__main__":
    if create_ico_from_iconset():
        print("🎉 ICO 文件创建完成!")
    else:
        sys.exit(1)
