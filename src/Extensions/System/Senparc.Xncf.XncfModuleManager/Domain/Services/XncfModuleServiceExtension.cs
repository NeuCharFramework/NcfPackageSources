﻿
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.XncfModuleManager.Models;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Xncf.XncfModuleManager.Domain.Services
{
    /// <summary>
    /// TODO：此接口需要开放到 OHS，对外提供服务
    /// </summary>
    public class XncfModuleServiceExtension : XncfModuleService
    {
        private readonly Lazy<SysMenuService> _sysMenuService;
        private readonly Lazy<SysRoleService> _sysRoleService;

        /// <summary>
        /// 获取模块起始触发 URL
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        private string GetStartUrl(IXncfRegister register)
        {
            return $"/Admin/XncfModule/Start/?uid={register.Uid}";
        }

        public XncfModuleServiceExtension(IRepositoryBase<XncfModule> repo, Lazy<SysMenuService> sysMenuService, Lazy<SysRoleService> sysRoleService, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _sysMenuService = sysMenuService;
            this._sysRoleService = sysRoleService;
        }

        /// <summary>
        /// 安装模块
        /// </summary>
        /// <param name="uid">模块 Uid</param>
        /// <param name="addMenu">是否在安装完成后添加到管理员菜单</param>
        /// <returns></returns>
        //public async Task<Tuple<PagedList<XncfModule>, string, InstallOrUpdate?>> InstallModuleAsync(string uid)
        public async Task<(PagedList<XncfModule> XncfModuleList, string scanAndInstallResult, InstallOrUpdate? InstallOrUpdate)> InstallModuleAsync(string uid, bool addMenu = true)
        {
            if (uid.IsNullOrEmpty())
            {
                throw new Exception("模块不存在！");
            }

            var xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == uid);
            if (xncfRegister == null)
            {
                throw new Exception("模块不存在！");
            }

            var xncfModule = await base.GetObjectAsync(z => z.Uid == xncfRegister.Uid && z.Version == xncfRegister.Version).ConfigureAwait(false);
            if (xncfModule != null)
            {
                throw new Exception("相同版本模块已安装，无需重复安装！");
            }

            /* 目前禁用自动升级，因此不从数据库访问所有记录 */

            PagedList<XncfModule> xncfModules = await base.GetObjectListAsync(1, 999, z => true, z => z.AddTime, Ncf.Core.Enums.OrderingType.Descending).ConfigureAwait(false);

            var xncfModuleDtos = xncfModules.Select(z => base.Mapper.Map<CreateOrUpdate_XncfModuleDto>(z)).ToList();

            //var se = _serviceProvider.GetService(typeof(BasePoolEntities_SqlServer));

            //进行模块扫描
            InstallOrUpdate? installOrUpdateValue = null;
            var result = await Senparc.Ncf.XncfBase.Register.ScanAndInstall(xncfModuleDtos, _serviceProvider, async (register, installOrUpdate) =>
            {
                installOrUpdateValue = installOrUpdate;
                if (addMenu)
                {
                    await InstallMenuAsync(register, installOrUpdate);
                }

            }, uid).ConfigureAwait(false);

            //记录日志
            var installOrUpdateMsg = installOrUpdateValue.HasValue
                                        ? (installOrUpdateValue.Value == InstallOrUpdate.Install ? "安装" : "更新")
                                        : "失败";
            SenparcTrace.SendCustomLog($"安装或更新模块（{installOrUpdateMsg}）", result.ToString());
            return (xncfModules, result, installOrUpdateValue);
        }

        /// <summary>
        /// 安装模块之后，安装菜单
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        public async Task InstallMenuAsync(IXncfRegister register, InstallOrUpdate installOrUpdate)
        {
            var topMenu = await _sysMenuService.Value.GetObjectAsync(z => z.MenuName == "扩展模块").ConfigureAwait(false);
            var startUrl = GetStartUrl(register);
            var currentMenu = await _sysMenuService.Value.GetObjectAsync(z => z.ParentId == topMenu.Id && z.Url == startUrl).ConfigureAwait(false);

            SysMenuDto menuDto;

            if (installOrUpdate == InstallOrUpdate.Update && currentMenu != null)
            {
                //更新菜单
                menuDto = _sysMenuService.Value.Mapper.Map<SysMenuDto>(currentMenu);
                menuDto.MenuName = register.MenuName;//更新菜单名称
                menuDto.Icon = register.Icon;//更新菜单图标
            }
            else
            {
                //新建菜单
                var icon = register.Icon.IsNullOrEmpty() ? "fa fa-bars" : register.Icon;
                var order = 20;
                switch (register.Uid)
                {
                    case SiteConfig.SYSTEM_XNCF_MODULE_SERVICE_MANAGER_UID:
                        order = 160;
                        break;
                    case SiteConfig.SYSTEM_XNCF_MODULE_AREAS_ADMIN_UID:
                        order = 150;
                        break;
                    default:
                        break;
                }
                menuDto = new SysMenuDto(true, null, register.MenuName, topMenu.Id, startUrl, icon, order, true, null);
            }

            var sysMemu = await _sysMenuService.Value.CreateOrUpdateAsync(menuDto).ConfigureAwait(false);

            if (installOrUpdate == InstallOrUpdate.Install)
            {
                //更新菜单信息

                //获取管理员组的 RoleId
                var administratorRoleCode = Senparc.Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE;
                var sysRole = await _sysRoleService.Value.GetObjectAsync(z => z.RoleCode == administratorRoleCode);

                SysPermissionDto sysPermissionDto = new SysPermissionDto()
                {
                    IsMenu = true,
                    ResourceCode = sysMemu.ResourceCode,
                    //RoleId = "1-1",
                    RoleId = sysRole?.Id ?? "1",
                    RoleCode = sysRole?.RoleCode ?? administratorRoleCode,
                    PermissionId = sysMemu.Id
                };
                //SenparcEntities db = _serviceProvider.GetService<SenparcEntities>();

                //XncfModuleManagerSenparcEntities db = _serviceProvider.GetService<XncfModuleManagerSenparcEntities>();
                BasePoolEntities db = _serviceProvider.GetService<BasePoolEntities>();

                db.Set<SysRolePermission>().Add(new SysRolePermission(sysPermissionDto));
                await db.SaveChangesAsync();
                var updateMenuDto = new UpdateMenuId_XncfModuleDto(register.Uid, sysMemu.Id);
                await base.UpdateMenuIdAsync(updateMenuDto).ConfigureAwait(false);
            }

        }


        /// <summary>
        /// 获取显示的版本号信息（如果有新版本，则显示格式：[旧版本号] -> [新版本号]
        /// </summary>
        /// <param name="xncfModules"></param>
        /// <param name="xncfRegister"></param>
        /// <returns></returns>
        public string GetVersionDisplayName(List<XncfModule> xncfModules, IXncfRegister xncfRegister)
        {
            var installedXncf = xncfModules.FirstOrDefault(z => z.Uid == xncfRegister.Uid);
            if (installedXncf == null || installedXncf.Version == xncfRegister.Version)
            {
                return xncfRegister.Version;
            }
            return $"{installedXncf.Version} -> {xncfRegister.Version}";
        }

        /// <summary>
        /// 获取未安装的 Xncf（或版本不同）
        /// </summary>
        /// <returns></returns>
        public List<IXncfRegister> GetUnInstallXncfModule(List<XncfModule> xncfModules)
        {
            var newXncfRegisters = XncfRegisterManager.RegisterList.Where(z => !z.IgnoreInstall && !xncfModules.Exists(m => m.Uid == z.Uid && m.Version == z.Version)).ToList() ?? new List<IXncfRegister>();
            return newXncfRegisters;
        }

        /// <summary>
        /// 获取未安装的 Xncf
        /// </summary>
        /// <returns></returns>
        public List<IXncfRegister> GetOnlyUnInstallXncfModule(List<XncfModule> xncfModules)
        {
            var newXncfRegisters = XncfRegisterManager.RegisterList.Where(z => !z.IgnoreInstall && !xncfModules.Exists(m => m.Uid == z.Uid)).ToList() ?? new List<IXncfRegister>();
            return newXncfRegisters;
        }

        /// <summary>
        /// 获取版本不同的 Xncf
        /// </summary>
        /// <returns></returns>
        public List<IXncfRegister> GetUpdatedInstallXncfModule(List<XncfModule> xncfModules)
        {

            var newXncfRegisters = XncfRegisterManager.RegisterList.Where(z => !z.IgnoreInstall && xncfModules.Exists(m => m.Uid == z.Uid && m.Version != z.Version)).ToList() ?? new List<IXncfRegister>();
            return newXncfRegisters;
        }
    }
}
