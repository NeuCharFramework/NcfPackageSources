/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SysRolePermissionRepository.cs
    File description: SysRolePermissionRepository related implementation

    Created by: Senparc - 20211223

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

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

namespace Senparc.Areas.Admin.ACL.Repository
{
    public interface ISysRolePermissionRepository: IClientRepositoryBase<SysRolePermission>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="adminUserInfoId"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetAllResouceCodesByAccountIdAsync(int adminUserInfoId);
    }

    public class SysRolePermissionRepository : ClientRepositoryBase<SysRolePermission>, ISysRolePermissionRepository
    {
        public SysRolePermissionRepository(INcfDbData db) : base(db)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="adminUserInfoId"></param>
        /// <returns></returns>
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
