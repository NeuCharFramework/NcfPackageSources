using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
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


        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        protected void RecordLog(StringBuilder sb, string msg)
        {
            FunctionHelper.RecordLog(sb, msg);
        }

    }
}
