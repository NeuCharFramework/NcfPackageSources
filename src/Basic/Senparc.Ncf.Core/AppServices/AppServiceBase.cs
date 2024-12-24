using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

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

        public T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public T GetRequiredService<T>()
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        public T GetRequiredKeyedService<T>(object? serviceKey)
        {
            return ServiceProvider.GetRequiredKeyedService<T>(serviceKey);
        }
    }
}
