#!/usr/bin/env python3
"""
macOS 应用图标处理工具 v2.0
=================================

功能：
- 为图标添加圆角效果（符合macOS设计规范）
- 支持透明或白色背景
- 智能边距处理
- 自动备份原始文件
- 验证处理结果
- 自动生成.icns文件

使用示例：
    python macos_icon_processor.py --white-background
    python macos_icon_processor.py --check-only
    python macos_icon_processor.py --restore-from-backup
    python macos_icon_processor.py --custom-corner-radius 0.2 --custom-padding 0.15

作者: NCF Team
许可: MIT License
"""

import os
import sys
import argparse
import shutil
import subprocess
from typing import Tuple, List, Optional
from dataclasses import dataclass

try:
    from PIL import Image, ImageDraw
except ImportError:
    print("❌ 错误: 需要安装Pillow库")
    print("请运行: pip install Pillow")
    sys.exit(1)

@dataclass
class ProcessingConfig:
    """图标处理配置"""
    corner_radius_ratio: float = 0.18  # macOS建议18%
    padding_ratio: float = 0.1         # 边距10%
    background_color: Tuple[int, int, int, int] = (0, 0, 0, 0)  # 透明背景
    white_background: bool = False
    backup_enabled: bool = True
    verbose: bool = True

class MacOSIconProcessor:
    """macOS图标处理器"""
    
    def __init__(self, base_dir: Optional[str] = None, config: Optional[ProcessingConfig] = None):
        self.base_dir = base_dir or os.path.dirname(os.path.abspath(__file__))
        self.config = config or ProcessingConfig()
        self.backup_dir = os.path.join(self.base_dir, "backup_originals")
        
        # 需要处理的文件列表（大于128px的图标）
        self.files_to_process = [
            ("NCF-logo.png", "NCF-logo-rounded.png"),
            ("AppIcon.iconset/icon_128x128@2x.png", "AppIcon.iconset/icon_128x128@2x.png"),
            ("AppIcon.iconset/icon_256x256.png", "AppIcon.iconset/icon_256x256.png"),
            ("AppIcon.iconset/icon_256x256@2x.png", "AppIcon.iconset/icon_256x256@2x.png"),
            ("AppIcon.iconset/icon_512x512.png", "AppIcon.iconset/icon_512x512.png"),
            ("AppIcon.iconset/icon_512x512@2x.png", "AppIcon.iconset/icon_512x512@2x.png"),
        ]
    
    def log(self, message: str, level: str = "info"):
        """日志输出"""
        if not self.config.verbose:
            return
            
        icons = {
            "info": "ℹ️",
            "success": "✅",
            "warning": "⚠️",
            "error": "❌",
            "process": "🔄",
            "backup": "📋",
            "check": "🔍"
        }
        print(f"{icons.get(level, 'ℹ️')} {message}")
    
    def create_rounded_rectangle_mask(self, size: Tuple[int, int], radius: int) -> Image.Image:
        """创建圆角矩形蒙版"""
        mask = Image.new('L', size, 0)
        draw = ImageDraw.Draw(mask)
        draw.rounded_rectangle(
            [(0, 0), (size[0] - 1, size[1] - 1)],
            radius=radius,
            fill=255
        )
        return mask
    
    def process_single_icon(self, input_path: str, output_path: str) -> bool:
        """处理单个图标文件"""
        try:
            # 打开原始图片
            with Image.open(input_path) as img:
                # 确保是RGBA模式
                if img.mode != 'RGBA':
                    img = img.convert('RGBA')
                
                # 获取原始尺寸（假设是正方形）
                original_size = img.size[0]
                
                # 计算边距和内容区域大小
                padding = int(original_size * self.config.padding_ratio)
                content_size = original_size - 2 * padding
                
                # 缩放原始图片到内容区域大小
                img_resized = img.resize((content_size, content_size), Image.Resampling.LANCZOS)
                
                # 创建背景图片
                if self.config.white_background:
                    bg_color = (255, 255, 255, 255)
                else:
                    bg_color = self.config.background_color
                
                background = Image.new('RGBA', (original_size, original_size), bg_color)
                
                # 将缩放后的图片粘贴到中心位置
                background.paste(img_resized, (padding, padding), img_resized)
                
                # 计算圆角半径
                corner_radius = int(original_size * self.config.corner_radius_ratio)
                
                # 创建圆角蒙版
                mask = self.create_rounded_rectangle_mask((original_size, original_size), corner_radius)
                
                # 创建最终图片
                if self.config.white_background:
                    # 白色背景版本：圆角外透明，圆角内白色
                    final_img = Image.new('RGBA', (original_size, original_size), (0, 0, 0, 0))
                    background_with_alpha = background.copy()
                    background_with_alpha.putalpha(mask)
                    final_img.paste(background_with_alpha, (0, 0), background_with_alpha)
                else:
                    # 透明背景版本
                    final_img = Image.new('RGBA', (original_size, original_size), (0, 0, 0, 0))
                    final_img.paste(background, (0, 0))
                    final_img.putalpha(mask)
                
                # 保存结果
                os.makedirs(os.path.dirname(output_path), exist_ok=True)
                final_img.save(output_path, 'PNG')
                
                self.log(f"处理完成: {os.path.basename(input_path)} -> {os.path.basename(output_path)}", "success")
                return True
                
        except Exception as e:
            self.log(f"处理失败 {input_path}: {e}", "error")
            return False
    
    def check_icon_corners(self, image_path: str) -> bool:
        """检查图片是否有圆角效果"""
        try:
            with Image.open(image_path) as img:
                if img.mode != 'RGBA':
                    return False
                
                # 检查四个角的透明度
                width, height = img.size
                corners = [(0, 0), (width-1, 0), (0, height-1), (width-1, height-1)]
                
                corner_alpha_values = []
                for x, y in corners:
                    pixel = img.getpixel((x, y))
                    alpha = pixel[3] if len(pixel) > 3 else 255
                    corner_alpha_values.append(alpha)
                
                # 如果四个角都是透明的，说明有圆角
                all_transparent = all(alpha < 128 for alpha in corner_alpha_values)
                
                if all_transparent:
                    self.log(f"{os.path.basename(image_path)}: 有圆角效果", "success")
                    return True
                else:
                    self.log(f"{os.path.basename(image_path)}: 没有圆角效果 (角落alpha值: {corner_alpha_values})", "warning")
                    return False
                    
        except Exception as e:
            self.log(f"检查失败 {image_path}: {e}", "error")
            return False
    
    def backup_file(self, file_path: str) -> bool:
        """备份单个文件"""
        if not self.config.backup_enabled:
            return True
            
        try:
            os.makedirs(self.backup_dir, exist_ok=True)
            backup_path = os.path.join(self.backup_dir, os.path.basename(file_path))
            
            if not os.path.exists(backup_path):
                shutil.copy2(file_path, backup_path)
                self.log(f"已备份: {os.path.basename(file_path)}", "backup")
            
            return True
        except Exception as e:
            self.log(f"备份失败 {file_path}: {e}", "error")
            return False
    
    def restore_from_backup(self) -> bool:
        """从备份恢复原始文件"""
        if not os.path.exists(self.backup_dir):
            self.log("备份目录不存在", "warning")
            return False
        
        self.log("从备份恢复原始文件...", "process")
        
        try:
            for backup_file in os.listdir(self.backup_dir):
                backup_path = os.path.join(self.backup_dir, backup_file)
                
                if backup_file == "NCF-logo.png":
                    target_path = os.path.join(self.base_dir, backup_file)
                else:
                    target_path = os.path.join(self.base_dir, "AppIcon.iconset", backup_file)
                
                if os.path.isfile(backup_path):
                    os.makedirs(os.path.dirname(target_path), exist_ok=True)
                    shutil.copy2(backup_path, target_path)
                    self.log(f"已恢复: {backup_file}", "success")
            
            return True
        except Exception as e:
            self.log(f"恢复失败: {e}", "error")
            return False
    
    def generate_icns(self) -> bool:
        """生成.icns文件"""
        iconset_path = os.path.join(self.base_dir, "AppIcon.iconset")
        icns_path = os.path.join(self.base_dir, "AppIcon.icns")
        
        if not os.path.exists(iconset_path):
            self.log("AppIcon.iconset目录不存在", "warning")
            return False
        
        try:
            self.log("生成.icns文件...", "process")
            result = subprocess.run(
                ["iconutil", "-c", "icns", iconset_path, "-o", icns_path],
                capture_output=True, text=True
            )
            
            if result.returncode == 0:
                self.log("AppIcon.icns生成成功", "success")
                return True
            else:
                self.log(f"iconutil失败: {result.stderr}", "error")
                return False
                
        except FileNotFoundError:
            self.log("iconutil命令不可用（需要macOS环境）", "warning")
            return False
        except Exception as e:
            self.log(f"生成.icns失败: {e}", "error")
            return False
    
    def check_all_icons(self) -> List[str]:
        """检查所有图标的圆角效果"""
        self.log("检查图标圆角效果...", "check")
        
        needs_fixing = []
        check_files = [
            "NCF-logo-rounded.png",
            "AppIcon.iconset/icon_128x128@2x.png",
            "AppIcon.iconset/icon_256x256.png", 
            "AppIcon.iconset/icon_256x256@2x.png",
            "AppIcon.iconset/icon_512x512.png",
            "AppIcon.iconset/icon_512x512@2x.png",
        ]
        
        for file_path in check_files:
            full_path = os.path.join(self.base_dir, file_path)
            if os.path.exists(full_path):
                if not self.check_icon_corners(full_path):
                    needs_fixing.append(file_path)
            else:
                self.log(f"文件不存在: {file_path}", "warning")
        
        return needs_fixing
    
    def process_all_icons(self) -> bool:
        """处理所有图标文件"""
        bg_type = "白色背景" if self.config.white_background else "透明背景"
        self.log(f"开始处理macOS图标 ({bg_type})...", "process")
        self.log(f"工作目录: {self.base_dir}")
        self.log(f"备份目录: {self.backup_dir}")
        
        success_count = 0
        total_count = len(self.files_to_process)
        
        for input_file, output_file in self.files_to_process:
            input_path = os.path.join(self.base_dir, input_file)
            output_path = os.path.join(self.base_dir, output_file)
            
            # 检查输入文件是否存在
            if not os.path.exists(input_path):
                self.log(f"文件不存在，跳过: {input_file}", "warning")
                continue
            
            # 备份原始文件
            if self.backup_file(input_path):
                # 处理图片
                if self.process_single_icon(input_path, output_path):
                    success_count += 1
        
        # 输出总结
        self.log(f"处理完成: {success_count}/{total_count} 个文件成功")
        
        if success_count > 0:
            self.log("说明:")
            if self.config.white_background:
                self.log("   - 使用白色背景匹配Logo背景色")
            else:
                self.log("   - 使用透明背景")
            self.log(f"   - 已添加{int(self.config.corner_radius_ratio*100)}%圆角和{int(self.config.padding_ratio*100)}%边距")
            self.log("   - 图标符合macOS设计规范")
        
        return success_count == total_count

