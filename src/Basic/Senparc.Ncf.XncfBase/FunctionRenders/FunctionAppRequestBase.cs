/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: FunctionAppRequestBase.cs

    File description: FunctionAppRequestBase related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>
    public class FunctionAppRequestBase : AppRequestBase, IFunctionAppRequest
    {
        public virtual Task LoadData(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        protected void RecordLog(StringBuilder sb, string msg)
        {
            FunctionHelper.RecordLog(sb, msg);
        }

    }
}
