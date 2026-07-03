/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MessageType.cs
    文件功能描述：MessageType 相关实现
    
    
    创建标识：Senparc - 20230920
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
