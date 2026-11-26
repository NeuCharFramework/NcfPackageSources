/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRolePermissionRepository.cs
    File description: SysRolePermissionRepository related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview12 Updated repository infrastructure and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Respository
{
    public interface ISysRolePermissionRepository: IClientRepositoryBase<SysRolePermission>
    {
        /// <summary>
        /// Returns the distinct resource codes granted to an administrator through
        /// the roles and menu permissions assigned to the account.
        /// </summary>
        /// <param name="adminUserInfoId">The administrator account identifier.</param>
        /// <returns>The non-empty resource codes granted to the account.</returns>
        Task<IEnumerable<string>> GetAllResouceCodesByAccountIdAsync(int adminUserInfoId);
    }

    public class SysRolePermissionRepository : ClientRepositoryBase<SysRolePermission>, ISysRolePermissionRepository
    {
        /// <summary>
        /// Initializes the role-permission repository with the NCF database provider.
        /// </summary>
        /// <param name="db">The client database data provider.</param>
        public SysRolePermissionRepository(INcfDbData db) : base(db)
        {
        }

        /// <summary>
        /// Returns the distinct resource codes granted to an administrator through
        /// the roles and menu permissions assigned to the account.
        /// </summary>
        /// <param name="adminUserInfoId">The administrator account identifier.</param>
        /// <returns>The non-empty resource codes granted to the account.</returns>
        public async Task<IEnumerable<string>> GetAllResouceCodesByAccountIdAsync(int adminUserInfoId)
        {
            IQueryable<string> roleIdQuery = BaseDB.BaseDataContext.Set<SysRoleAdminUserInfo>().Where(sra => sra.AccountId == adminUserInfoId).Select(_ => _.RoleId); //Legacy comment translated to English.
            var menuIdQuery = BaseDB.BaseDataContext.Set<SysRolePermission>().Where(srp => roleIdQuery.Contains(srp.RoleId)).Select(srp => srp.PermissionId); //Legacy comment translated to English.
            var resourceCodes = await BaseDB.BaseDataContext.Set<SysMenu>().Where(menu => menuIdQuery.Contains(menu.Id))
                .Select(menu => menu.ResourceCode)
                .ToListAsync();
            return resourceCodes.Where(code => !string.IsNullOrEmpty(code)).ToList();
        }
    }
}
