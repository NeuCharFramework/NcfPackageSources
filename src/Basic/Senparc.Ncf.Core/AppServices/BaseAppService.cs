using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    public interface IBaseAppService
    { }

    public class BaseAppService : IBaseAppService
    {
        protected IServiceProvider ServiceProvider { get; private set; }

        public BaseAppService(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }
    }
}
