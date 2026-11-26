using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Senparc.Core.Models.DataBaseModel;
using Senparc.Core.WorkContext.Provider;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Service
{
    /// <summary>
    /// Resolves, caches, and updates administrator menu and button permissions.
    /// </summary>
    public class SysPermissionService : ClientServiceBase<SysPermission>
    {
        private readonly IDistributedCache _distributedCache;

        public SysMenuService _sysMenuService { get; }

        private readonly IAdminWorkContextProvider _adminWorkContextProvider;
        private readonly SysRoleService _sysRoleService;
        private const string PermissionKey = "Permission";

        /// <summary>
        /// Initializes the permission service and related menu/role dependencies.
        /// </summary>
        /// <param name="repo">The permission repository.</param>
        /// <param name="serviceProvider">The service provider for related services.</param>
        public SysPermissionService(ClientRepositoryBase<SysPermission> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            this._distributedCache = _serviceProvider.GetService<IDistributedCache>();
            this._sysMenuService = _serviceProvider.GetService<SysMenuService>();
            this._adminWorkContextProvider = _serviceProvider.GetService<IAdminWorkContextProvider>();
            this._sysRoleService = _serviceProvider.GetService<SysRoleService>();
        }

        /// <summary>Determines whether the current administrator can access a menu URL.</summary>
        /// <param name="url">The menu URL to check.</param>
        /// <returns><see langword="true"/> when a role permission matches the URL.</returns>
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
            var urls = from menu in sysMenus
                       join permission in permissions on menu.Id equals permission.PermissionId
                       where !string.IsNullOrEmpty(menu.Url)
                       select menu.Url.ToLower();

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

        /// <summary>Loads all persisted permissions and refreshes the distributed permission cache.</summary>
        /// <returns>The mapped permission DTOs stored in cache.</returns>
        public async Task<IEnumerable<SysPermissionDto>> DbToCacheAsync()
        {
            IEnumerable<SysPermission> permissions = await GetFullListAsync(_ => true);
            IEnumerable<SysPermissionDto> permissionDtos = Mapper.Map<IEnumerable<SysPermissionDto>>(permissions);
            await _distributedCache.RemoveAsync(PermissionKey);
            await _distributedCache.SetStringAsync(PermissionKey, Newtonsoft.Json.JsonConvert.SerializeObject(permissionDtos));
            return permissionDtos;
        }

        /// <summary>Refreshes permission and menu cache projections.</summary>
        public async Task InitPermissionCache()
        {
            await DbToCacheAsync();
            await _sysMenuService.GetMenuDtoByCacheAsync(true);
        }

        /// <summary>Replaces all permission assignments for the role in the supplied DTOs.</summary>
        /// <param name="sysMenuDto">The permission DTOs containing the target role.</param>
        /// <returns>A task that completes after the transaction and cache refresh.</returns>
        public async Task AddAsync(IEnumerable<SysPermissionDto> sysMenuDto)
        {
            List<SysPermission> sysRoleMenus = new List<SysPermission>();
            string RoleId = sysMenuDto.FirstOrDefault().RoleId;
            SysRole sysRole = await _sysRoleService.GetObjectAsync(_ => _.Id == RoleId);
            //
            foreach (var item in sysMenuDto)
            {
                item.RoleCode = sysRole.RoleCode;
                SysPermission sysPermission = new SysPermission(item);
                sysRoleMenus.Add(sysPermission);
            }

            await BeginTransactionAsync(async () => 
            {
                IEnumerable<SysPermission> entitis = await GetFullListAsync(_ => _.RoleId == sysMenuDto.FirstOrDefault().RoleId);
                await DeleteAllAsync(entitis);
                await SaveObjectListAsync(sysRoleMenus);
                await DbToCacheAsync();//temporarily
            });
        }

        /// <summary>Gets permission DTOs belonging to the current administrator's roles.</summary>
        /// <returns>The current administrator's effective permissions.</returns>
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

        /// <summary>Determines whether a button resource code is allowed for a menu URL.</summary>
        /// <param name="code">The button resource code.</param>
        /// <param name="url">The parent menu URL.</param>
        /// <returns><see langword="true"/> when the current roles grant the button.</returns>
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

        /// <summary>Builds the visible menu tree allowed for the current administrator.</summary>
        /// <returns>The effective visible menu tree.</returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetMenuTreeDtoByCacheAsync()
        {
            //IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            IEnumerable<SysMenuDto> sysMenuDtos = await _sysMenuService.GetMenuDtoByCacheAsync();
            IEnumerable<SysPermissionDto> permissions = await GetUserSysPermissionDtosAsync();
            var urls = from menu in sysMenuDtos
                       join permission in permissions on menu.Id equals permission.PermissionId
                       where menu.Visible
                       select menu;
            return _sysMenuService.GetSysMenuTreesMainRecursive(urls);
        }
    }
}
