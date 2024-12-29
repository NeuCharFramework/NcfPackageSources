using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// 检查权限
    /// </summary>
    public interface ICheckPermission
    {
        /// <summary>
        /// 检查权限
        /// </summary>
        /// <param name="resourceCodes"></param>
        /// <param name="adminUserInfoId"></param>
        /// <returns></returns>
        Task<bool> HasPermissionAsync(string[] resourceCodes, int adminUserInfoId);
    }
}
