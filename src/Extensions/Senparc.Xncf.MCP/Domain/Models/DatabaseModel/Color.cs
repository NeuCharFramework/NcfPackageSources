using Senparc.Ncf.Core.Models;
using Senparc.Xncf.MCP.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.MCP
{
    /// <summary>
    /// Color entity class
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Green { get; private set; }

        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Blue { get; private set; }

        /// <summary>
        /// Additional columns, test the database multiple times Migrate
        /// </summary>
        public string AdditionNote { get; private set; }

        private Color() { }

        public Color(int red, int green, int blue)
        {
            if (red < 0 || green < 0 || blue < 0)
            {
                Random();//random
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

        public void Random()
        {
            //Randomly generate color codes
            var radom = new Random();
            Func<int> getRadomColorCode = () => radom.Next(0, 255);
            Red = getRadomColorCode();
            Green = getRadomColorCode();
            Blue = getRadomColorCode();
        }

        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }
    }
}
