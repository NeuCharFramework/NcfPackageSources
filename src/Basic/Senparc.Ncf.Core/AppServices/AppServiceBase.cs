/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AppServiceBase.cs
    文件功能描述：AppServiceBase 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

#nullable enable
namespace Senparc.Ncf.Core.AppServices
{
    public interface IAppService
    {
        IServiceProvider ServiceProvider { get; }
        CancellationToken CancellationToken { get; set; }
    }

    public abstract class AppServiceBase : IAppService
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public CancellationToken CancellationToken { get; set; }

        public AppServiceBase(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CancellationToken = new CancellationToken();
        }

        public T? GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public T GetRequiredService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        public T GetRequiredKeyedService<T>(object? serviceKey) where T : notnull
        {
            return ServiceProvider.GetRequiredKeyedService<T>(serviceKey);
        }
    }
}
