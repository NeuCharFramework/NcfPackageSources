/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SysRolePermissionRepository.cs
    文件功能描述：SysRolePermissionRepository 相关实现
    
    
    创建标识：Senparc - 20211223
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        /// 获取某个用户下面的所有资源
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
        /// 获取某个用户下面的所有资源
        /// </summary>
        /// <param name="adminUserInfoId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetAllResouceCodesByAccountIdAsync(int adminUserInfoId)
        {
            IQueryable<string> roleIdQuery = BaseDB.BaseDataContext.Set<SysRoleAdminUserInfo>().Where(sra => sra.AccountId == adminUserInfoId).Select(_ => _.RoleId); // 查询角色
            var menuIdQuery = BaseDB.BaseDataContext.Set<SysRolePermission>().Where(srp => roleIdQuery.Contains(srp.RoleId)).Select(srp => srp.PermissionId); // 查询所拥有的菜单
            var resourceCodes = await BaseDB.BaseDataContext.Set<SysMenu>().Where(menu => menuIdQuery.Contains(menu.Id))
                .Select(menu => menu.ResourceCode)
                .ToListAsync();
            return resourceCodes.Where(code => !string.IsNullOrEmpty(code)).ToList();
        }
    }
}