def main():
    """主函数"""
    parser = argparse.ArgumentParser(
        description="macOS应用图标处理工具",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
示例用法:
  {prog}                               # 处理图标（透明背景）
  {prog} --white-background            # 处理图标（白色背景）
  {prog} --check-only                  # 仅检查圆角效果
  {prog} --restore-from-backup         # 从备份恢复
  {prog} --custom-corner-radius 0.2    # 自定义圆角半径（20%%）
  {prog} --custom-padding 0.15         # 自定义边距（15%%）
  {prog} --no-backup                   # 不创建备份
  {prog} --quiet                       # 静默模式
        """.format(prog="macos_icon_processor.py")
    )
    
    parser.add_argument("--white-background", action="store_true",
                       help="使用白色背景（默认透明背景）")
    parser.add_argument("--check-only", action="store_true",
                       help="仅检查圆角效果，不处理文件")
    parser.add_argument("--restore-from-backup", action="store_true",
                       help="从备份恢复原始文件")
    parser.add_argument("--custom-corner-radius", type=float, metavar="RATIO",
                       help="自定义圆角半径比例（0.0-0.5，默认0.18）")
    parser.add_argument("--custom-padding", type=float, metavar="RATIO",
                       help="自定义边距比例（0.0-0.3，默认0.1）")
    parser.add_argument("--no-backup", action="store_true",
                       help="不创建备份文件")
    parser.add_argument("--no-icns", action="store_true",
                       help="不生成.icns文件")
    parser.add_argument("--quiet", action="store_true",
                       help="静默模式（减少输出）")
    parser.add_argument("--base-dir", type=str, metavar="PATH",
                       help="指定工作目录（默认脚本所在目录）")
    
    args = parser.parse_args()
    
    # 验证参数
    if args.custom_corner_radius is not None:
        if not 0.0 <= args.custom_corner_radius <= 0.5:
            print("❌ 圆角半径比例必须在0.0-0.5之间")
            sys.exit(1)
    
    if args.custom_padding is not None:
        if not 0.0 <= args.custom_padding <= 0.3:
            print("❌ 边距比例必须在0.0-0.3之间")
            sys.exit(1)
    
    # 创建配置
    config = ProcessingConfig(
        corner_radius_ratio=args.custom_corner_radius or 0.18,
        padding_ratio=args.custom_padding or 0.1,
        white_background=args.white_background,
        backup_enabled=not args.no_backup,
        verbose=not args.quiet
    )
    
    # 创建处理器
    processor = MacOSIconProcessor(base_dir=args.base_dir, config=config)
    
    try:
        # 执行相应操作
        if args.restore_from_backup:
            success = processor.restore_from_backup()
            sys.exit(0 if success else 1)
        
        elif args.check_only:
            needs_fixing = processor.check_all_icons()
            if needs_fixing:
                processor.log(f"需要修复 {len(needs_fixing)} 个文件:")
                for file in needs_fixing:
                    processor.log(f"  - {file}")
                sys.exit(1)
            else:
                processor.log("所有图标都已正确应用圆角效果!", "success")
                sys.exit(0)
        
        else:
            # 正常处理流程
            success = processor.process_all_icons()
            
            if success and not args.no_icns:
                processor.generate_icns()
            
            sys.exit(0 if success else 1)
    
    except KeyboardInterrupt:
        print("\n❌ 用户中断操作")
        sys.exit(1)
    except Exception as e:
        print(f"❌ 未知错误: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
