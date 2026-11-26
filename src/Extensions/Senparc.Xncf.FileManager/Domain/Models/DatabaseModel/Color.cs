/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Color.cs
    File description: Color related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.FileManager.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager
{
    /// <summary>
    /// Represents an RGB color persisted by the FileManager sample data model.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// Gets the red component, normally in the range 0 through 255.
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// Gets the green component, normally in the range 0 through 255.
        /// </summary>
        public int Green { get; private set; }

        /// <summary>
        /// Gets the blue component, normally in the range 0 through 255.
        /// </summary>
        public int Blue { get; private set; }

        /// <summary>
        /// Gets the auxiliary note retained for multi-database migration tests.
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
                Random();
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
            // Generate each component independently so the fallback color is not biased
            // toward a single channel.
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
