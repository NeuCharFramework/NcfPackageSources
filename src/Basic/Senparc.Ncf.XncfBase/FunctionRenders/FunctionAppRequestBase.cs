using Senparc.Ncf.Core.AppServices;
using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    /// <summary>
    /// FunctionAppRequest 基类
    /// </summary>
    public class FunctionAppRequestBase : AppRequestBase
    {
        public virtual Task LoadData(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }
    }
}
