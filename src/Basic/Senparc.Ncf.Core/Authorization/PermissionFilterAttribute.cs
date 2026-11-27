/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PermissionFilterAttribute.cs
    File description: PermissionFilterAttribute related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>
        /// Initializes a resource-permission authorization filter.
        /// </summary>
        /// <param name="authorizationService">The authorization service.</param>
        /// <param name="requirement">The resource requirement to evaluate.</param>
        public PermissionFilterAttribute(IAuthorizationService authorizationService, PermissionRequirement requirement)
        {
            _authorizationService = authorizationService;
            _requirement = requirement;
        }

        /// <summary>
        /// Evaluates the requirement and returns a forbidden application response when unauthorized.
        /// </summary>
        /// <param name="context">The MVC authorization context.</param>
        /// <returns>A task that completes after authorization evaluation.</returns>
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
