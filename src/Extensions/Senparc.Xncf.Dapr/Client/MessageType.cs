/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MessageType.cs
    File description: MessageType related implementation

    Created by: Senparc - 20230920

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr;

internal enum MessageType
{
    InvokePatch = 1,
    InvokePost = 2,
    InvokeGet = 3,
    InvokePut = 4,
    InvokeDelete = 5,
    Publish = 6,
    SetState = 7,
    GetState = 8,
    DeleteState = 9
}
