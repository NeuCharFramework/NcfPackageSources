/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Color.cs
    File description: Color related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.KnowledgeBase
{
    /// <summary>
    /// Stores the sample RGB color entity used by knowledge-base module migration demonstrations.
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

        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        /// <summary>
        /// Replaces the color channels with random values in the supported byte range.
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
        /// Increases each color channel by ten, capped at 255.
        /// </summary>
        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        /// <summary>
        /// Decreases each color channel by ten, floored at zero.
        /// </summary>
        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }
    }
}
