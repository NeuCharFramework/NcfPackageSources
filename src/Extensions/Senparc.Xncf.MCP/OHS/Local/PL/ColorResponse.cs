using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.OHS.Local.PL
{
    public class Color_GetOrInitColorResponse
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
        /// spend time
        /// </summary>
        public double CostMillionSeconds { get; set; }

        public Color_GetOrInitColorResponse(int red, int green, int blue, double costMillionSeconds)
        {
            Red = red;
            Green = green;
            Blue = blue;
            CostMillionSeconds = costMillionSeconds;
        }
    }
}
