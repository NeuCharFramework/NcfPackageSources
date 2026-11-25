/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysMenuService.cs
    File description: SysMenuService related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.23.10-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Distributed;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Exceptions;
using AutoMapper.QueryableExtensions;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Service;
using Microsoft.AspNetCore.Authentication;

namespace Senparc.Ncf.Service
{
    /// <summary>
    /// Provides persistence, caching, tenant propagation, and initialization operations
    /// for system menus and their button permissions.
    /// </summary>
    public class SysMenuService : ClientServiceBase<SysMenu>
    {
        private readonly SysButtonService _sysButtonService;
        private readonly IDistributedCache _distributedCache;
        private readonly SysRoleService sysRoleService;
        private readonly ClientRepositoryBase<SysRolePermission> sysRolePermissionService;
        private readonly SysRoleAdminUserInfoService sysRoleAdminUserInfoService;
        public const string MenuCacheKey = "AllMenus";
        public const string MenuTreeCacheKey = "AllMenusTree";

        //private readonly SenparcEntitiesBase _senparcEntities;

        /// <summary>
        /// Initializes the menu service and connects the related button, role, permission,
        /// and role-admin services used by menu operations.
        /// </summary>
        /// <param name="repo">The repository used to persist <see cref="SysMenu"/> entities.</param>
        /// <param name="serviceProvider">The provider used to resolve shared infrastructure such as <see cref="IDistributedCache"/>.</param>
        /// <param name="sysRoleService">The role service used during system initialization.</param>
        /// <param name="sysRolePermissionService">The repository used to persist role permissions.</param>
        /// <param name="sysRoleAdminUserInfoService">The service used to associate administrator accounts with roles.</param>
        /// <param name="sysButtonService">The service used to manage menu buttons.</param>
        public SysMenuService(ClientRepositoryBase<SysMenu> repo, IServiceProvider serviceProvider,
            SysRoleService sysRoleService, ClientRepositoryBase<SysRolePermission> sysRolePermissionService,
            SysRoleAdminUserInfoService sysRoleAdminUserInfoService, SysButtonService sysButtonService
            ) : base(repo, serviceProvider)
        {
            _sysButtonService = sysButtonService;
            _distributedCache = _serviceProvider.GetService<IDistributedCache>();
            this.sysRoleService = sysRoleService;
            this.sysRolePermissionService = sysRolePermissionService;
            this.sysRoleAdminUserInfoService = sysRoleAdminUserInfoService;
            //_senparcEntities = _serviceProvider.GetService<SenparcEntitiesBase>();
        }

        /// <summary>
        /// Applies the supplied tenant context to this service and every related menu dependency
        /// when multi-tenant mode is enabled.
        /// </summary>
        /// <param name="requestTenantInfo">The tenant context to propagate to the related services.</param>
        public void SetTenantInfoForAllServices(RequestTenantInfo requestTenantInfo)
        {
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                base.SetTenantInfo(requestTenantInfo);
                _sysButtonService.SetTenantInfo(requestTenantInfo);
                sysRoleService.SetTenantInfo(requestTenantInfo);
                (sysRolePermissionService.BaseDB.BaseDataContext as ISenparcEntitiesDbContext).TenantInfo = requestTenantInfo;
                sysRoleAdminUserInfoService.SetTenantInfo(requestTenantInfo);
            }
        }

        /// <summary>
        /// Creates a new menu or updates an existing unlocked menu, validates button resource-code
        /// uniqueness, persists the entity, and refreshes the menu caches.
        /// </summary>
        /// <param name="sysMenuDto">The menu data to create or apply to an existing menu.</param>
        /// <returns>The persisted menu entity. A locked existing menu is returned unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown by downstream code when <paramref name="sysMenuDto"/> is <see langword="null"/>.</exception>
        /// <exception cref="NcfExceptionBase">Thrown when a button menu reuses an existing resource code.</exception>
        public virtual async Task<SysMenu> CreateOrUpdateAsync(SysMenuDto sysMenuDto)
        {
            SysMenu menu;
            ICollection<SysButton> sysButtons = new List<SysButton>();
            bool isRepeat;
            if (!string.IsNullOrEmpty(sysMenuDto.Id))
            {
                menu = await GetObjectAsync(_ => _.Id == sysMenuDto.Id);
                if (menu.IsLocked)
                {
                    return menu;
                }

                isRepeat = await this.GetObjectAsync(_ => _.ResourceCode == sysMenuDto.ResourceCode && _.Id != sysMenuDto.Id) != null;

                //isRepeat = await _serviceProvider.GetService<SenparcEntitiesBase>().Set<SysMenu>().AnyAsync(_ => _.ResourceCode == sysMenuDto.ResourceCode && _.Id != sysMenuDto.Id);

                menu.Update(sysMenuDto);
            }
            else
            {
                menu = new SysMenu(sysMenuDto);

                isRepeat = await this.GetObjectAsync(_ => _.ResourceCode == sysMenuDto.ResourceCode) != null;

                //isRepeat = await _serviceProvider.GetService<SenparcEntitiesBase>().Set<SysMenu>().AnyAsync(_ => _.ResourceCode == sysMenuDto.ResourceCode);
            }
            if (isRepeat && sysMenuDto.MenuType == MenuType.按钮)
            {
                throw new NcfExceptionBase($"ResourceCode：{sysMenuDto.ResourceCode}已重复");
            }
            menu.ResourceCode = sysMenuDto.MenuType == MenuType.按钮 ? menu.ResourceCode : string.Empty;
            await SaveObjectAsync(menu);
            await GetMenuDtoByCacheAsync(true);
            return menu;
        }

