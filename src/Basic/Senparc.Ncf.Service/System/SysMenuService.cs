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
    /// 
    /// </summary>
    public class SysMenuService : ClientServiceBase<SysMenu>
    {
        private readonly SysButtonService _sysButtonService;
        private readonly IDistributedCache _distributedCache;
        private readonly SysRoleService sysRoleService;
        private readonly SysMenuService sysMenuService;
        private readonly ServiceBase<SysRolePermission> sysRolePermissionService;
        private readonly SysRoleAdminUserInfoService sysRoleAdminUserInfoService;
        public const string MenuCacheKey = "AllMenus";
        public const string MenuTreeCacheKey = "AllMenusTree";

        //private readonly SenparcEntitiesBase _senparcEntities;

        public SysMenuService(ClientRepositoryBase<SysMenu> repo, IServiceProvider serviceProvider,
            SysRoleService sysRoleService, SysMenuService sysMenuService, ServiceBase<SysRolePermission> sysRolePermissionService,
            SysRoleAdminUserInfoService sysRoleAdminUserInfoService
            ) : base(repo, serviceProvider)
        {
            _sysButtonService = _serviceProvider.GetService<SysButtonService>();
            _distributedCache = _serviceProvider.GetService<IDistributedCache>();
            this.sysRoleService = sysRoleService;
            this.sysMenuService = sysMenuService;
            this.sysRolePermissionService = sysRolePermissionService;
            this.sysRoleAdminUserInfoService = sysRoleAdminUserInfoService;
            //_senparcEntities = _serviceProvider.GetService<SenparcEntitiesBase>();
        }

        /// <summary>
        /// TODO...重建菜单角色缓存
        /// </summary>
        /// <param name="sysMenuDto"></param>
        /// <returns></returns>
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
                    return menu;//TODO：需要给出提示
                }

                isRepeat = await sysMenuService.GetObjectAsync(_ => _.ResourceCode == sysMenuDto.ResourceCode && _.Id != sysMenuDto.Id) != null;

                //isRepeat = await _serviceProvider.GetService<SenparcEntitiesBase>().Set<SysMenu>().AnyAsync(_ => _.ResourceCode == sysMenuDto.ResourceCode && _.Id != sysMenuDto.Id);

                menu.Update(sysMenuDto);
            }
            else
            {
                menu = new SysMenu(sysMenuDto);

                isRepeat = await sysMenuService.GetObjectAsync(_ => _.ResourceCode == sysMenuDto.ResourceCode) != null;

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
        /// TODO...重建菜单角色缓存
        /// </summary>
        /// <param name="sysMenuDto"></param>
        /// <param name="buttons"></param>
        /// <returns></returns>
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
        /// 获取缓存中的数据
        /// </summary>
        /// <param name="isRefresh"></param>
        /// <returns></returns>
        public virtual async Task RemoveMenuAsync()
        {
            await _distributedCache.RemoveAsync(MenuCacheKey);
        }

        /// <summary>
        /// 获取缓存中的数据 TODO...
        /// </summary>
        /// <param name="isRefresh"></param>
        /// <returns></returns>
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

        public virtual IEnumerable<SysMenuTreeItemDto> GetSysMenuTreesMainRecursive(IEnumerable<SysMenuDto> sysMenuTreeItems)
        {
            List<SysMenuTreeItemDto> sysMenuTrees = new List<SysMenuTreeItemDto>();
            GetSysMenuTreesRecursive(sysMenuTreeItems, sysMenuTrees, null);
            return sysMenuTrees;
        }


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
        /// 获取缓存中的数据
        /// </summary>
        /// <returns></returns>
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
        /// 初始化菜单及其权限
        /// </summary>
        public async void Init(RequestTenantInfo requestTenantInfo)
        {
            //设置 TenantId 前缀，避免不同租户之间的 ID 冲突
            string tenantId = null;
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                requestTenantInfo ??= _serviceProvider.GetRequiredService<RequestTenantInfo>();
                tenantId = $"{requestTenantInfo.Id}-";
            }

            IEnumerable<SysMenu> sysMenus = new List<SysMenu>()
            {
                new SysMenu(){ Id = tenantId+"1", MenuName = "系统管理", Url = null, Icon = "fa fa-cog", Visible = true, IsLocked = true, Sort = 300, MenuType = MenuType.菜单},

#region 管理员管理
                new SysMenu(){ Id = tenantId+"1.1", MenuName = "管理员管理", Url = "/Admin/AdminUserInfo/Index", Icon = "fa fa-user-secret", Visible = true, IsLocked = true, Sort = 300, ParentId = tenantId+"1", MenuType = MenuType.菜单},

                new SysMenu() { Id =tenantId+"1.1.1", MenuName = "新增", Visible = true,ResourceCode = "admin-add", ParentId =tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.2", MenuName = "查询", Visible = true,ResourceCode = "admin-search", ParentId =tenantId+ "1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.3", MenuName = "分配角色", Visible = true,ResourceCode = "admin-grant", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.4", MenuName = "编辑", Visible = true,ResourceCode = "admin-edit", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.5", MenuName = "删除", Visible = true,ResourceCode = "admin-delete", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
                new SysMenu() { Id =tenantId+"1.1.6", MenuName = "查看", Visible = true,ResourceCode = "admin-detail", ParentId = tenantId+"1.1", MenuType = MenuType.按钮 },
#endregion

#region 角色管理
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
                new SysRoleAdminUserInfo() { RoleCode = Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, RoleId =tenantId+ "1", AccountId = 1 }
            };

            try
            {
                await sysRoleService.SaveObjectListAsync(sysRoles);
                await sysMenuService.SaveObjectListAsync(sysMenus);
                await sysRolePermissionService.SaveObjectListAsync(sysPermissions);
                await sysRoleAdminUserInfoService.SaveObjectListAsync(sysRoleAdminUserInfos);

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
        /// 获取数据库的菜单集合(线性)
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<SysMenuDto>> GetMenuDtoByDbAsync()
        {
            List<SysMenuDto> selectListItems = null;
            IConfigurationProvider configurationProvider = _serviceProvider.GetService<IMapper>().ConfigurationProvider;

            var sysMenuList = await sysMenuService.GetFullListAsync(z => true);

            selectListItems = sysMenuService.Mapping<SysMenuDto>(sysMenuList);  //await _serviceProvider.GetService<SenparcEntitiesBase>().Set<SysMenu>().OrderByDescending(_ => _.AddTime).ProjectTo<SysMenuDto>(configurationProvider).ToListAsync();

            //List<SysMenu> sysMenus = (await GetFullListAsync(_ => _.Visible).ConfigureAwait(false)).OrderByDescending(z => z.Sort).ToList();
            //List<SysButton> sysButtons = (await _sysButtonService.GetFullListAsync(_ => true).ConfigureAwait(false)).OrderBy(z => z.Id).ToList();
            //selectListItems = Mapper.Map<List<SysMenuDto>>(sysMenus);
            //List<SysMenuDto> buttons = _sysButtonService.Mapper.Map<List<SysMenuDto>>(sysButtons);
            //selectListItems.AddRange(buttons);
            return selectListItems;
        }
    }
}
