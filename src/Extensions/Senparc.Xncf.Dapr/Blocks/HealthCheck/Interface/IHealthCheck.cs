using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr.Blocks.HealthCheck.Interface
{
    internal interface IHealthCheck
    {
        Task<bool> HealthCheckAsync();
    }
}
