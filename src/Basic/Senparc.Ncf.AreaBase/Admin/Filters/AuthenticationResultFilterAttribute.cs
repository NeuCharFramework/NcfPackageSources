/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AuthenticationResultFilterAttribute.cs
    File description: AuthenticationResultFilterAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.23.0-preview4 will integrate core multilingual resources for the Handler missing and none permission prompts.

    Modified by: Senparc - 20260812
    Change description: Redirect to login when a valid management session has not been established to avoid incorrectly returning 403

    Modified by: Senparc - 20260813
    Change description: v0.23.3-preview7 Fixes the login redirection when the backend session expires and the persistence of data protection keys

    Modified by: Senparc - 20261119
    Change description: v0.23.12 Improved administration page and authorization support

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Internal;

namespace Senparc.Ncf.AreaBase.Admin.Filters
{
    /// <summary>
    /// Class for validating permissions
    /// </summary>
    public class AuthenticationResultFilterAttribute : IAsyncPageFilter, IFilterMetadata
    {
        private IServiceProvider _serviceProvider;
        /// <summary>
        /// Initializes the filter with the service provider used to resolve authorization services.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        public AuthenticationResultFilterAttribute(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Rejects missing handlers or delegates the request to permission validation.
        /// </summary>
        /// <param name="context">The page-handler execution context.</param>
        /// <param name="next">The next page-handler delegate.</param>
        /// <returns>A task that completes after validation and, when allowed, downstream execution.</returns>
        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (context.HandlerMethod == null)
            {
                context.Result = new OkObjectResult(new AjaxReturnModel() { Success = false, Msg = string.Format(NcfCoreResource.Get("Auth.HandlerNotFound", "404，未找到对应的Handler。请检查请求方法请求地址是否有误！请求方法：{0}"), context.HttpContext.Request.Method) }) { StatusCode = 404 };
                return;
            }
            await ValidatePermissionAsync(_serviceProvider, context, next);
        }

        /// <summary>
        /// Validates the current administration session and resource permission before execution.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve work-context and permission services.</param>
        /// <param name="context">The page-handler execution context.</param>
        /// <param name="next">The delegate to invoke when access is permitted.</param>
        /// <returns>A task that completes after the request is allowed or a result is assigned.</returns>
        public virtual async Task ValidatePermissionAsync(IServiceProvider serviceProvider, PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            bool canAccessResource = false;
            bool isAjax = false;
            isAjax = IsAjax(context);
            CustomerResourceAttribute attributeCodes = context.HandlerMethod.MethodInfo
                .GetCustomAttributes(typeof(CustomerResourceAttribute), false)
                .OfType<CustomerResourceAttribute>()
                .FirstOrDefault();
            bool isIgnore = context.Filters.OfType<IgnoreAuthAttribute>().Any();
            IEnumerable<string> resourceCodes = attributeCodes?.ResourceCodes.ToList() ?? new List<string>() { "*" };//Resource Code for currentmethod
            //Console.WriteLine("isAjax:{0}， isIgnore：{1}", isAjax, isIgnore);
            System.Diagnostics.Debug.WriteLine("isAjax:{0}, isIgnore: {1}", isAjax, isIgnore);
            if (isIgnore || (resourceCodes.Any(_ => "*".Equals(_)) && isAjax))
            {
                await next();
                return;
            }

            // When there is no effective management session, log in, and not 403/Forbidden after menu permission failure.
            var adminWorkContext = serviceProvider.GetService<IAdminWorkContextProvider>()?.GetAdminWorkContext();
            if (adminWorkContext == null || adminWorkContext.AdminUserId <= 0)
            {
                var returnUrl = $"{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}";
                if (isAjax)
                {
                    context.Result = new JsonResult(new AjaxReturnModel()
                    {
                        Success = false,
                        Msg = NcfCoreResource.Get("Auth.Forbidden", "您没有权限访问")
                    })
                    { StatusCode = (int)System.Net.HttpStatusCode.Unauthorized };
                }
                else
                {
                    context.Result = new RedirectResult(
                        "/Admin/Login?ReturnUrl=" + System.Web.HttpUtility.UrlEncode(returnUrl));
                }

                return;
            }

            string url = string.Join(string.Empty, context.RouteData.Values.Values.Reverse());
            if (!url.StartsWith("/"))
            {
                url = string.Concat("/", url); // /Admin/AdminUserInfo/Index
            }
            System.Diagnostics.Debug.WriteLine("url:{0}", url);
            canAccessResource = await serviceProvider.GetService<SysRolePermissionService>().HasPermissionAsync(resourceCodes, url, isAjax);// await Task.FromResult(true);//TODO...
            if (canAccessResource)
            {
                await next();
            }
            else
            {
                string path = context.HttpContext.Request.Path.Value;
                IActionResult actionResult = null;
                if (isAjax)
                {
                    actionResult = new OkObjectResult(new AjaxReturnModel<string>(path) { Msg = NcfCoreResource.Get("Auth.Forbidden", "您没有权限访问"), Success = false }) { StatusCode = (int)System.Net.HttpStatusCode.Forbidden };
                }

                context.Result = actionResult ?? new RedirectResult("/Admin/Forbidden?url=" + System.Web.HttpUtility.UrlEncode(path));
            }
        }

        /// <summary>
        /// Determines whether the request carries the conventional XMLHttpRequest header.
        /// </summary>
        /// <param name="context">The page-handler execution context to inspect.</param>
        /// <returns><see langword="true"/> when the request identifies itself as XMLHttpRequest.</returns>
        public virtual bool IsAjax(PageHandlerExecutingContext context)
        {
            bool isAjax = false;
            if (context.HttpContext.Request.Headers.TryGetValue("x-requested-with", out Microsoft.Extensions.Primitives.StringValues strings))
            {
                if (strings.Contains("XMLHttpRequest"))
                {
                    isAjax = true;
                }
            }
            return isAjax;
        }

        /// <summary>
        /// Completes page-handler selection without changing the selected handler.
        /// </summary>
        /// <param name="context">The selected page-handler context.</param>
        /// <returns>A completed task.</returns>
        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }
    }
}
