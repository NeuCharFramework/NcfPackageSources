/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ICheckPermission.cs
    文件功能描述：ICheckPermission 相关实现
    
    
    创建标识：Senparc - 20211223
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
