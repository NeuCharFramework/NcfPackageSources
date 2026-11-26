/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Color.cs
    File description: Color related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.DynamicData.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>
    /// Stores an RGB color used by the DynamicData sample and configuration surfaces.
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

        /// <summary>
        /// Initializes an empty color for ORM materialization.
        /// </summary>
        private Color() { }

        /// <summary>
        /// Initializes a color from RGB components, generating a random color when any component is negative.
        /// </summary>
        /// <param name="red">The red component, normally in the range 0 through 255.</param>
        /// <param name="green">The green component, normally in the range 0 through 255.</param>
        /// <param name="blue">The blue component, normally in the range 0 through 255.</param>
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
        /// Initializes a color from its DTO representation.
        /// </summary>
        /// <param name="colorDto">The DTO containing RGB component values.</param>
        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        /// <summary>
        /// Replaces all RGB components with independently generated values from the supported color range.
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
        /// Increases each RGB component by ten without exceeding 255.
        /// </summary>
        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        /// <summary>
        /// Decreases each RGB component by ten without going below zero.
        /// </summary>
        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }
    }
}
