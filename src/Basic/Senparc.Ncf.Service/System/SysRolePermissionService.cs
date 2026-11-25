/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRolePermissionService.cs
    File description: SysRolePermissionService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Core.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Senparc.Respository;

namespace Senparc.Ncf.Service
{
    /// <summary>
    /// Resolves and persists role-based menu and button permissions, including their distributed cache.
    /// </summary>
    public class SysRolePermissionService : ClientServiceBase<SysRolePermission>, Core.Authorization.ICheckPermission
    {
        private readonly IDistributedCache _distributedCache;

        public SysMenuService _sysMenuService { get; }

        private readonly IAdminWorkContextProvider _adminWorkContextProvider;
        private readonly SysRoleService _sysRoleService;
        private const string PermissionKey = "Permission";
        private readonly ISysRolePermissionRepository _repo;
        /// <summary>
        /// Initializes the role-permission service and resolves its menu, role, cache,
        /// and administrator context dependencies.
        /// </summary>
        /// <param name="repo">The repository used to persist role permissions.</param>
        /// <param name="serviceProvider">The provider used to resolve related services.</param>
        public SysRolePermissionService(ISysRolePermissionRepository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            this._distributedCache = _serviceProvider.GetService<IDistributedCache>();
            this._sysMenuService = _serviceProvider.GetService<SysMenuService>();
            this._adminWorkContextProvider = _serviceProvider.GetService<IAdminWorkContextProvider>();
            this._sysRoleService = _serviceProvider.GetService<SysRoleService>();
            _repo = repo;
        }

        /// <summary>
        /// Checks whether the current administrator context has a cached permission associated
        /// with the requested menu URL.
        /// </summary>
        /// <param name="url">The URL to compare after removing its query string.</param>
        /// <returns><see langword="true"/> when a matching permitted menu URL is found; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> HasPermissionAsync(string url)
        {
            IEnumerable<string> roleIds = _adminWorkContextProvider.GetAdminWorkContext().RoleCodes;
            string str = await _distributedCache.GetStringAsync(PermissionKey);
            IEnumerable<SysPermissionDto> permissions;
            if (string.IsNullOrEmpty(str))
            {
                permissions = await DbToCacheAsync();
            }
            else
            {
                permissions = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<SysPermissionDto>>(str);
            }
            permissions.Where(_ => roleIds.Contains(_.RoleCode));
            if (!permissions.Any())
            {
                return false;
            }
            IEnumerable<SysMenuDto> sysMenus = await _sysMenuService.GetMenuDtoByCacheAsync();

            Func<string, string> getUrlPath = u =>
            {
                var index = u.IndexOf("?");
                return index >= 0 ? u.Substring(0, index) : u;
            };

            var urls = from menu in sysMenus
                       join permission in permissions on menu.Id equals permission.PermissionId
                       where !string.IsNullOrEmpty(menu.Url)
                       select getUrlPath(menu.Url).ToLower();

            if (!urls.Any() || !urls.Contains(url.ToLower()))
            {
                return false;
            }
            return true;
        }

        //public async Task<IEnumerable<SysMenuDto>> GetUserMenuDtosAsync()
        //{
        //    IEnumerable<string> roleIds = _adminWorkContextProvider.GetAdminWorkContext().RoleCodes;
        //    string str = await _distributedCache.GetStringAsync(PermissionKey);
        //    IEnumerable<SysPermissionDto> permissions = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<SysPermissionDto>>(str).Where(_ => roleIds.Contains(_.RoleCode));
        //    IEnumerable<SysMenuDto> sysMenus = await _sysMenuService.GetMenuDtoByCacheAsync();
        //    var urls = from menu in sysMenus
        //               join permission in permissions on menu.Id equals permission.PermissionId
        //               where !string.IsNullOrEmpty(menu.Url)
        //               select menu;

        //    return urls;
        //}

