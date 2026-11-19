/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ICheckPermission.cs
    File description: ICheckPermission related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface ICheckPermission
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="resourceCodes"></param>
        /// <param name="adminUserInfoId"></param>
        /// <returns></returns>
        Task<bool> HasPermissionAsync(string[] resourceCodes, int adminUserInfoId);
    }
}
