/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ColorRequestDto.cs
    文件功能描述：ColorRequestDto 相关实现
    
    
    创建标识：Senparc - 20250606
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// 创建颜色请求DTO
    /// </summary>
    public class CreateColorRequestDto
    {
        [Range(0, 255, ErrorMessage = "红色值必须在0-255之间")]
        public int Red { get; set; }

        [Range(0, 255, ErrorMessage = "绿色值必须在0-255之间")]
        public int Green { get; set; }

        [Range(0, 255, ErrorMessage = "蓝色值必须在0-255之间")]
        public int Blue { get; set; }

        /// <summary>
        /// 附加备注
        /// </summary>
        public string AdditionNote { get; set; }
    }

    /// <summary>
    /// 更新颜色请求DTO
    /// </summary>
    public class UpdateColorRequestDto
    {
        [Required]
        public int Id { get; set; }

        [Range(0, 255, ErrorMessage = "红色值必须在0-255之间")]
        public int Red { get; set; }

        [Range(0, 255, ErrorMessage = "绿色值必须在0-255之间")]
        public int Green { get; set; }

        [Range(0, 255, ErrorMessage = "蓝色值必须在0-255之间")]
        public int Blue { get; set; }

        /// <summary>
        /// 附加备注
        /// </summary>
        public string AdditionNote { get; set; }
    }

    /// <summary>
    /// 删除颜色请求DTO
    /// </summary>
    public class DeleteColorRequestDto
    {
        [Required]
        public int Id { get; set; }
    }

    /// <summary>
    /// 随机化颜色请求DTO
    /// </summary>
    public class RandomizeColorRequestDto
    {
        [Required]
        public int Id { get; set; }
    }
}