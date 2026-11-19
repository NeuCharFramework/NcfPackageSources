/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XmlConfig.cs
    File description: XmlConfig related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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