        /// <summary>
        /// Creates or updates a menu together with its button definitions in one transaction,
        /// then invalidates the menu caches so subsequent reads observe the persisted state.
        /// </summary>
        /// <param name="sysMenuDto">The menu data to create or apply to an existing menu.</param>
        /// <param name="buttons">The button definitions to create or update for the menu.</param>
        /// <returns>A task that completes after the menu and button changes are committed.</returns>
        public virtual async Task CreateOrUpdateAsync(SysMenuDto sysMenuDto, IEnumerable<SysButtonDto> buttons)
        {
            SysMenu menu;
            List<SysButton> sysButtons = new List<SysButton>();
            if (!string.IsNullOrEmpty(sysMenuDto.Id))
            {
                menu = await GetObjectAsync(_ => _.Id == sysMenuDto.Id);
                menu.Update(sysMenuDto);
            }
            else
            {
                menu = new SysMenu(sysMenuDto);
            }

            IEnumerable<string> modifySysButtons = buttons.Where(_ => !string.IsNullOrEmpty(_.Id)).Select(_ => _.Id);
            IEnumerable<SysButton> updateBUttons;
            if (modifySysButtons.Any())
            {
                updateBUttons = await _sysButtonService.GetFullListAsync(_ => modifySysButtons.Contains(_.Id));
            }
            else
            {
                updateBUttons = new List<SysButton>();
            }
            foreach (var item in buttons)
            {
                if (string.IsNullOrEmpty(item.ButtonName))
                {
                    continue;
                }
                SysButton sysButton;
                if (string.IsNullOrEmpty(item.Id))
                {
                    sysButton = new SysButton(item);
                    sysButton.MenuId = menu.Id;
                }
                else
                {
                    sysButton = updateBUttons.FirstOrDefault(_ => _.Id == item.Id);
                    sysButton.Update(item);
                }
                sysButtons.Add(sysButton);
            }
            await BeginTransactionAsync(async () =>
            {
                if (sysButtons.Any())
                {
                    await _sysButtonService.SaveObjectListAsync(sysButtons);
                }
                await SaveObjectAsync(menu);
            });
            await GetMenuDtoByCacheAsync(true);
        }

        /// <summary>
        /// Removes the flat menu cache entry. The hierarchical cache is rebuilt as part of
        /// the refresh path used by <see cref="GetMenuDtoByCacheAsync(bool)"/>.
        /// </summary>
        /// <returns>A task that completes after both cache entries have been removed.</returns>
        public virtual async Task RemoveMenuAsync()
        {
            await _distributedCache.RemoveAsync(MenuCacheKey);
        }

