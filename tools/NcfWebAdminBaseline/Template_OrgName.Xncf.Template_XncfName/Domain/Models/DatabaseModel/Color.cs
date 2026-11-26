/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Color.cs
    File description: Color related implementation

    Created by: Senparc - 20200820

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261124
    Change description: v1.1.15 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;

namespace Template_OrgName.Xncf.Template_XncfName
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Green { get; private set; }

        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Blue { get; private set; }

        /// <summary>
        /// additional columns; test multiple database migrations
        /// </summary>
        public string AdditionNote { get; private set; }

        private Color() { }

        /// <summary>
        /// Initializes a color from RGB channels; any negative channel requests
        /// select generated channel values instead.
        /// </summary>
        /// <param name="red">The red channel, or a negative value to generate channels.</param>
        /// <param name="green">The green channel, or a negative value to generate channels.</param>
        /// <param name="blue">The blue channel, or a negative value to generate channels.</param>
        public Color(int red, int green, int blue)
        {
            if (red < 0 || green < 0 || blue < 0)
            {
                Random();//Legacy comment translated to English.
            }
            else
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        /// <summary>
        /// Initializes a color and stores an additional sample note.
        /// </summary>
        /// <param name="red">The red channel.</param>
        /// <param name="green">The green channel.</param>
        /// <param name="blue">The blue channel.</param>
        /// <param name="additionNote">The additional sample note.</param>
        public Color(int red, int green, int blue, string additionNote) : this(red, green, blue)
        {
            AdditionNote = additionNote;
        }

        /// <summary>
        /// Creates an entity from a color DTO.
        /// </summary>
        /// <param name="colorDto">The DTO containing RGB channel values.</param>
        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        /// <summary>
        /// Replaces all RGB channels with independently generated values.
        /// </summary>
        public void Random()
        {
            //generate a random color code
            var radom = new Random();
            Func<int> getRadomColorCode = () => radom.Next(0, 255);
            Red = getRadomColorCode();
            Green = getRadomColorCode();
            Blue = getRadomColorCode();
        }

        /// <summary>
        /// Raises each RGB channel by ten while capping values at 255.
        /// </summary>
        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        /// <summary>
        /// Lowers each RGB channel by ten while flooring values at zero.
        /// </summary>
        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }

        /// <summary>
        /// Replaces RGB channels and the additional note from an update DTO.
        /// </summary>
        /// <param name="dto">The replacement color values.</param>
        public void Update(UpdateColorRequestDto dto)
        {
            Red = dto.Red;
            Green = dto.Green;
            Blue = dto.Blue;
            AdditionNote = dto.AdditionNote;
        }
    }
}
