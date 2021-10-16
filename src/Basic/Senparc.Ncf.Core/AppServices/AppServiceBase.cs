using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Senparc.Ncf.Core.AppServices
{
    public interface IAppService
    {
        CancellationToken CancellationToken { get; set; }
    }

    public abstract class AppServiceBase : IAppService
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        public CancellationToken CancellationToken { get; set; }

        public AppServiceBase(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CancellationToken =new CancellationToken();
        }
    }
}
