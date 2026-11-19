/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InvokeType.cs
    File description: InvokeType related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr.Blocks.ServiceInvoke
{
    public enum InvokeType
    {
        Patch = 1,
        Post = 2,
        Get = 3,
        Put = 4,
        Delete = 5,
    }
}
