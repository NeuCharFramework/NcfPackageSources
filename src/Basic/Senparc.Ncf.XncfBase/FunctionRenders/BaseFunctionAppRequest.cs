using Senparc.Ncf.Core.AppServices.Models;
using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    public class BaseFunctionAppRequest : BaseAppRequest
    {
        public virtual Task LoadData(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }
    }
}