        /// <summary>
        /// Loads all persisted role permissions, maps them to DTOs, and replaces the distributed
        /// permission cache entry.
        /// </summary>
        /// <returns>A task whose result contains the permissions written to the cache.</returns>
        public async Task<IEnumerable<SysPermissionDto>> DbToCacheAsync()
        {
            IEnumerable<SysRolePermission> permissions = await GetFullListAsync(_ => true);
            IEnumerable<SysPermissionDto> permissionDtos = Mapper.Map<IEnumerable<SysPermissionDto>>(permissions);
            await _distributedCache.RemoveAsync(PermissionKey);
            await _distributedCache.SetStringAsync(PermissionKey, Newtonsoft.Json.JsonConvert.SerializeObject(permissionDtos));
            return permissionDtos;
        }

        /// <summary>
        /// Rebuilds the permission cache and forces the menu cache to rebuild as well.
        /// </summary>
        /// <returns>A task that completes after both cache refresh operations finish.</returns>
        public async Task InitPermissionCache()
        {
            await DbToCacheAsync();
            await _sysMenuService.GetMenuDtoByCacheAsync(true);
        }

        /// <summary>
        /// Replaces the persisted permissions for the role represented by the supplied DTOs,
        /// updates each DTO's role code, and refreshes the permission cache inside a resilient transaction.
        /// </summary>
        /// <param name="sysMenuDto">The permission DTOs to persist for one role.</param>
        /// <returns>A task that completes after the replacement and cache refresh succeed.</returns>
        public async Task AddAsync(IEnumerable<SysPermissionDto> sysMenuDto)
        {
            List<SysRolePermission> sysRoleMenus = new List<SysRolePermission>();
            string roleId = sysMenuDto.FirstOrDefault().RoleId;
            SysRole sysRole = await _sysRoleService.GetObjectAsync(_ => _.Id == roleId);
            //
            foreach (var item in sysMenuDto)
            {
                item.RoleCode = sysRole.RoleCode;
                SysRolePermission sysPermission = new SysRolePermission(item);
                sysRoleMenus.Add(sysPermission);
            }
            #region Legacy section
            //var db = BaseData.BaseDB.BaseDataContext;
            //IEnumerable<SysPermission> entitis = await db.Set<SysPermission>().Where(o => o.RoleId == sysMenuDto.FirstOrDefault().RoleId).ToListAsync();
            //db.Set<SysPermission>().RemoveRange(entitis);
            //db.Set<SysPermission>().AddRange(sysRoleMenus);
            //await db.SaveChangesAsync();
            //await DbToCacheAsync();//temporarily 
            #endregion

            #region Legacy section
            //await BeginTransactionAsync(async () =>
            //{
            //    IEnumerable<SysPermission> entitis = await GetFullListAsync(_ => _.RoleId == sysMenuDto.FirstOrDefault().RoleId);
            //    await DeleteAllAsync(entitis);
            //    await SaveObjectListAsync(sysRoleMenus);
            //    await DbToCacheAsync();//temporarily
            //}); 
            #endregion
            #region Legacy section
            // https://docs.microsoft.com/zh-cn/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions
            await ServiceBase.ResilientTransaction.New(BaseData.BaseDB.BaseDataContext).ExecuteAsync(async () =>
            {
                IEnumerable<SysRolePermission> entitis = await GetFullListAsync(_ => _.RoleId == sysMenuDto.FirstOrDefault().RoleId);
                await DeleteAllAsync(entitis);
                await SaveObjectListAsync(sysRoleMenus);
                await DbToCacheAsync();//temporarily
            });
            #endregion
        }

        /// <summary>
        /// Gets the permission DTOs associated with the current administrator's role codes,
        /// loading the distributed permission cache from the database when it is absent.
        /// </summary>
        /// <returns>A task whose result contains the current administrator's permissions.</returns>
        public async Task<IEnumerable<SysPermissionDto>> GetUserSysPermissionDtosAsync()
        {
            IEnumerable<string> roleIds = _adminWorkContextProvider.GetAdminWorkContext().RoleCodes;
            string str = await _distributedCache.GetStringAsync(PermissionKey);
            IEnumerable<SysPermissionDto> permissions;
            if (string.IsNullOrEmpty(str))
            {
                permissions = await DbToCacheAsync();
            }
            else
            {
                permissions = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<SysPermissionDto>>(str);
            }
            return permissions.Where(_ => roleIds.Contains(_.RoleCode));
        }

