/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HttpGlobalExceptionFilter.cs
    File description: Senparc.Mvc.Filter related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Models.VD;
using System.Net;

namespace Senparc.Mvc.Filter
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Converts an unhandled MVC exception into the legacy HTML error view
        /// and marks the exception as handled by the filter.
        /// </remarks>
        public void OnException(ExceptionContext context)
        {
            //context.Result = new ObjectResult(context.Exception);
            //context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            //context.ExceptionHandled = true;
            context.HttpContext.Response.ContentType = "text/html";
            var result = new ViewResult
            {
                ViewName = "Error",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new Error_ExceptionVD()
                    {
                        Message = context.Exception.Message,
                    }
                }
            };
            context.Result = result;
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.ExceptionHandled = true;
        }
    }
}
