/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorResponse.cs
    File description: ColorResponse related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.OHS.Local.PL
{
    public class Color_GetOrInitColorResponse
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
        /// elapsed time
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
