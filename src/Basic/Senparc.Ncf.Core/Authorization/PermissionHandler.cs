/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PermissionHandler.cs
    File description: PermissionHandler related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// Authorization handler that evaluates permission requirements through the configured
    /// application permission checker.
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ICheckPermission _checkPermission;

        /// <summary>
        /// Initializes the permission handler.
        /// </summary>
        /// <param name="checkPermission">The service used to evaluate resource permissions.</param>
        public PermissionHandler(ICheckPermission checkPermission)
        {
            _checkPermission = checkPermission;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (requirement.ResourceCodes.Length == 1 && PermissionRequirement.All.Equals(requirement.ResourceCodes[0]))
            {
                //Legacy comment translated to English.
                context.Succeed(requirement);
                return;
            }
            int adminUserInfoId = GetCurrentAdminUserInfoId(context);
            bool hasPermission = await _checkPermission.HasPermissionAsync(requirement.ResourceCodes, adminUserInfoId); //Legacy comment translated to English.
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        /// <summary>
        /// Extracts the administrator identifier from the authorization principal.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <returns>The parsed administrator identifier, or zero when no valid claim exists.</returns>
        public virtual int GetCurrentAdminUserInfoId(AuthorizationHandlerContext context)
        {
            var user = context.User;
            bool isConvertSucess = int.TryParse(user.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int adminUserInfoId);
            return adminUserInfoId;

        }
    }
}
