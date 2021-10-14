using Senparc.Ncf.Core.AppServices;
using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    public interface IFunctionAppRequest : IAppRequest
    {
        Task LoadData(IServiceProvider serviceProvider);
    }

    /// <summary>
    /// FunctionAppRequest 基类
    /// </summary>
    public class FunctionAppRequestBase : AppRequestBase, IFunctionAppRequest
    {
        public virtual Task LoadData(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }
    }
}
