/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionAppRequestBase.cs
    文件功能描述：FunctionAppRequestBase 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