        /// <summary>
        /// Checks whether a button resource code is associated with a menu URL for the current
        /// administrator context. This compatibility API is superseded by the newer overload.
        /// </summary>
        /// <param name="code">The button resource code to locate.</param>
        /// <param name="url">The menu URL whose child buttons should be checked.</param>
        /// <returns><see langword="true"/> when the code is found under the requested menu; otherwise, <see langword="false"/>.</returns>
        [Obsolete("HasPermissionAsync(IEnumerable<string> codes, string url, bool isAjax)")]
        public async Task<bool> HasPermissionByButtonCodeAsync(string code, string url)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            IEnumerable<string> roleIds = _adminWorkContextProvider.GetAdminWorkContext().RoleCodes;
            string str = await _distributedCache.GetStringAsync(PermissionKey);
            IEnumerable<SysPermissionDto> permissions;
            if (string.IsNullOrEmpty(str))
            {
                permissions = await DbToCacheAsync();
            }
            else
            {
                permissions = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<SysPermissionDto>>(str);
            }
            permissions.Where(_ => roleIds.Contains(_.RoleCode));
            if (!permissions.Any())
            {
                return false;
            }
            IEnumerable<SysMenuDto> sysMenus = await _sysMenuService.GetMenuDtoByCacheAsync();
            var resourceCodes = from menu in sysMenus
                                join permission in permissions on menu.Id equals permission.PermissionId
                                where !string.IsNullOrEmpty(menu.ResourceCode) && !permission.IsMenu
                                select menu.ResourceCode;

            IEnumerable<SysMenuDto> sysMenuDtos = await _sysMenuService.GetMenuDtoByCacheAsync();
            SysMenuDto sysMenuDto = sysMenuDtos.FirstOrDefault(_ => _.Url?.ToLower() == url.ToLower() && _.IsMenu);
            if (sysMenuDto == null)
            {
                return false;
            }
            bool isInUrl = sysMenuDtos.Any(_ => _.ParentId == sysMenuDto.Id && !_.IsMenu);
            bool has = resourceCodes.Any(_ => _ == code);
            return has && isInUrl;
        }

        /// <summary>
        /// Checks the current administrator's permissions against either resource codes
        /// (for AJAX requests) or a menu URL (for ordinary page requests).
        /// </summary>
        /// <param name="codes">The resource codes presented by an AJAX request.</param>
        /// <param name="url">The page URL checked for a non-AJAX request.</param>
        /// <param name="isAjax">Whether to compare resource codes instead of a menu URL.</param>
        /// <returns><see langword="true"/> when the requested permission is present; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> HasPermissionAsync(IEnumerable<string> codes, string url, bool isAjax)
        {
            int adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            IQueryable<SysMenuDto> permissions = GetUserPermissions(adminUserId);
            if (isAjax)
            {
                var userPermissionCodes = await permissions.Where(_ => _.ResourceCode != string.Empty).Select(_ => _.ResourceCode).Distinct().ToListAsync();
                bool result = codes.Intersect(userPermissionCodes).Any();
                return result;
            }
            else
            {
                return await permissions.AnyAsync(_ => _.Url == url && _.ResourceCode == string.Empty);
            }
        }

        /// <summary>
        /// Gets the current administrator's visible menu permissions and converts them into
        /// a hierarchical menu tree.
        /// </summary>
        /// <returns>A task whose result contains the current user's menu tree.</returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetCurrentUserMenuTreeDtoAsync()
        {
            //IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            int currentAdminId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            SenparcEntitiesBase db = _serviceProvider.GetService<SenparcEntitiesBase>();
            List<SysMenuDto> sysMenuDtos = await GetUserPermissions(currentAdminId).Where(_ => _.MenuType == MenuType.菜单).OrderByDescending(_ => _.Sort).ToListAsync();
            return _sysMenuService.GetSysMenuTreesMainRecursive(sysMenuDtos);
        }

