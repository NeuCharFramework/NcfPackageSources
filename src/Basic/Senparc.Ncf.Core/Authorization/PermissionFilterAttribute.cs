/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: PermissionFilterAttribute.cs

    File description: PermissionFilterAttribute related implementation

    

    

    Created by: Senparc - 20211223

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Authorization
{
    public class PermissionFilterAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly PermissionRequirement _requirement;
        private readonly IAuthorizationService _authorizationService;

        public PermissionFilterAttribute(IAuthorizationService authorizationService, PermissionRequirement requirement)
        {
            _authorizationService = authorizationService;
            _requirement = requirement;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var result = await _authorizationService.AuthorizeAsync(context.HttpContext.User, null, _requirement);
            if (!result.Succeeded)
            {
                //Legacy comment translated to English.
                AppResponseBase<string[]> responseBase = new AppResponseBase<string[]>() { Data = _requirement.ResourceCodes, Success = false, ErrorMessage = "您没有此资源的操作权限。" };
                context.Result = new OkObjectResult(responseBase)
                {
                    StatusCode = (int)System.Net.HttpStatusCode.Forbidden
                };
            }
        }
    }
}
