/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ColorRequestDto.cs
    File description: ColorRequestDto related implementation

    Created by: Senparc - 20250524

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261124
    Change description: v1.1.15 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// createcolorrequestDTO
    /// </summary>
    public class CreateColorRequestDto
    {
        [Range(0, 255, ErrorMessage = "The red value must be between 0 and 255")]
        public int Red { get; set; }

        [Range(0, 255, ErrorMessage = "The green value must be between 0 and 255")]
        public int Green { get; set; }

        [Range(0, 255, ErrorMessage = "The blue value must be between 0 and 255")]
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

        [Range(0, 255, ErrorMessage = "The red value must be between 0 and 255")]
        public int Red { get; set; }

        [Range(0, 255, ErrorMessage = "The green value must be between 0 and 255")]
        public int Green { get; set; }

        [Range(0, 255, ErrorMessage = "The blue value must be between 0 and 255")]
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
