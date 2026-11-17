/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IServiceInvoke.cs
    File description: IServiceInvoke related implementation

    Created by: Senparc - 20230925

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr.Blocks.ServiceInvoke.Interface
{
    internal interface IServiceInvoke
    {
        Task<TResult> InvokeMethodAsync<TResult>(InvokeType invokeType, string serviceId, string methodName, object? data = null);
    }
}
