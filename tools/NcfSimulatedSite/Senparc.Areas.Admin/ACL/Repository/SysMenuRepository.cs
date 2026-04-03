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
    public interface ISysMenuRepository : IClientRepositoryBase<SysMenu>
    {
        /// <summary>
        /// Get the user-visible menu
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        Task<IEnumerable<SysMenuDto>> GetVisibleMenuDtosAsync(int accountId);

        /// <summary>
        /// Get all menus (only menus and pages)
        /// <param name="hasButton"></param>
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SysMenuDto>> GetAllMenuDtosAsync(bool hasButton);
    }

    public class SysMenuRepository : ClientRepositoryBase<SysMenu>, ISysMenuRepository
    {
        public SysMenuRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }

        /// <summary>
        /// Get the menu visible to the user
        /// </summary>
        /// <param name="hasButton"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetAllMenuDtosAsync(bool hasButton)
        {
            var query = BaseDB.BaseDataContext.Set<SysMenu>().Where(o => true);
            if (!hasButton)
            {
                query = query.Where(menu => menu.MenuType < MenuType.按钮); // Get the visible menu
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

        /// <summary>
        /// Get the menu visible to the user
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetVisibleMenuDtosAsync(int accountId)
        {
            var sysRoleAdminUserInfos = BaseDB.BaseDataContext.Set<SysRoleAdminUserInfo>()
                .Where(sysRoleAdminUserInfo => sysRoleAdminUserInfo.AccountId == accountId); // Get user role information
            var sysRoles = BaseDB.BaseDataContext.Set<SysRole>().Where(role => role.Enabled); // Get valid role information
            IQueryable<string> roleIdQuery = sysRoleAdminUserInfos.Select(_ => _.RoleId); // Get the user's role ID
            var permissions = BaseDB.BaseDataContext.Set<SysRolePermission>().Where(rolePermission => roleIdQuery.Contains(rolePermission.RoleId));
            IQueryable<string> menuIds = permissions.Select(o => o.PermissionId);
            var menus = BaseDB.BaseDataContext.Set<SysMenu>()
                .Where(menu => menu.Visible && menuIds.Contains(menu.Id) && menu.MenuType == MenuType.菜单) // Get the visible menu
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
