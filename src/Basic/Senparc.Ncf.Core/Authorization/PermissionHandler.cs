/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: PermissionHandler.cs

    File description: PermissionHandler related implementation

    

    

    Created by: Senparc - 20211223

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ICheckPermission _checkPermission;

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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual int GetCurrentAdminUserInfoId(AuthorizationHandlerContext context)
        {
            var user = context.User;
            bool isConvertSucess = int.TryParse(user.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int adminUserInfoId);
            return adminUserInfoId;

        }
    }
}
