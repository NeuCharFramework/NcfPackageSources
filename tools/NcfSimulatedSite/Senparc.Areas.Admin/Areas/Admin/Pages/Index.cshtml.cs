using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages
{
    [Ncf.AreaBase.Admin.Filters.IgnoreAuth]
    public class IndexModel(
        IServiceProvider serviceProvider,
        XncfModuleServiceExtension xncfModuleServiceEx,
        IAdminWorkContextProvider adminWorkContextProvider)
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

        //TODO: Obtained from other modules
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx = xncfModuleServiceEx;

        /// <summary>
        ///Current user ID (optional, used for front-end acquisition)
        /// </summary>
        public int CurrentUserId { get; set; }

        public IActionResult OnGet()
        {
            CurrentUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            return null;
            //return RedirectToPage("/Home/Index");
        }

        /// <summary>
        /// Get status
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetXncfStatAsync()
        {
            //All installed modules
            var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
            //Modules not installed or to be upgraded
            var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);
            //The version of the freight forwarding upgrade module is not installed
            var newVersions = updateXncfRegisters.Select(z => _xncfModuleServiceEx.GetVersionDisplayName(installedXncfModules, z));
            //Version number that needs to be upgraded
            var newXncfCount = newVersions.Count(z => !z.Contains("->"));
            //New, uninstalled version number
            var updateVersionXncfCount = newVersions.Count() - newXncfCount;
            //Missing modules after installation
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
        /// Get open module information
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetXncfOpeningAsync()
        {
            //All installed modules
            var installedXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, 
                z => z.State == Ncf.Core.Enums.XncfModules_State.开放, 
                z => z.Id, 
                Ncf.Core.Enums.OrderingType.Descending);

            //Get modules that are not installed or need to be upgraded
            var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);

            var xncfModuleDtos = installedXncfModules.Select(z =>
            {
                var data = _xncfModuleServiceEx.Mapper.Map<XncfModuleDisplayDto>(z);

                //Get module menu information
                IXncfRegister xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == data.Uid);
                // if (xncfRegister == null)
                // {
                //     throw new Exception($"Module is missing or not loaded ({XncfRegisterManager.RegisterList.Count})!");
                // }
                data.Menus = (xncfRegister as Ncf.Core.Areas.IAreaRegister)?.AreaPageMenuItems ?? new List<Ncf.Core.Areas.AreaPageMenuItem>();

                //Get the Functions information of the module
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

                //Find the corresponding updated version
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

        #region 菜单相关


        /// <summary>
        /// Get stateless menu information
        /// </summary>
        /// <param name="sysPermissionService"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMenuTreeAsync([FromServices] SysRolePermissionService sysPermissionService)
        {
            IEnumerable<SysMenuDto> sysMenus = await sysPermissionService.GetCurrentUserMenuDtoAsync();
            var results = await GetSysMenuTreesMainRecursiveAsync(sysMenus);
            return Ok(results);
        }

        /// <summary>
        /// Get the menu information containing the current page information
        /// </summary>
        /// <param name="sysPermissionService"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMenuResourceAsync([FromServices] SysRolePermissionService sysPermissionService)
        {
            IEnumerable<SysMenuDto> sysMenus = await sysPermissionService.GetCurrentUserMenuDtoAsync();
            var results = await GetSysMenuTreesMainRecursiveAsync(sysMenus);
            var resourceCodes = await sysPermissionService.GetCurrentUserResourcesDtoAsync();
            return Ok(new { menuList = results, resourceCodes });
        }

        /// <summary>
        /// Get the Admin background left menu structure
        /// </summary>
        /// <param name="sysMenuTreeItems"></param>
        /// <returns></returns>
        private async Task<IEnumerable<SysMenuTreeItemDto>> GetSysMenuTreesMainRecursiveAsync(IEnumerable<SysMenuDto> sysMenuTreeItems)
        {
            bool hideModuleManager = FullSystemConfig.HideModuleManager == true;//Whether it is in publishing state and some menus need to be hidden
            List<SysMenuTreeItemDto> sysMenuTrees = new List<SysMenuTreeItemDto>();
            List<SysMenuDto> dest = new List<SysMenuDto>();
            int index = 60000;

            XncfRegisterManager xncfRegisterManager = new XncfRegisterManager(_serviceProvider);

            //Traverse the menu setting items and find the menu nodes related to the XNCF module
            foreach (var item in sysMenuTreeItems)
            {
                //Locating XNCF modules
                IXncfRegister xncfRegister = XncfRegisterList
                    .FirstOrDefault(z => !string.IsNullOrEmpty(item.Url) &&
                                         item.Url.Contains($"uid={z.Uid}", StringComparison.OrdinalIgnoreCase)); //TODO: Judging Xncf conditions can be more detailed

                var isStoredXncf = !string.IsNullOrEmpty(item.Url) &&
                                    item.Url.Contains("uid=", StringComparison.OrdinalIgnoreCase);//Register as a module in the database
                var xncfMissing = isStoredXncf && xncfRegister == null;//Assembly not loaded

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
                            MenuName = "设置/执行",
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
                else if (!string.IsNullOrEmpty(item.Url) && item.Url.Contains("uid=") && hideModuleManager)//TODO: Judging Xncf conditions can be more detailed
                {
                    item.ParentId = null;
                    item.Id = (index++).ToString();
                }

                if (hideModuleManager && item.Id == "4")
                {
                    continue;//Expand the module to determine whether to hide it
                }

                //This menu is a menu for modules
                if (isStoredXncf)
                {
                    if (xncfMissing)
                    {
                        //Prompt when XNCF is unavailable (whether in management mode or not)
                        item.MenuName = $"!! {item.MenuName} !!";
                    }
                    else
                    {
                        var CheckXncfAvailable = await xncfRegisterManager.CheckXncfAvailable(xncfRegister);
                        if (!CheckXncfAvailable)
                        {
                            if (hideModuleManager)
                            {
                                //XNCF is not displayed when it is unavailable.
                                continue;
                            }
                            else
                            {
                                //When XNCF is unavailable, give a prompt
                                item.MenuName = $"~~ {item.MenuName} ~~";
                            }
                        }
                    }
                }

                dest.Add(item);
            }
            GetSysMenuTreesRecursive(dest, sysMenuTrees, null);
            return sysMenuTrees;
        }

        /// <summary>
        /// Recursively set menu node information
        /// </summary>
        /// <param name="sysMenuTreeItems"></param>
        /// <param name="sysMenuTrees"></param>
        /// <param name="parent"></param>
        private void GetSysMenuTreesRecursive(IEnumerable<SysMenuDto> sysMenuTreeItems, IList<SysMenuTreeItemDto> sysMenuTrees, SysMenuDto parent)
        {
            IEnumerable<SysMenuDto> list = sysMenuTreeItems.Where(_ => _.ParentId == parent?.Id);
            foreach (var item in list)
            {
                SysMenuTreeItemDto sysMenu = new SysMenuTreeItemDto()
                {
                    MenuName = item.MenuName,
                    Id = item.Id,
                    Icon = item.Icon,
                    Url = item.Url,
                    Children = new List<SysMenuTreeItemDto>()
                };
                sysMenuTrees.Add(sysMenu);
                GetSysMenuTreesRecursive(sysMenuTreeItems, sysMenu.Children, item);
            }
        }

        #endregion


    }
}