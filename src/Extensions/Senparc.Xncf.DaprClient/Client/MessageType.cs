using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DaprClient
{
    internal enum MessageType
    {
        Invoke = 1,
        Publish = 2,
        SetState = 3,
        GetState = 4,
        DeleteState = 5
    }
}
