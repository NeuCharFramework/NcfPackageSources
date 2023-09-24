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
