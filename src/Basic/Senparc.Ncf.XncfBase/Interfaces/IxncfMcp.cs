/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IxncfMcp.cs

    File description: IxncfMcp related implementation

    

    

    Created by: Senparc - 20250620

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase
{
    internal interface IxncfMcp
    {

        public IServiceCollection AddMcp(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {

            return services;
        }
    }
}
