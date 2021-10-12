using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    public interface IAppServiceBase
    { }

    public abstract class AppServiceBase : IAppServiceBase
    {
        protected IServiceProvider ServiceProvider { get; private set; }

        public AppServiceBase(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }
    }
}
