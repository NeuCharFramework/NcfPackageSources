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