        /// <summary>
        /// Gets the current administrator's visible menu DTOs for the requested menu type.
        /// </summary>
        /// <param name="menuType">The menu type to select.</param>
        /// <returns>A task whose result contains the current user's matching menus.</returns>
        public async Task<IEnumerable<SysMenuDto>> GetCurrentUserMenuDtoAsync(MenuType menuType = MenuType.菜单)
        {
            //IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            int currentAdminId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            SenparcEntitiesBase db = _serviceProvider.GetService<SenparcEntitiesBase>();
            List<SysMenuDto> sysMenuDtos = await GetUserPermissions(currentAdminId).Where(_ => _.MenuType == menuType).OrderByDescending(_ => _.Sort).ToListAsync();
            return sysMenuDtos;// _sysMenuService.GetSysMenuTreesMainRecursive(sysMenuDtos);
        }

        /// <summary>
        /// Gets the current administrator's visible non-menu resources, such as button permissions.
        /// </summary>
        /// <returns>A task whose result contains the current user's resource DTOs.</returns>
        public async Task<IEnumerable<SysMenuDto>> GetCurrentUserResourcesDtoAsync()
        {
            //IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            int currentAdminId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            SenparcEntitiesBase db = _serviceProvider.GetService<SenparcEntitiesBase>();
            List<SysMenuDto> sysMenuDtos = await GetUserPermissions(currentAdminId).Where(_ => _.MenuType > MenuType.菜单).OrderByDescending(_ => _.Sort).ToListAsync();
            return sysMenuDtos;// _sysMenuService.GetSysMenuTreesMainRecursive(sysMenuDtos);
        }

        /// <summary>
        /// Builds a query for visible menus and resources granted through enabled roles
        /// associated with an administrator account.
        /// </summary>
        /// <param name="currentAdminUserId">The administrator account identifier.</param>
        /// <returns>An unexecuted query projected to <see cref="SysMenuDto"/>.</returns>
        private IQueryable<SysMenuDto> GetUserPermissions(int currentAdminUserId)
        {
            IConfigurationProvider autoMapConfigurationProvider = _serviceProvider.GetService<IMapper>().ConfigurationProvider;
            SenparcEntitiesBase db = _serviceProvider.GetService<SenparcEntitiesBase>();
            IQueryable<string> roleIds = from roleAdmin in db.Set<SysRoleAdminUserInfo>()
                                         where roleAdmin.AccountId == currentAdminUserId && db.Set<SysRole>().Any(role => role.Id == roleAdmin.RoleId && role.Enabled)
                                         select roleAdmin.RoleId;// db.SysRoleAdminUserInfos.Where(_ => _.AccountId == currentAdminUserId).Select(_ => _.RoleId);
            IQueryable<string> menuIds = from permission in db.Set<SysRolePermission>()
                                         where roleIds.Any(_ => _ == permission.RoleId)
                                         select permission.PermissionId;
            return db.Set<SysMenu>().Where(_ => _.Visible && menuIds.Contains(_.Id)).ProjectTo<SysMenuDto>(autoMapConfigurationProvider);
        }

        /// <summary>
        /// Checks whether an administrator has at least one of the requested resource codes,
        /// loading and caching the account's complete resource-code set when necessary.
        /// </summary>
        /// <param name="resourceCodes">The resource codes to match.</param>
        /// <param name="adminUserInfoId">The administrator account identifier.</param>
        /// <returns><see langword="true"/> when any requested code is granted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> HasPermissionAsync(string[] resourceCodes, int adminUserInfoId)
        {
            if (!resourceCodes.Any())
            {
                return false;
            }
            string cacheKey = string.Format("adminUserInfoPermission:{0}", adminUserInfoId);
            var distributedCache = _serviceProvider.GetService<IDistributedCache>();
            string codesJsonValue = await distributedCache.GetStringAsync(cacheKey);
            IEnumerable<string> codes = null;
            if (string.IsNullOrEmpty(codesJsonValue))
            {
                codes = await _repo.GetAllResouceCodesByAccountIdAsync(adminUserInfoId);
                codesJsonValue = Newtonsoft.Json.JsonConvert.SerializeObject(codes);
                await distributedCache.SetStringAsync(cacheKey, codesJsonValue, new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromHours(8)
                });
            }
            else
            {
                codes = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<string>>(codesJsonValue);
            }
            if (!codes.Any())
            {
                return false;
            }
            return codes.Intersect(resourceCodes).Any();
        }
    }
}
