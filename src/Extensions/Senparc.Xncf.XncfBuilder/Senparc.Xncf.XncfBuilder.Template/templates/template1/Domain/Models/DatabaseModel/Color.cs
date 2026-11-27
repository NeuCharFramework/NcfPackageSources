/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Color.cs
    File description: Color related implementation

    Created by: Senparc - 20200820

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;

namespace Template_OrgName.Xncf.Template_XncfName
{
    /// <summary>Stores the template sample RGB color entity.</summary>
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

        /// <summary>Initializes an empty color for ORM materialization.</summary>
        private Color() { }

        /// <summary>Initializes a color from RGB values, generating random values for negative components.</summary>
        /// <param name="red">The red component.</param>
        /// <param name="green">The green component.</param>
        /// <param name="blue">The blue component.</param>
        public Color(int red, int green, int blue)
        {
            if (red < 0 || green < 0 || blue < 0)
            {
                Random();
            }
            else
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        /// <summary>Initializes a color and its additional note.</summary>
        /// <param name="red">The red component.</param>
        /// <param name="green">The green component.</param>
        /// <param name="blue">The blue component.</param>
        /// <param name="additionNote">The optional additional note.</param>
        public Color(int red, int green, int blue, string additionNote) : this(red, green, blue)
        {
            AdditionNote = additionNote;
        }

        /// <summary>Initializes a color from a DTO.</summary>
        /// <param name="colorDto">The source color DTO.</param>
        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        /// <summary>Replaces all components with generated color values.</summary>
        public void Random()
        {
            //generate a random color code
            var radom = new Random();
            Func<int> getRadomColorCode = () => radom.Next(0, 255);
            Red = getRadomColorCode();
            Green = getRadomColorCode();
            Blue = getRadomColorCode();
        }

        /// <summary>Increases each component by ten, capped at 255.</summary>
        /// <summary>
        /// Increases each color channel by ten while clamping the result to the
        /// valid byte-channel upper bound.
        /// </summary>
        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        /// <summary>Decreases each component by ten, floored at zero.</summary>
        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }

        /// <summary>Copies editable color values from a request DTO.</summary>
        /// <param name="dto">The update request.</param>
        public void Update(UpdateColorRequestDto dto)
        {
            Red = dto.Red;
            Green = dto.Green;
            Blue = dto.Blue;
            AdditionNote = dto.AdditionNote;
        }
    }
}
