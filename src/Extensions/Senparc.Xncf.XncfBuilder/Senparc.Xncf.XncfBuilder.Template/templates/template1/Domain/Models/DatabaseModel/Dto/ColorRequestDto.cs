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