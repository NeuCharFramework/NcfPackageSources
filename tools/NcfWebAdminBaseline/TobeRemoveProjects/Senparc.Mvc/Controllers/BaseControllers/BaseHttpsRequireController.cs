/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: BaseHttpsRequireController.cs
    File description: BaseHttpsRequireController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Mvc.Filter;

namespace Senparc.Mvc
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    //[SenparcRequireHttps(Order = -1)]
    public class BaseHttpsRequireController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var httpMethod = httpContext.Request.Method;

            //Log.LogUtility.WebLogger.Error("BaseHttpsRequire url:{0}  Method:{1}, IsIndex:{2},Https:{3}".With(url,
            //    httpMethod, isIndex, isHttps));
            if (httpMethod == "HEAD")
            {
                var isHttps = httpContext.Request.IsHttps;
                if (!isHttps)
                {
                    var url = httpContext.Request.Path; //Legacy comment translated to English.
                    var isIndex = url == null || url.ToString() == "/";
                    if (isIndex)
                    {
                        context.Result = Redirect("https://ncf.senparc.com/ ");//Legacy comment translated to English.
                        //Log.LogUtility.WebLogger.Error("BaseHttpsRequire url:{0}  IP:{1},Method:{2},"
                        //    .With(httpContext.Request.Url, httpContext.Request.UserHostAddress,
                        //        httpContext.Request.HttpMethod));
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
}