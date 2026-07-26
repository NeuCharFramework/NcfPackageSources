/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ColorResponse.cs
    文件功能描述：XNCF 模板示例实现
    
    
    创建标识：Senparc - 20211225
    
    修改标识：Senparc - 20260726
    修改描述：v1.1.0 补充示例模板 EventBus 请求-响应回环与多语言能力

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
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; private set; }
        /// <summary>
        /// 花费时间
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
