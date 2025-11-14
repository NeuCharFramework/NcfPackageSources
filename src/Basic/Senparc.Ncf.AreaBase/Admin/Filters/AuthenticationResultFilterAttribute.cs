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
    /// 校验权限的类
    /// </summary>
    public class AuthenticationResultFilterAttribute : IAsyncPageFilter, IFilterMetadata
    {
        private IServiceProvider _serviceProvider;
        public AuthenticationResultFilterAttribute(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (context.HandlerMethod == null)
            {
                context.Result = new OkObjectResult(new AjaxReturnModel() { Success = false, Msg = $"404，未找到对应的Handler。请检查请求方法请求地址是否有误！请求方法：{context.HttpContext.Request.Method}" }) { StatusCode = 404 };
                return;
            }
            await ValidatePermissionAsync(_serviceProvider, context, next);
        }

        /// <summary>
        /// 验证权限得方法
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
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
            IEnumerable<string> resourceCodes = attributeCodes?.ResourceCodes.ToList() ?? new List<string>() { "*" };//当前方法的资源Code
            //Console.WriteLine("isAjax:{0}， isIgnore：{1}", isAjax, isIgnore);
            System.Diagnostics.Debug.WriteLine("isAjax:{0}, isIgnore: {1}", isAjax, isIgnore);
            if (isIgnore || (resourceCodes.Any(_ => "*".Equals(_)) && isAjax))
            {
                await next();
            }
            else
            {
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
                        actionResult = new OkObjectResult(new AjaxReturnModel<string>(path) { Msg = "您没有权限访问", Success = false }) { StatusCode = (int)System.Net.HttpStatusCode.Forbidden };
                    }

                    context.Result = actionResult ?? new RedirectResult("/Admin/Forbidden?url=" + System.Web.HttpUtility.UrlEncode(path));
                }
            }
        }

        /// <summary>
        /// 是否是Ajax请求
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
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

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }
    }
}
