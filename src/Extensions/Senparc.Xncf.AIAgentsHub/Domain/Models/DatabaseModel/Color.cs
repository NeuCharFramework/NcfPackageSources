/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Color.cs
    File description: Color related implementation

    Created by: Senparc - 20240311

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.13.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.13.16.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIAgentsHub.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AIAgentsHub
{
    /// <summary>
    /// Stores an RGB color used by the AIAgentsHub sample domain and its migration tests.
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
        /// Initializes a color from RGB components. If any component is negative, the entity
        /// generates a random color instead of preserving the supplied values.
        /// </summary>
        /// <param name="red">The red component, normally from 0 through 255.</param>
        /// <param name="green">The green component, normally from 0 through 255.</param>
        /// <param name="blue">The blue component, normally from 0 through 255.</param>
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
        /// Initializes a color from the RGB values in a DTO.
        /// </summary>
        /// <param name="colorDto">The DTO containing the source RGB values.</param>
        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        /// <summary>
        /// Replaces all RGB components with independently generated values.
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
        /// Increases every RGB component by ten, capped at 255.
        /// </summary>
        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        /// <summary>
        /// Decreases every RGB component by ten, floored at zero.
        /// </summary>
        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }
    }
}
