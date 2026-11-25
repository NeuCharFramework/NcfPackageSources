/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionAppRequestBase.cs
    File description: FunctionAppRequestBase related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

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
    /// <summary>
    /// Defines a function request that can load additional data from the service container.
    /// </summary>
    public interface IFunctionAppRequest : IAppRequest
    {
        /// <summary>
        /// Loads request-dependent data before function execution.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve data-loading dependencies.</param>
        /// <returns>A task that completes when loading is finished.</returns>
        Task LoadData(IServiceProvider serviceProvider);
    }

    /// <summary>
    /// Base request type for function-rendered application operations.
    /// </summary>
    public class FunctionAppRequestBase : AppRequestBase, IFunctionAppRequest
    {
        /// <summary>
        /// Provides a no-op data-loading hook for requests that need no additional initialization.
        /// </summary>
        /// <param name="serviceProvider">The provider available to derived request types.</param>
        /// <returns>A completed task.</returns>
        public virtual Task LoadData(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }


        /// <summary>
        /// Appends a standardized function log message through <see cref="FunctionHelper"/>.
        /// </summary>
        /// <param name="sb">The string builder receiving the log entry.</param>
        /// <param name="msg">The message to record.</param>
        protected void RecordLog(StringBuilder sb, string msg)
        {
            FunctionHelper.RecordLog(sb, msg);
        }

    }
}