        /// <summary>
        /// Gets menu and button DTOs from the distributed cache, rebuilding and storing both
        /// the flat list and tree cache when the entry is absent or a refresh is requested.
        /// </summary>
        /// <param name="isRefresh">When <see langword="true"/>, bypasses the existing flat-list cache and rebuilds both menu caches.</param>
        /// <returns>A task whose result contains menu and button DTOs visible to the current service scope.</returns>
        public virtual async Task<IEnumerable<SysMenuDto>> GetMenuDtoByCacheAsync(bool isRefresh = false)
        {
            List<SysMenuDto> selectListItems = null;
            byte[] selectLiteItemBytes = await _distributedCache.GetAsync(MenuCacheKey);
            if (selectLiteItemBytes == null || isRefresh)
            {
                List<SysMenu> sysMenus = (await GetFullListAsync(_ => _.Visible).ConfigureAwait(false)).OrderByDescending(z => z.Sort).ToList();
                List<SysButton> sysButtons = (await _sysButtonService.GetFullListAsync(_ => true).ConfigureAwait(false)).OrderBy(z => z.Id).ToList();
                selectListItems = Mapper.Map<List<SysMenuDto>>(sysMenus);
                List<SysMenuDto> buttons = _sysButtonService.Mapper.Map<List<SysMenuDto>>(sysButtons);
                selectListItems.AddRange(buttons);
                string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(selectListItems);
                await _distributedCache.RemoveAsync(MenuCacheKey);
                await _distributedCache.RemoveAsync(MenuTreeCacheKey);
                await _distributedCache.SetAsync(MenuCacheKey, System.Text.Encoding.UTF8.GetBytes(jsonStr));
                await _distributedCache.SetStringAsync(MenuTreeCacheKey, Newtonsoft.Json.JsonConvert.SerializeObject(GetSysMenuTreesMainRecursive(selectListItems)));
            }
            else
            {
                selectListItems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SysMenuDto>>(System.Text.Encoding.UTF8.GetString(selectLiteItemBytes));
            }
            return selectListItems;
        }

        /// <summary>
        /// Builds the top-level menu tree from a flat menu/button DTO sequence.
        /// Only entries marked as menus participate in the hierarchy.
        /// </summary>
        /// <param name="sysMenuTreeItems">The flat DTO sequence containing parent identifiers.</param>
        /// <returns>The root menu nodes with recursively populated children.</returns>
        public virtual IEnumerable<SysMenuTreeItemDto> GetSysMenuTreesMainRecursive(IEnumerable<SysMenuDto> sysMenuTreeItems)
        {
            List<SysMenuTreeItemDto> sysMenuTrees = new List<SysMenuTreeItemDto>();
            GetSysMenuTreesRecursive(sysMenuTreeItems, sysMenuTrees, null);
            return sysMenuTrees;
        }


        /// <summary>
        /// Recursively appends menu nodes whose parent identifier matches <paramref name="parentId"/>.
        /// </summary>
        /// <param name="sysMenuTreeItems">The complete flat menu sequence.</param>
        /// <param name="sysMenuTrees">The list receiving nodes for the current parent.</param>
        /// <param name="parentId">The parent identifier to match; <see langword="null"/> selects root menus.</param>
        private void GetSysMenuTreesRecursive(IEnumerable<SysMenuDto> sysMenuTreeItems, IList<SysMenuTreeItemDto> sysMenuTrees, string parentId)
        {
            foreach (var item in sysMenuTreeItems.Where(_ => _.ParentId == parentId && _.IsMenu))
            {
                SysMenuTreeItemDto sysMenu = new SysMenuTreeItemDto() { MenuName = item.MenuName, Id = item.Id, IsMenu = item.IsMenu, Icon = item.Icon, Url = item.Url, Children = new List<SysMenuTreeItemDto>() };
                sysMenuTrees.Add(sysMenu);
                GetSysMenuTreesRecursive(sysMenuTreeItems, sysMenu.Children, item.Id);
            }
        }

        /// <summary>
        /// Gets the hierarchical menu tree from the distributed cache, initializing the menu
        /// caches first when the tree entry is absent.
        /// </summary>
        /// <returns>A task whose result contains the cached menu tree.</returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetMenuTreeDtoByCacheAsync()
        {
            IEnumerable<SysMenuTreeItemDto> sysMenuTreeItems = null;//
            string jsonStr = await _distributedCache.GetStringAsync(MenuTreeCacheKey);
            if (string.IsNullOrEmpty(jsonStr))
            {
                await GetMenuDtoByCacheAsync(true);
            }
            jsonStr = await _distributedCache.GetStringAsync(MenuTreeCacheKey);
            sysMenuTreeItems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SysMenuTreeItemDto>>(jsonStr);
            return sysMenuTreeItems;
        }

