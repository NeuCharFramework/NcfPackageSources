/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AuthenticationAsyncPageFilterAttribute.cs
    File description: AuthenticationAsyncPageFilterAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.23.0-preview4 will integrate Ajax none permission prompt with core multilingual resources

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.AreaBase.Admin.Filters
{

    //https://docs.microsoft.com/zh-cn/aspnet/core/razor-pages/filter?view=aspnetcore-3.1

    [Obsolete("AuthenticationResultFilterAttribute")]
    public class AuthenticationAsyncPageFilterAttribute : IAsyncPageFilter
    {
        private readonly SysRolePermissionService _sysPermissionService;
        private readonly IAdminWorkContextProvider _adminWorkContextProvider;
        private readonly SysMenuService _sysMenuService;

        public AuthenticationAsyncPageFilterAttribute(SysRolePermissionService sysPermissionService, Core.WorkContext.Provider.IAdminWorkContextProvider adminWorkContextProvider, SysMenuService _sysMenuService)
        {
            this._sysPermissionService = sysPermissionService;
            this._adminWorkContextProvider = adminWorkContextProvider;
            this._sysMenuService = _sysMenuService;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            //Perform asyncinvoke before ininvokehandlingprogrammethod, but after inmodel binding end.

            //context.ActionDescriptor.FilterDescriptors
            var authenticateResult = await context.HttpContext.AuthenticateAsync(AdminAuthorizeAttribute.AuthenticationScheme);
            if (authenticateResult.Succeeded && !context.Filters.Any(_ => _ is AllowAnonymousFilter))
            {
                AdminPageModelBase adminPageModel = context.HandlerInstance as AdminPageModelBase;
                //adminPageModel.SysMenuDtos = await _sysMenuService.GetMenuTreeDtoByCacheAsync();
                adminPageModel.AdminWorkContext = _adminWorkContextProvider.GetAdminWorkContext();

                bool hasPageRoute = context.RouteData.Values.TryGetValue("page", out object page);
                bool hasAreaRoute = context.RouteData.Values.TryGetValue("area", out object area);

                bool hasRight = hasPageRoute && hasAreaRoute;
                if (hasRight)
                {
                    var url = context.HttpContext.Request.Path;/*.GetEncodedPathAndQuery()*/;
                    hasRight = await _sysPermissionService.HasPermissionAsync(url/*string.Concat("/", area, page)*/);
                }

                if (!hasRight /*&& !(adminPageModel is Pages.IndexModel)*/)
                {
                    IActionResult actionResult = new Microsoft.AspNetCore.Mvc.RedirectResult("/Admin/Forbidden");
                    //bounced
                    if (context.HttpContext.Request.Headers.TryGetValue("x-requested-with", out Microsoft.Extensions.Primitives.StringValues strings))
                    {
                        if (strings.Contains("XMLHttpRequest"))
                        {
                            actionResult = new JsonResult(new AjaxReturnModel() { Success = false, Msg = NcfCoreResource.Get("Auth.Forbidden", "您没有权限访问") }) { StatusCode = 401 };
                        }
                    }
                    context.Result = actionResult;
                    return;//If an IAsyncPageFilter provides a result value by setting the Result property of PageHandlerExecutingContext to a non-null value, then it cannot call the next filter by invoking PageHandlerExecutionDelegate.
                }
            }
            await next.Invoke();
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            //After selecting the handling program method, but before the model binding occurs, perform an async invoke.
            return Task.CompletedTask;
        }
    }
}
