/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MpMessageHandlerAttribute.cs
    File description: MpMessageHandlerAttribute related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class MpMessageHandlerAttribute : Attribute
    {
        public MpMessageHandlerAttribute(string name)
        {
            Name = name;
        }


        public string Name { get; set; }
    }
}
