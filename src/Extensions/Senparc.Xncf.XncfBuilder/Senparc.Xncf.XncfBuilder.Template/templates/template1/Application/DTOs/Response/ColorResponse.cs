/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorResponse.cs
    File description: XNCF template sample implementation

    Created by: Senparc - 20211225

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

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

        public Color_GetOrInitColorResponse(int red, int green, int blue, double costMillionSeconds)
        {
            Red = red;
            Green = green;
            Blue = blue;
            CostMillionSeconds = costMillionSeconds;
        }
    }
}
