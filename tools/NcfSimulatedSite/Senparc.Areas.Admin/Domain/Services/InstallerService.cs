using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NcfSysMenuService = Senparc.Ncf.Service.SysMenuService;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class InstallerService(IServiceProvider serviceProvider, TenantInfoService tenantInfoService, XncfModuleServiceExtension xncfModuleServiceExtension)
    {
        public async Task InitSystemAsync(string systemName,int adminUserInfoId, TenantInfo tenantInfo)
        {
            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();

            Senparc.Xncf.SystemCore.Register systemCoreRegister = new Senparc.Xncf.SystemCore.Register();

            Senparc.Xncf.SystemManager.Register systemManagerRegister = new Senparc.Xncf.SystemManager.Register();

            Senparc.Xncf.SystemPermission.Register systemPermissionRegister = new Senparc.Xncf.SystemPermission.Register();

            Senparc.Xncf.XncfModuleManager.Register xncfModuleManagerRegister = new Senparc.Xncf.XncfModuleManager.Register();

            Senparc.Xncf.AreasBase.Register areasBaseRegister = new Senparc.Xncf.AreasBase.Register();

            //Senparc.Xncf.Installer.Register installerRegister = new Senparc.Xncf.Installer.Register();

            Senparc.Xncf.Menu.Register menuRegister = new Senparc.Xncf.Menu.Register();

            {
                //Start installing permission module
                //(Must be placed first, other module operations need to rely on this module)
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, tenantInfo, installNow: false, addMenu: false);

                //Start installing the menu management module
                //(Must be placed second, other module operations need to rely on this module)
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, tenantInfo, installNow: false, addMenu: false);

                try
                {
                    //Install default modules and configurations such as permissions and menus

                    NcfSysMenuService _sysMenuService = serviceProvider.GetService<NcfSysMenuService>();
                    //.Init will be executed once internally
                    _sysMenuService.SetTenantInfoForAllServices(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                    _sysMenuService.Init(tenantInfoService.GetRequestTenantInfo(tenantInfo), adminUserInfoId);
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("_sysMenuService.Init 异常", ex.Message);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }

                //Start installing the module management module
                //(Must be placed in the third position, other module operations need to rely on this module)
                await InstallAndOpenModuleAsync(xncfModuleManagerRegister, serviceProvider, tenantInfo);

                //Start installing system basic modules
                await InstallAndOpenModuleAsync(systemCoreRegister, serviceProvider, tenantInfo);

                //Start installing the system management management module
                await InstallAndOpenModuleAsync(systemManagerRegister, serviceProvider, tenantInfo);

                //Start installing the Areas module
                await InstallAndOpenModuleAsync(areasBaseRegister, serviceProvider, tenantInfo);

                ////Start installing the Installer module
                //await InstallAndOpenModuleAsync(installerRegister, serviceProvider);

                //Install tenant module
                //if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                //{
                //    await InstallAndOpenModuleAsync(tenantRegister, serviceProvider, tenantInfo);
                //}

                //Install the permission module
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, tenantInfo, true, true);

                //Install the menu module
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, tenantInfo, true, true);
            }

            //TODO: Selectively install user-defined modules
            {
                List<string> installedList = new List<string> { "00000000-0000-0000-0001-000000000001", "00000000-0000-0000-0001-000000000002", "00000000-0000-0000-0001-000000000003", "00000000-0000-0000-0001-000000000004"
                , "00000000-0000-0000-0001-000000000005", "00000000-0000-0000-0001-000000000006","00000000-0000-0001-0001-000000000001", "62FBB022-B04E-423F-82FE-926D418A0815"};
                var _xncfModuleService = xncfModuleServiceExtension;// serviceProvider.GetService<XncfModuleServiceExtension>();
                //if (needModelList != null)
                //{
                //    foreach (var needModelId in needModelList)
                //    {
                //        if (!installedList.Contains(needModelId))
                //        {
                //            //var docRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == needModel);
                //            var docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                //            if (docModule == null)
                //            {
                //                await _xncfModuleService.InstallModuleAsync(needModelId);
                //                docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                //            }
                //            //Open module
                //            if (docModule.State != Ncf.Core.Enums.XncfModules_State.open)
                //            {
                //                docModule.UpdateState(Ncf.Core.Enums.XncfModules_State.Open);
                //                await _xncfModuleService.SaveObjectAsync(docModule);
                //            }
                //        }
                //    }
                //}
            }


            {
                var _xncfModuleService = xncfModuleServiceExtension;
                _xncfModuleService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                //Start installing and enabling system modules (Admin)
                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister, serviceProvider, tenantInfo);
                //Make sure it has been assigned
                adminModule ??= await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);

                //Save (all) changes at once
                await _xncfModuleService.SaveObjectAsync(adminModule).ConfigureAwait(false);

                var _systemConfigService = serviceProvider.GetService<SystemConfigService>();
                _systemConfigService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));
                _systemConfigService.Init(systemName);//Initialize system information
            }

            {
                //Start installing user modules (if any) TODO: Selective installation

            }
        }


        /// <summary>
        ///Install and open the module
        /// </summary>
        /// <param name="register"></param>
        /// <param name="installNow">Whether to install now</param>
        /// <param name="addMenu">Whether to add a menu (must be installedNow is true to take effect)</param>
        /// <returns></returns>
        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, IServiceProvider serviceProvider, TenantInfo tenantInfo, bool installNow = true, bool addMenu = true)
        {
            try
            {
                //Start installing the module (create database related tables)
                await register.InstallOrUpdateAsync(serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);

                throw;
            }


            XncfModule xncfModule = null;

            //Install module
            if (installNow)
            {
                var _xncfModuleService = xncfModuleServiceExtension;// serviceProvider.GetService<XncfModuleServiceExtension>();

                //Set tenant information
                _xncfModuleService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                xncfModule = _xncfModuleService.GetObject(z => z.Uid == register.Uid);
                if (xncfModule == null)
                {
                    try
                    {
                        await _xncfModuleService.InstallModuleAsync(register.Uid, addMenu);
                        xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("_xncfModuleService.InstallModuleAsync 异常：", ex.Message);
                        SenparcTrace.BaseExceptionLog(ex);
                        throw;
                    }
                }

                //enable module
                //xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
                xncfModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
            }

            //if (addMenu)
            //{
            //    //TODO: Determine whether it already exists
            //    await _xncfModuleService.InstallMenuAsync(register, Ncf.Core.Enums.InstallOrUpdate.Install);
            //}

            return xncfModule;
        }
    }
}
