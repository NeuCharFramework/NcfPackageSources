using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// 校验
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
                // 有且仅有一个*
                context.Succeed(requirement);
                return;
            }
            int adminUserInfoId = GetCurrentAdminUserInfoId(context);
            bool hasPermission = await _checkPermission.HasPermissionAsync(requirement.ResourceCodes, adminUserInfoId); // 当前用户是否有权限
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
        /// 获取当前登录人Id
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
