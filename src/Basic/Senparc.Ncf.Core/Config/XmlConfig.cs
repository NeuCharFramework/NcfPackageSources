/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XmlConfig.cs
    文件功能描述：XmlConfig 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.CO2NET.Utilities;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Senparc.Ncf.Core.Config
{
    public partial class XmlConfig
    {

        public static XElement GetXElement(string path)
        {
            return XElement.Load(ServerUtility.ContentRootMapPath(path));
        }

        public static string GetMapPath(string path)
        {
            return ServerUtility.ContentRootMapPath(path);
        }

    }
}