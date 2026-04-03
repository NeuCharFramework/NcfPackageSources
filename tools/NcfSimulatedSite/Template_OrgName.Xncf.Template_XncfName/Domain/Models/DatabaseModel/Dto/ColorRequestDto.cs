using System.ComponentModel.DataAnnotations;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    ///Create color request DTO
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
        ///Additional notes
        /// </summary>
        public string AdditionNote { get; set; }
    }

    /// <summary>
    /// Update color request DTO
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
        ///Additional notes
        /// </summary>
        public string AdditionNote { get; set; }
    }

    /// <summary>
    /// Delete color request DTO
    /// </summary>
    public class DeleteColorRequestDto
    {
        [Required]
        public int Id { get; set; }
    }

    /// <summary>
    /// Randomize color request DTO
    /// </summary>
    public class RandomizeColorRequestDto
    {
        [Required]
        public int Id { get; set; }
    }
}