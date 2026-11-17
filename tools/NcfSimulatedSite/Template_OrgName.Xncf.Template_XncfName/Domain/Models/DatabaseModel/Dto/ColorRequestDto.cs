using System.ComponentModel.DataAnnotations;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// createcolorrequestDTO
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
        ///Legacy comment translated to English.
        /// </summary>
        public string AdditionNote { get; set; }
    }

    /// <summary>
    /// updatecolorrequestDTO
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
        ///Legacy comment translated to English.
        /// </summary>
        public string AdditionNote { get; set; }
    }

    /// <summary>
    /// deletecolorrequestDTO
    /// </summary>
    public class DeleteColorRequestDto
    {
        [Required]
        public int Id { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class RandomizeColorRequestDto
    {
        [Required]
        public int Id { get; set; }
    }
}