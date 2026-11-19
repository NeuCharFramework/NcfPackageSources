/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SenparcTraceType.cs
    File description: SenparcTraceType related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.SenparcTraceManager
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum SenparcTraceType
    {
        Normal = 0,
        API = 2,
        PostRequest = 4,
        GetRequest = 6,
        Exception = 8
    }
}
