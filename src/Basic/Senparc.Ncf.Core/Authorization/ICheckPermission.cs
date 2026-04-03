using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    ///check permissions
    /// </summary>
    public interface ICheckPermission
    {
        /// <summary>
        ///check permissions
        /// </summary>
        /// <param name="resourceCodes"></param>
        /// <param name="adminUserInfoId"></param>
        /// <returns></returns>
        Task<bool> HasPermissionAsync(string[] resourceCodes, int adminUserInfoId);
    }
}
