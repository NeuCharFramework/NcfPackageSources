/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Index.cshtml.cs
    File description: Related implementation.


    Created by: Senparc - 20241028

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain;
using AdminMenuLocalizationService = Senparc.Areas.Admin.Domain.Services.AdminMenuLocalizationService;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages
{
    [Ncf.AreaBase.Admin.Filters.IgnoreAuth]
    public class IndexModel(
        IServiceProvider serviceProvider,
        XncfModuleServiceExtension xncfModuleServiceEx,
        IAdminWorkContextProvider adminWorkContextProvider,
        AdminMenuLocalizationService menuLocalization,
        IStringLocalizer<AdminResource> localizer)
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;
        private readonly AdminMenuLocalizationService _menuLocalization = menuLocalization;
        private readonly IStringLocalizer<AdminResource> _localizer = localizer;

        //Legacy comment translated to English.
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx = xncfModuleServiceEx;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int CurrentUserId { get; set; }

        public IActionResult OnGet()
        {
            CurrentUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            return null;
            //return RedirectToPage("/Home/Index");
        }

        /// <summary>
        /// getstatus
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetXncfStatAsync()
        {
            //Legacy comment translated to English.
            var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
            //Legacy comment translated to English.
            var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);
            //Legacy comment translated to English.
            var newVersions = updateXncfRegisters.Select(z => _xncfModuleServiceEx.GetVersionDisplayName(installedXncfModules, z));
            //Legacy comment translated to English.
            var newXncfCount = newVersions.Count(z => !z.Contains("->"));
            //Legacy comment translated to English.
            var updateVersionXncfCount = newVersions.Count() - newXncfCount;
            //Legacy comment translated to English.
            var xncfRegisterManager = new XncfRegisterManager(_serviceProvider);
            var missingXncfCount = installedXncfModules.Count(z => !XncfRegisterManager.RegisterList.Exists(r => r.Uid == z.Uid));

            var data = new
            {
                success = true,
                data = new
                {
                    installedXncfCount = installedXncfModules.Count,
                    updateVersionXncfCount,
                    newXncfCount,
                    missingXncfCount
                }
            };
            return new JsonResult(data);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetXncfOpeningAsync()
        {
            //Legacy comment translated to English.
            var installedXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, 
                z => z.State == Ncf.Core.Enums.XncfModules_State.开放, 
                z => z.Id, 
                Ncf.Core.Enums.OrderingType.Descending);

            //Legacy comment translated to English.
            var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);

            var xncfModuleDtos = installedXncfModules.Select(z =>
            {
                var data = _xncfModuleServiceEx.Mapper.Map<XncfModuleDisplayDto>(z);

                //Legacy comment translated to English.
                IXncfRegister xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == data.Uid);
                if (xncfRegister != null)
                {
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    data.ApplyLocalizedMetadata(xncfRegister.MenuName, xncfRegister.Description);
                }
                // if (xncfRegister == null)
                // {
                //Legacy comment translated to English.
                // }
                data.Menus = (xncfRegister as Ncf.Core.Areas.IAreaRegister)?.AreaPageMenuItems ?? new List<Ncf.Core.Areas.AreaPageMenuItem>();

                //Legacy comment translated to English.
                if (xncfRegister != null)
                {
                    var registerType = xncfRegister.GetType();
                    var functionsByModule = Senparc.Ncf.XncfBase.Register.FunctionRenderCollection?.TryGetValue(registerType, out var funcs) == true ? funcs : null;
                    if (functionsByModule != null && functionsByModule.Count > 0)
                    {
                        data.Functions = functionsByModule.Values.Select(f => new
                        {
                            f.FunctionRenderAttribute.Name,
                            f.FunctionRenderAttribute.Description
                        }).ToList();
                    }
                    else
                    {
                        data.Functions = new List<object>();
                    }
                }

                //Legacy comment translated to English.
                var register = updateXncfRegisters.FirstOrDefault(r => r.Uid == z.Uid);
                if (register != null)
                {
                    var versionDisplay = _xncfModuleServiceEx.GetVersionDisplayName(installedXncfModules, register);
                    if (versionDisplay.Contains("->"))
                    {
                        data.HasNewVersion = true;
                        data.NewVersion = versionDisplay.Split("->")[1].Trim();
                    }
                }

                return data;
            });

            return new JsonResult(new
            {
                success = true,
                data = xncfModuleDtos
            });
        }

        #region Legacy section


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="sysPermissionService"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMenuTreeAsync([FromServices] SysRolePermissionService sysPermissionService)
        {
            IEnumerable<SysMenuDto> sysMenus = (await sysPermissionService.GetCurrentUserMenuDtoAsync())
                .Select(_menuLocalization.ResolveInPlace)
                .ToList();
            var results = await GetSysMenuTreesMainRecursiveAsync(sysMenus);
            return Ok(results);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="sysPermissionService"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMenuResourceAsync([FromServices] SysRolePermissionService sysPermissionService)
        {
            IEnumerable<SysMenuDto> sysMenus = (await sysPermissionService.GetCurrentUserMenuDtoAsync())
                .Select(_menuLocalization.ResolveInPlace)
                .ToList();
            var results = await GetSysMenuTreesMainRecursiveAsync(sysMenus);
            var resourceCodes = await sysPermissionService.GetCurrentUserResourcesDtoAsync();
            return Ok(new { menuList = results, resourceCodes });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="sysMenuTreeItems"></param>
        /// <returns></returns>
        private async Task<IEnumerable<SysMenuTreeItemDto>> GetSysMenuTreesMainRecursiveAsync(IEnumerable<SysMenuDto> sysMenuTreeItems)
        {
            bool hideModuleManager = FullSystemConfig.HideModuleManager == true;//Legacy comment translated to English.
            List<SysMenuTreeItemDto> sysMenuTrees = new List<SysMenuTreeItemDto>();
            List<SysMenuDto> dest = new List<SysMenuDto>();
            int index = 60000;

            XncfRegisterManager xncfRegisterManager = new XncfRegisterManager(_serviceProvider);

            //Legacy comment translated to English.
            foreach (var item in sysMenuTreeItems)
            {
                //Legacy comment translated to English.
                IXncfRegister xncfRegister = XncfRegisterList
                    .FirstOrDefault(z => !string.IsNullOrEmpty(item.Url) &&
                                         item.Url.Contains($"uid={z.Uid}", StringComparison.OrdinalIgnoreCase)); //Legacy comment translated to English.

                var isStoredXncf = !string.IsNullOrEmpty(item.Url) &&
                                    item.Url.Contains("uid=", StringComparison.OrdinalIgnoreCase);//Legacy comment translated to English.
                var xncfMissing = isStoredXncf && xncfRegister == null;//Legacy comment translated to English.

                if (xncfRegister != null &&
                    xncfRegister is Senparc.Ncf.Core.Areas.IAreaRegister xncfAreapage &&
                    xncfAreapage.AreaPageMenuItems.Count() > 0)
                {
                    if (hideModuleManager)
                    {
                        item.ParentId = null;
                        item.Id = (index++).ToString();
                    }
                    else
                    {
                        dest.Add(new SysMenuDto()
                        {
                            MenuName = _localizer["Xncf.SettingsAndExecute"].Value,
                            Url = item.Url,
                            Id = (index++).ToString(),
                            ParentId = item.Id,
                            Icon = "fa fa-play"
                        });
                    }


                    dest.AddRange(xncfAreapage.AreaPageMenuItems.Select(_ => new SysMenuDto()
                    {
                        MenuName = _.Name,
                        Url = _.Url,
                        Icon = _.Icon,
                        Id = string.IsNullOrEmpty(_.Id) ? (index++).ToString() : _.Id,
                        ParentId = string.IsNullOrEmpty(_.ParentId) ? item.Id : _.ParentId
                    }));
                    item.Url = string.Empty;
                }
                else if (!string.IsNullOrEmpty(item.Url) && item.Url.Contains("uid=") && hideModuleManager)//Legacy comment translated to English.
                {
                    item.ParentId = null;
                    item.Id = (index++).ToString();
                }

                if (hideModuleManager && item.Id == "4")
                {
                    continue;//Legacy comment translated to English.
                }

                //Legacy comment translated to English.
                if (isStoredXncf)
                {
                    if (xncfMissing)
                    {
                        //Legacy comment translated to English.
                        item.MenuName = $"!! {item.MenuName} !!";
                    }
                    else
                    {
                        var CheckXncfAvailable = await xncfRegisterManager.CheckXncfAvailable(xncfRegister);
                        if (!CheckXncfAvailable)
                        {
                            if (hideModuleManager)
                            {
                                //Legacy comment translated to English.
                                continue;
                            }
                            else
                            {
                                //Legacy comment translated to English.
                                item.MenuName = $"~~ {item.MenuName} ~~";
                            }
                        }
                    }
                }

                dest.Add(item);
            }

            var systemManagementMenu = dest.FirstOrDefault(z =>
                z.ParentId == null &&
                (string.Equals(z.MenuName, "系统管理", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(z.MenuName, "System Management", StringComparison.OrdinalIgnoreCase)))
                ?? dest.FirstOrDefault(z =>
                    z.ParentId == null &&
                    dest.Any(child => child.ParentId == z.Id && string.Equals(child.MenuName, "管理员管理", StringComparison.OrdinalIgnoreCase)));

            var adminChatParentId = systemManagementMenu?.Id;
            var hasAdminChatMenu = dest.Any(z => string.Equals(z.Url, "/Admin/AdminChat/Chat", StringComparison.OrdinalIgnoreCase));

            if (!hasAdminChatMenu)
            {
                dest.Insert(0,new SysMenuDto()
                {
                    MenuName = "AI 智能助手",
                    Url = "/Admin/AdminChat/Chat",
                    Icon = "fa fa-comments-o",
                    Id = (index++).ToString(),
                    ParentId = adminChatParentId
                });
            }

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var pivotMenu = dest.FirstOrDefault(z =>
                string.Equals(z.MenuName, "NeuCharPivot", StringComparison.OrdinalIgnoreCase));
            if (pivotMenu == null)
            {
                pivotMenu = new SysMenuDto
                {
                    MenuName = "NeuCharPivot",
                    Url = string.Empty,
                    Icon = "fa fa-th-large",
                    Id = (index++).ToString(),
                    ParentId = adminChatParentId
                };
                var adminChatIndex = dest.FindIndex(z =>
                    string.Equals(z.Url, "/Admin/AdminChat/Chat", StringComparison.OrdinalIgnoreCase));
                dest.Insert(adminChatIndex >= 0 ? adminChatIndex + 1 : 0, pivotMenu);
            }

            if (!dest.Any(z => string.Equals(z.Url, "/Admin/NeuCharPivot/Aggregate", StringComparison.OrdinalIgnoreCase)))
            {
                dest.Add(new SysMenuDto
                {
                    MenuName = "聚合",
                    Url = "/Admin/NeuCharPivot/Aggregate",
                    Icon = "fa fa-cubes",
                    Id = (index++).ToString(),
                    ParentId = pivotMenu.Id
                });
            }

            if (!dest.Any(z => string.Equals(z.Url, "/Admin/NeuCharPivot/Board", StringComparison.OrdinalIgnoreCase)))
            {
                dest.Add(new SysMenuDto
                {
                    MenuName = "面板",
                    Url = "/Admin/NeuCharPivot/Board",
                    Icon = "fa fa-th-large",
                    Id = (index++).ToString(),
                    ParentId = pivotMenu.Id
                });
            }
            if (!dest.Any(z => string.Equals(z.Url, "/Admin/NeuCharPivot/Access", StringComparison.OrdinalIgnoreCase)))
            {
                dest.Add(new SysMenuDto
                {
                    MenuName = "访问控制",
                    Url = "/Admin/NeuCharPivot/Access",
                    Icon = "fa fa-user-shield",
                    Id = (index++).ToString(),
                    ParentId = pivotMenu.Id
                });
            }
            GetSysMenuTreesRecursive(dest, sysMenuTrees, null);
            return sysMenuTrees;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="sysMenuTreeItems"></param>
        /// <param name="sysMenuTrees"></param>
        /// <param name="parent"></param>
        private void GetSysMenuTreesRecursive(
            IEnumerable<SysMenuDto> sysMenuTreeItems,
            IList<SysMenuTreeItemDto> sysMenuTrees,
            SysMenuDto parent,
            string moduleName = null,
            string moduleUid = null)
        {
            IEnumerable<SysMenuDto> list = sysMenuTreeItems.Where(_ => _.ParentId == parent?.Id);
            foreach (var item in list)
            {
                var currentModuleName = moduleName;
                var currentModuleUid = moduleUid;
                var itemUid = GetUidFromUrl(item.Url);
                if (IsXncfModuleStartUrl(item.Url)
                    && !string.IsNullOrWhiteSpace(itemUid)
                    && (string.IsNullOrWhiteSpace(currentModuleUid)
                        || !string.Equals(currentModuleUid, itemUid, StringComparison.OrdinalIgnoreCase)))
                {
                    currentModuleName = item.MenuName;
                    currentModuleUid = itemUid;
                }

                SysMenuTreeItemDto sysMenu = new SysMenuTreeItemDto()
                {
                    MenuName = item.MenuName,
                    Id = item.Id,
                    Icon = item.Icon,
                    Url = item.Url,
                    ModuleName = currentModuleName,
                    ModuleUid = currentModuleUid,
                    Children = new List<SysMenuTreeItemDto>()
                };
                sysMenuTrees.Add(sysMenu);
                GetSysMenuTreesRecursive(sysMenuTreeItems, sysMenu.Children, item, currentModuleName, currentModuleUid);
            }
        }

        private static bool IsXncfModuleStartUrl(string url)
        {
            return !string.IsNullOrWhiteSpace(url)
                   && url.Contains("/Admin/XncfModule/Start", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetUidFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            var queryIndex = url.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex < 0 || queryIndex == url.Length - 1)
            {
                return string.Empty;
            }

            foreach (var pair in url[(queryIndex + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = pair.IndexOf('=');
                var key = separatorIndex < 0 ? pair : pair[..separatorIndex];
                if (!string.Equals(key, "uid", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = separatorIndex < 0 ? string.Empty : pair[(separatorIndex + 1)..];
                return WebUtility.UrlDecode(value) ?? string.Empty;
            }

            return string.Empty;
        }

        #endregion


    }
}
