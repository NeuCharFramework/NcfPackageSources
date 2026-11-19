/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Enum.cs
    File description: Enum related implementation

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
    public enum WeixinPlatformType
    {
        WeChat_OfficialAccount = 1,
        WeChat_MiniProgram = 2,
        WeChat_Work = 4,
        WeChat_Open = 8,
        WeChat_Tenpay = 16,
    }
}
