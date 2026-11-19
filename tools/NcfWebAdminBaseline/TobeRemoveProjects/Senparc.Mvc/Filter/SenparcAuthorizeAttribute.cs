/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SenparcAuthorizeAttribute.cs
    File description: SenparcAuthorizeAttribute related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Senparc.Mvc
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
            Justification = "Unsealed so that subclassed types can set properties in the default constructor or override our behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class SenparcAuthorizeAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        private string Message { get; set; }
        private string RoleModuleName { get; set; }
        private bool forceLogout = false;//Legacy comment translated to English.


        public SenparcAuthorizeAttribute()
            : this(null)
        { }

        public SenparcAuthorizeAttribute(string roleModuleName)
        {
            RoleModuleName = roleModuleName;
        }

        protected bool IsLogined(HttpContext httpContext)
        {
            return httpContext != null && httpContext.User.Identity.IsAuthenticated;
        }

        protected virtual bool AuthorizeCore(HttpContext httpContext)
        {
            //return true;
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            IPrincipal user = httpContext.User;
            if (!IsLogined(httpContext))
            {
                return false;//Legacy comment translated to English.
            }

            return true;

        }

        public virtual void OnAuthorization(AuthorizationFilterContext filterContext)
        {

            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (AuthorizeCore(filterContext.HttpContext))
            {
                // ** IMPORTANT **
                // Since we're performing authorization at the action level, the authorization code runs
                // after the output caching module. In the worst case this could allow an authorized user
                // to cause the page to be cached, then an unauthorized user would later be served the
                // cached page. We work around this by telling proxies not to cache the sensitive page,
                // then we hook our custom authorization code into the caching mechanism so that we have
                // the final say on whether a page should be served from the cache.

                //HttpCachePolicyBase cachePolicy = filterContext.HttpContext.Response.Cache;
                //cachePolicy.SetProxyMaxAge(new TimeSpan(0));
                //cachePolicy.AddValidationCallback(CacheValidateHandler, null /* data */);
            }
            else
            {
                // auth failed, redirect to login page

                //if (filterContext.RouteData.DataTokens["area"] != null
                //    && filterContext.RouteData.DataTokens["area"].ToString().ToUpper() == "ADMIN")
                //{
                //    filterContext.Controller.TempData["AdminLogin"] = true;
                //}

                //todo: to a special page
                if (IsLogined(filterContext.HttpContext))
                {
                    //Legacy comment translated to English.
                    if (forceLogout)
                    {
                        //COCONET notsupportfilterContext.Controllerproperty，temporarilyhide
                        //Legacy comment translated to English.
                    }
                    else
                    {
                        //COCONET notsupportfilterContext.Controllerproperty，temporarilyhide
                        //Legacy comment translated to English.
                    }
                }
                else
                {
                    if (filterContext.HttpContext.Request.IsAjaxRequest())
                    {
                        var data = new { Success = false, Result = new { Message = "您尚未登录，请登录后再试！" } };
                        var jsonResult = new JsonResult(data);
                        filterContext.Result = jsonResult;
                    }
                }
                if (filterContext.Result == null)
                {
                    filterContext.Result = new UnauthorizedResult();
                }
            }
        }
    }
}
