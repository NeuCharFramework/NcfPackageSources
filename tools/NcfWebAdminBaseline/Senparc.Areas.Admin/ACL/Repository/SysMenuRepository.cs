/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SysMenuRepository.cs
    File description: SysMenuRepository related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

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
    /// <summary>Defines administrator menu projection queries.</summary>
    public interface ISysMenuRepository : IClientRepositoryBase<SysMenu>
    {
        /// <summary>Gets visible menu DTOs granted to an administrator account.</summary>
        /// <param name="accountId">The administrator account identifier.</param>
        /// <returns>The visible menu DTO sequence.</returns>
        Task<IEnumerable<SysMenuDto>> GetVisibleMenuDtosAsync(int accountId);

        /// <summary>Gets all menus, optionally including button entries.</summary>
        /// <param name="hasButton">Whether button menu types are included.</param>
        /// <returns>The projected menu DTO sequence.</returns>
        Task<IEnumerable<SysMenuDto>> GetAllMenuDtosAsync(bool hasButton);
    }

    public class SysMenuRepository : ClientRepositoryBase<SysMenu>, ISysMenuRepository
    {
        /// <summary>Initializes the menu repository.</summary>
        /// <param name="ncfDbData">The NCF database abstraction.</param>
        public SysMenuRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }

        /// <inheritdoc />
        public async Task<IEnumerable<SysMenuDto>> GetAllMenuDtosAsync(bool hasButton)
        {
            var query = BaseDB.BaseDataContext.Set<SysMenu>().Where(o => true);
            if (!hasButton)
            {
                query = query.Where(menu => menu.MenuType < MenuType.按钮);
            }
            var menus = query.Select(menu => new SysMenuDto()
            {
                Id = menu.Id,
                MenuType = menu.MenuType,
                AddTime = menu.AddTime,
                AdminRemark = menu.AdminRemark,
                Flag = menu.Flag,
                Icon = menu.Icon,
                IsLocked = menu.IsLocked,
                LastUpdateTime = menu.LastUpdateTime,
                MenuName = menu.MenuName,
                ParentId = menu.ParentId,
                Remark = menu.Remark,
                ResourceCode = menu.ResourceCode,
                Sort = menu.Sort,
                Url = menu.Url,
                Visible = menu.Visible
            });
            ;
            return await menus.ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SysMenuDto>> GetVisibleMenuDtosAsync(int accountId)
        {
            var sysRoleAdminUserInfos = BaseDB.BaseDataContext.Set<SysRoleAdminUserInfo>()
                .Where(sysRoleAdminUserInfo => sysRoleAdminUserInfo.AccountId == accountId);
            var sysRoles = BaseDB.BaseDataContext.Set<SysRole>().Where(role => role.Enabled);
            IQueryable<string> roleIdQuery = sysRoleAdminUserInfos.Select(_ => _.RoleId);
            var permissions = BaseDB.BaseDataContext.Set<SysRolePermission>().Where(rolePermission => roleIdQuery.Contains(rolePermission.RoleId));
            IQueryable<string> menuIds = permissions.Select(o => o.PermissionId);
            var menus = BaseDB.BaseDataContext.Set<SysMenu>()
                .Where(menu => menu.Visible && menuIds.Contains(menu.Id) && menu.MenuType == MenuType.菜单)
                .Select(menu => new SysMenuDto()
                {
                    Id = menu.Id,
                    MenuType = menu.MenuType,
                    AddTime = menu.AddTime,
                    AdminRemark = menu.AdminRemark,
                    Flag = menu.Flag,
                    Icon = menu.Icon,
                    IsLocked = menu.IsLocked,
                    LastUpdateTime = menu.LastUpdateTime,
                    MenuName = menu.MenuName,
                    ParentId = menu.ParentId,
                    Remark = menu.Remark,
                    ResourceCode = menu.ResourceCode,
                    Sort = menu.Sort,
                    Url = menu.Url,
                    Visible = menu.Visible
                });
            ;
            return await menus.ToListAsync();
        }
    }
}
