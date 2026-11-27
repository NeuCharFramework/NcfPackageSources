/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorResponse.cs
    File description: XNCF template sample implementation

    Created by: Senparc - 20211225

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Template_OrgName.Xncf.Template_XncfName.Application.DTOs.Response
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

        /// <summary>
        /// Initializes a response containing RGB values and the measured operation duration.
        /// </summary>
        /// <param name="red">The red component in the range 0 through 255.</param>
        /// <param name="green">The green component in the range 0 through 255.</param>
        /// <param name="blue">The blue component in the range 0 through 255.</param>
        /// <param name="costMillionSeconds">The elapsed duration reported by the sample operation.</param>
        public Color_GetOrInitColorResponse(int red, int green, int blue, double costMillionSeconds)
        {
            Red = red;
            Green = green;
            Blue = blue;
            CostMillionSeconds = costMillionSeconds;
        }
    }
}
