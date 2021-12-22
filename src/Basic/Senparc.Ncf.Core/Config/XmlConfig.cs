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