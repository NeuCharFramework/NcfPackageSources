/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: InstallerService.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20250503
    
    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
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
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, tenantInfo, installNow: false, addMenu: false);

                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, tenantInfo, installNow: false, addMenu: false);

                try
                {
                    //Legacy comment translated to English.

                    NcfSysMenuService _sysMenuService = serviceProvider.GetService<NcfSysMenuService>();
                    //Legacy comment translated to English.
                    _sysMenuService.SetTenantInfoForAllServices(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                    await _sysMenuService.InitAsync(tenantInfoService.GetRequestTenantInfo(tenantInfo), adminUserInfoId);
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("_sysMenuService.Init 异常", ex.Message);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }

                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(xncfModuleManagerRegister, serviceProvider, tenantInfo);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemCoreRegister, serviceProvider, tenantInfo);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemManagerRegister, serviceProvider, tenantInfo);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(areasBaseRegister, serviceProvider, tenantInfo);

                ///Legacy comment translated to English.
                //await InstallAndOpenModuleAsync(installerRegister, serviceProvider);

                //Legacy comment translated to English.
                //if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                //{
                //    await InstallAndOpenModuleAsync(tenantRegister, serviceProvider, tenantInfo);
                //}

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, tenantInfo, true, true);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, tenantInfo, true, true);
            }

            //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //            {
                //Legacy comment translated to English.
                //                await _xncfModuleService.SaveObjectAsync(docModule);
                //            }
                //        }
                //    }
                //}
            }


            {
                var _xncfModuleService = xncfModuleServiceExtension;
                _xncfModuleService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                //Legacy comment translated to English.
                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister, serviceProvider, tenantInfo);
                //Legacy comment translated to English.
                adminModule ??= await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);

                //Legacy comment translated to English.
                await _xncfModuleService.SaveObjectAsync(adminModule).ConfigureAwait(false);

                var _systemConfigService = serviceProvider.GetService<SystemConfigService>();
                _systemConfigService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));
                _systemConfigService.Init(systemName);//Legacy comment translated to English.

                var _adminAuthConfigService = serviceProvider.GetService<AdminAuthConfigService>();
                _adminAuthConfigService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));
                var authSettings = _adminAuthConfigService.GetEffectiveExpireSettings();
                if (authSettings.UsingDefault && authSettings.Source == "missing-record")
                {
                    var authInitResult = await _adminAuthConfigService.TrySaveExpireSettingsAsync(
                        AdminAuthConfig.DefaultAdminWebLoginExpireMinutes,
                        AdminAuthConfig.DefaultBackendJwtExpireMinutes);
                    if (!authInitResult.Success)
                    {
                        SenparcTrace.SendCustomLog("AdminAuthConfig 初始化提示", authInitResult.Message);
                    }
                }
            }

            {
                //Legacy comment translated to English.

            }
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="register"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, IServiceProvider serviceProvider, TenantInfo tenantInfo, bool installNow = true, bool addMenu = true)
        {
            try
            {
                //start installing the module (create related database tables)
                await register.InstallOrUpdateAsync(serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);

                throw;
            }


            XncfModule xncfModule = null;

            //Legacy comment translated to English.
            if (installNow)
            {
                var _xncfModuleService = xncfModuleServiceExtension;// serviceProvider.GetService<XncfModuleServiceExtension>();

                //Legacy comment translated to English.
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

                //Legacy comment translated to English.
                //xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
                xncfModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
            }

            //if (addMenu)
            //{
            //Legacy comment translated to English.
            //    await _xncfModuleService.InstallMenuAsync(register, Ncf.Core.Enums.InstallOrUpdate.Install);
            //}

            return xncfModule;
        }
    }
}
