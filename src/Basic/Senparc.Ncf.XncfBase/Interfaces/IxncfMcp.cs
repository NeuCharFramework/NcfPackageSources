/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IxncfMcp.cs
    文件功能描述：IxncfMcp 相关实现
    
    
    创建标识：Senparc - 20250620
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
