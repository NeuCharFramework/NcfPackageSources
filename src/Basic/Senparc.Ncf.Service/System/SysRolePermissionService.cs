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
    public class SysRolePermissionService : ClientServiceBase<SysRolePermission>, Core.Authorization.ICheckPermission
    {
        private readonly IDistributedCache _distributedCache;

        public SysMenuService _sysMenuService { get; }

        private readonly IAdminWorkContextProvider _adminWorkContextProvider;
        private readonly SysRoleService _sysRoleService;
        private const string PermissionKey = "Permission";
        private readonly ISysRolePermissionRepository _repo;
        public SysRolePermissionService(ISysRolePermissionRepository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            this._distributedCache = _serviceProvider.GetService<IDistributedCache>();
            this._sysMenuService = _serviceProvider.GetService<SysMenuService>();
            this._adminWorkContextProvider = _serviceProvider.GetService<IAdminWorkContextProvider>();
            this._sysRoleService = _serviceProvider.GetService<SysRoleService>();
            _repo = repo;
        }

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

        public async Task<IEnumerable<SysPermissionDto>> DbToCacheAsync()
        {
            IEnumerable<SysRolePermission> permissions = await GetFullListAsync(_ => true);
            IEnumerable<SysPermissionDto> permissionDtos = Mapper.Map<IEnumerable<SysPermissionDto>>(permissions);
            await _distributedCache.RemoveAsync(PermissionKey);
            await _distributedCache.SetStringAsync(PermissionKey, Newtonsoft.Json.JsonConvert.SerializeObject(permissionDtos));
            return permissionDtos;
        }

        public async Task InitPermissionCache()
        {
            await DbToCacheAsync();
            await _sysMenuService.GetMenuDtoByCacheAsync(true);
        }

        /// <summary>
        ///Add permission information
        /// </summary>
        /// <param name="sysMenuDto"></param>
        /// <returns></returns>
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
            #region 正确方式1 传统方式
            //var db = BaseData.BaseDB.BaseDataContext;
            //IEnumerable<SysPermission> entitis = await db.Set<SysPermission>().Where(o => o.RoleId == sysMenuDto.FirstOrDefault().RoleId).ToListAsync();
            //db.Set<SysPermission>().RemoveRange(entitis);
            //db.Set<SysPermission>().AddRange(sysRoleMenus);
            //await db.SaveChangesAsync();
            //await DbToCacheAsync();//temporarily 
            #endregion

            #region 异常方法
            //await BeginTransactionAsync(async () =>
            //{
            //    IEnumerable<SysPermission> entitis = await GetFullListAsync(_ => _.RoleId == sysMenuDto.FirstOrDefault().RoleId);
            //    await DeleteAllAsync(entitis);
            //    await SaveObjectListAsync(sysRoleMenus);
            //    await DbToCacheAsync();//temporarily
            //}); 
            #endregion
            #region 正确方式2 多次调用DbContext.SaveChanges 方式
            // https://docs.microsoft.com/zh-cn/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions
            await ServiceBase.ResilientTransaction.New(BaseData.BaseDB.BaseDataContext).ExecuteAsync(async () =>
            {
                IEnumerable<SysRolePermission> entitis = await GetFullListAsync(_ => _.RoleId == sysMenuDto.FirstOrDefault().RoleId);
                await DeleteAllAsync(entitis); // SaveChangeAsync will be called here
                await SaveObjectListAsync(sysRoleMenus); // SaveChangeAsync will be called here
                await DbToCacheAsync();//temporary
            });
            #endregion
        }

        /// <summary>
        /// Get the permissions of the current user
        /// </summary>
        /// <returns></returns>
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
        ///Verify permissions
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
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
        /// Get the menu (visible) tree structure visible to the current user
        /// </summary>
        /// <param name="isRefresh"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetCurrentUserMenuTreeDtoAsync()
        {
            //IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            int currentAdminId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            SenparcEntitiesBase db = _serviceProvider.GetService<SenparcEntitiesBase>();
            List<SysMenuDto> sysMenuDtos = await GetUserPermissions(currentAdminId).Where(_ => _.MenuType == MenuType.菜单).OrderByDescending(_ => _.Sort).ToListAsync();
            return _sysMenuService.GetSysMenuTreesMainRecursive(sysMenuDtos);
        }

        /// <summary>
        /// Get the menu that the current user can see (visible)
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetCurrentUserMenuDtoAsync(MenuType menuType = MenuType.菜单)
        {
            //IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            int currentAdminId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            SenparcEntitiesBase db = _serviceProvider.GetService<SenparcEntitiesBase>();
            List<SysMenuDto> sysMenuDtos = await GetUserPermissions(currentAdminId).Where(_ => _.MenuType == menuType).OrderByDescending(_ => _.Sort).ToListAsync();
            return sysMenuDtos;// _sysMenuService.GetSysMenuTreesMainRecursive(sysMenuDtos);
        }

        /// <summary>
        /// Get all resources visible to the user (except menus)
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetCurrentUserResourcesDtoAsync()
        {
            //IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            int currentAdminId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            SenparcEntitiesBase db = _serviceProvider.GetService<SenparcEntitiesBase>();
            List<SysMenuDto> sysMenuDtos = await GetUserPermissions(currentAdminId).Where(_ => _.MenuType > MenuType.菜单).OrderByDescending(_ => _.Sort).ToListAsync();
            return sysMenuDtos;// _sysMenuService.GetSysMenuTreesMainRecursive(sysMenuDtos);
        }

        /// <summary>
        /// Get user permissions (including buttons)
        /// </summary>
        /// <param name="currentAdminUserId"></param>
        /// <returns></returns>
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
        /// Check whether the current user has permissions
        /// </summary>
        /// <param name="resourceCodes">Resource codes</param>
        /// <param name="adminUserInfoId">Current user Id</param>
        /// <returns></returns>
        public async Task<bool> HasPermissionAsync(string[] resourceCodes, int adminUserInfoId)
        {
            if (!resourceCodes.Any())
            {
                return false;
            }
            string cacheKey = string.Format("adminUserInfoPermission:{0}", adminUserInfoId); // Cache all resources of the current user
            var distributedCache = _serviceProvider.GetService<IDistributedCache>();
            string codesJsonValue = await distributedCache.GetStringAsync(cacheKey); // Try to read the user's resource from cache
            IEnumerable<string> codes = null;
            if (string.IsNullOrEmpty(codesJsonValue))
            {
                codes = await _repo.GetAllResouceCodesByAccountIdAsync(adminUserInfoId);
                codesJsonValue = Newtonsoft.Json.JsonConvert.SerializeObject(codes);
                await distributedCache.SetStringAsync(cacheKey, codesJsonValue, new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromHours(8)
                }); // Cache for 8 hours
            }
            else
            {
                codes = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<string>>(codesJsonValue);
            }
            if (!codes.Any())
            {
                return false;
            }
            //Get all resource codes of the current user
            return codes.Intersect(resourceCodes).Any();
        }
    }
}
