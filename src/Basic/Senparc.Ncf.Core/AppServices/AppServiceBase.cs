/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AppServiceBase.cs
    File description: AppServiceBase related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