        /// <summary>
        /// Initializes the built-in system menus, roles, permissions, and administrator-role
        /// association using the synchronous compatibility wrapper.
        /// </summary>
        /// <param name="requestTenantInfo">The tenant context used to prefix and scope initialized identifiers.</param>
        /// <param name="adminUserInfoId">The administrator account identifier to associate with the built-in role.</param>
        /// <exception cref="Exception">Propagates initialization failures after the asynchronous operation has been synchronously waited.</exception>
        [Obsolete($"Use {nameof(InitAsync)} so initialization failures can be observed.")]
        public void Init(RequestTenantInfo requestTenantInfo, int adminUserInfoId)
        {
            InitAsync(requestTenantInfo, adminUserInfoId).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes the built-in system menus, roles, permissions, and administrator-role
        /// association for the supplied tenant.
        /// </summary>
        /// <param name="requestTenantInfo">The tenant context used to prefix and scope initialized identifiers.</param>
        /// <param name="adminUserInfoId">The administrator account identifier to associate with the built-in role.</param>
        /// <returns>A task that completes after all initialization entities have been persisted.</returns>
        /// <exception cref="Exception">Thrown when any initialization persistence operation fails.</exception>
        public async Task InitAsync(RequestTenantInfo requestTenantInfo, int adminUserInfoId)
        {
            this.SetTenantInfoForAllServices(requestTenantInfo);

            string tenantId = null;
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                requestTenantInfo ??= _serviceProvider.GetRequiredService<RequestTenantInfo>();
                tenantId = $"{requestTenantInfo.Id}-";
            }

            IEnumerable<SysMenu> sysMenus = new List<SysMenu>()
            {
                new SysMenu(){ Id = tenantId+"1", MenuName = "系统管理", Url = null, Icon = "fa fa-cog", Visible = true, IsLocked = true, Sort = 300, MenuType = MenuType.菜单},

#region Legacy section
                new SysMenu(){ Id = tenantId+"1.1", MenuName = "管理员管理", Url = "/Admin/AdminUserInfo/Index", Icon = "fa fa-user-secret", Visible = true, IsLocked = true, Sort = 300, ParentId = tenantId+"1", MenuType = MenuType.菜单},

                new SysMenu() { Id =tenantId+"1.1.1", MenuName = "新增", Visible = true,ResourceCode = "admin-add", ParentId =tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.2", MenuName = "查询", Visible = true,ResourceCode = "admin-search", ParentId =tenantId+ "1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.3", MenuName = "分配角色", Visible = true,ResourceCode = "admin-grant", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.4", MenuName = "编辑", Visible = true,ResourceCode = "admin-edit", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.5", MenuName = "删除", Visible = true,ResourceCode = "admin-delete", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.6", MenuName = "查看", Visible = true,ResourceCode = "admin-detail", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
#endregion

#region rolemanagement
                new SysMenu(){ Id = tenantId+"1.2", MenuName = "角色管理", Url = "/Admin/Role/Index", Icon = "fa fa-user", Visible = true, IsLocked = true, Sort = 275, ParentId =tenantId+ "1", MenuType = MenuType.菜单},

                new SysMenu() { Id =tenantId+"1.2.1", MenuName = "新增", Visible = true, ResourceCode = "role-add", ParentId = tenantId+"1.2", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.2.2", MenuName = "查询", Visible = true,ResourceCode = "role-search", ParentId = tenantId+"1.2", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.2.3", MenuName = "授权", Visible = true,ResourceCode = "role-grant", ParentId = tenantId+"1.2", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.2.4", MenuName = "删除", Visible = true,ResourceCode = "role-delete", ParentId = tenantId+"1.2", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.2.5", MenuName = "编辑", Visible = true,ResourceCode = "role-edit", ParentId =tenantId+ "1.2", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.2.6", MenuName = "查看", Visible = true,ResourceCode = "role-detail", ParentId =tenantId+ "1.2", MenuType = MenuType.按钮 },
#endregion

                new SysMenu(){ Id = tenantId+"1.3", MenuName = "菜单管理", Url = "/Admin/Menu/Index", Icon = "fa fa-bars", Visible = true, IsLocked = true, Sort = 250, ParentId =tenantId+ "1", MenuType = MenuType.菜单},

                new SysMenu(){ Id = tenantId+"1.4", MenuName = "系统信息", Url = "/Admin/SystemConfig/Index", Icon = "fa fa-flag", Visible = true, IsLocked = true, Sort = 225, ParentId =tenantId+ "1", MenuType = MenuType.菜单},
                new SysMenu(){ Id = tenantId+"1.5", MenuName = "多租户信息", Url = "/Admin/TenantInfo/Index", Icon = "fa fa-group", Visible = true, IsLocked = true, Sort = 210, ParentId = tenantId+"1", MenuType = MenuType.菜单},

                new SysMenu(){ Id =tenantId+"2", MenuName = "扩展模块", Url = null, Icon = "fa fa-cog", Visible = true, IsLocked = true, Sort = 200, MenuType = MenuType.菜单},
                new SysMenu(){ Id =tenantId+"2.1", MenuName = "模块管理", Url = "/Admin/XncfModule/Index", Icon = "fa fa-user-secret", Visible = true, IsLocked = true, Sort = 175, ParentId =tenantId+ "2", MenuType = MenuType.菜单},
            };

            IEnumerable<SysRole> sysRoles = new List<SysRole>()
            {
                new SysRole() { Id = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, RoleName = "超级管理员", Enabled = true }
            };


            IEnumerable<SysRolePermission> sysPermissions = new List<SysRolePermission>()
            {
                new SysRolePermission() { PermissionId = tenantId+"1", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.1", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.1.1", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.1.2", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.1.3", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.1.4", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.1.5", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true } ,
                new SysRolePermission() { PermissionId = tenantId+"1.1.6", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true } ,
                new SysRolePermission() { PermissionId = tenantId+"1.2", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.2.1", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.2.2", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.2.3", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.2.4", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.2.5", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.2.6", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.3", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.4", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"1.5", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"2", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true },
                new SysRolePermission() { PermissionId = tenantId+"2.1", ResourceCode = string.Empty, RoleId = tenantId+"1", RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, IsMenu = true }
            };

            IEnumerable<SysRoleAdminUserInfo> sysRoleAdminUserInfos = new List<SysRoleAdminUserInfo>()
            {
                new SysRoleAdminUserInfo() { RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, RoleId =tenantId+ "1", AccountId = adminUserInfoId }
            };

            try
            {
                //Console.WriteLine("sysRoleService DbConte HashCode: " + sysRoleService.BaseData.BaseDB.BaseDataContext.GetHashCode());
                //Console.WriteLine("sysRolePermissionService DbConte HashCode: " + sysRolePermissionService.BaseDB.BaseDataContext.GetHashCode());
                //Console.WriteLine("sysRoleAdminUserInfoService DbConte HashCode: " + sysRoleAdminUserInfoService.BaseData.BaseDB.BaseDataContext.GetHashCode());
                //Console.WriteLine("sysMenus DbConte HashCode: " + this.BaseData.BaseDB.BaseDataContext.GetHashCode());


                await base.SaveObjectListAsync(sysMenus)
                    .ContinueWith(async t =>
                {
                    await sysRoleService.SaveObjectListAsync(sysRoles);
                    await sysRolePermissionService.SaveObjectListAsync(sysPermissions);
                    await sysRoleAdminUserInfoService.SaveObjectListAsync(sysRoleAdminUserInfos);
                });



                //_senparcEntities.Set<SysRole>().AddRange(sysRoles);
                //_senparcEntities.Set<SysMenu>().AddRange(sysMenus);
                //_senparcEntities.Set<SysRolePermission>().AddRange(sysPermissions);
                //_senparcEntities.Set<SysRoleAdminUserInfo>().AddRange(sysRoleAdminUserInfos);
                //_senparcEntities.SaveChanges();
            }
            catch (Exception ex)
            {

                throw new Exception("初始化数据失败，原因:" + ex);
            }
        }

        /// <summary>
        /// Loads the complete menu list directly from the database and maps it to menu DTOs.
        /// Unlike <see cref="GetMenuDtoByCacheAsync(bool)"/>, this method does not read or update
        /// the distributed menu caches.
        /// </summary>
        /// <returns>A task whose result contains DTOs mapped from all persisted menus.</returns>
        public virtual async Task<IEnumerable<SysMenuDto>> GetMenuDtoByDbAsync()
        {
            List<SysMenuDto> selectListItems = null;
            IConfigurationProvider configurationProvider = _serviceProvider.GetService<IMapper>().ConfigurationProvider;

            var sysMenuList = await this.GetFullListAsync(z => true);

            selectListItems = this.Mapping<SysMenuDto>(sysMenuList);  //await _serviceProvider.GetService<SenparcEntitiesBase>().Set<SysMenu>().OrderByDescending(_ => _.AddTime).ProjectTo<SysMenuDto>(configurationProvider).ToListAsync();

            //List<SysMenu> sysMenus = (await GetFullListAsync(_ => _.Visible).ConfigureAwait(false)).OrderByDescending(z => z.Sort).ToList();
            //List<SysButton> sysButtons = (await _sysButtonService.GetFullListAsync(_ => true).ConfigureAwait(false)).OrderBy(z => z.Id).ToList();
            //selectListItems = Mapper.Map<List<SysMenuDto>>(sysMenus);
            //List<SysMenuDto> buttons = _sysButtonService.Mapper.Map<List<SysMenuDto>>(sysButtons);
            //selectListItems.AddRange(buttons);
            return selectListItems;
        }
    }
}
