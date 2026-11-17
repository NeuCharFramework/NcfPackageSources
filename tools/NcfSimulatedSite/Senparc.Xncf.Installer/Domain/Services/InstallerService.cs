/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc



    File name: InstallerService.cs

    File description: InstallerService.cs related implementation





    Created by: Senparc - 20240312



    Modified by: Senparc - 20260729

    Change description: v0.4.1 Updated module behavior and related support.



    Modified by: Senparc - 20261103

    Change description: v0.5.12 Updated module behavior and related support.



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.Tenant.Domain.Services;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.Domain.Services
{
    public class InstallerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly InstallOptionsService _installOptionsService;
        private readonly XncfModuleServiceExtension _xncfModuleServiceExtension;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }
        public TenantRule TenantRule { get; set; }
        public bool MultiTenantEnable { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        private async Task InitSystemAsync(string systemName, IServiceProvider serviceProvider, List<string> needModelList,int adminUserInfoId)
        {
            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();

            Senparc.Xncf.SystemCore.Register systemCoreRegister = new Senparc.Xncf.SystemCore.Register();

            Senparc.Xncf.SystemManager.Register systemManagerRegister = new Senparc.Xncf.SystemManager.Register();

            Senparc.Xncf.SystemPermission.Register systemPermissionRegister = new Senparc.Xncf.SystemPermission.Register();

            Senparc.Xncf.XncfModuleManager.Register xncfModuleManagerRegister = new Senparc.Xncf.XncfModuleManager.Register();

            Senparc.Xncf.AreasBase.Register areasBaseRegister = new Senparc.Xncf.AreasBase.Register();

            Senparc.Xncf.Installer.Register installerRegister = new Senparc.Xncf.Installer.Register();

            Senparc.Xncf.Menu.Register menuRegister = new Senparc.Xncf.Menu.Register();

            {
                await InitDatabaseAsync(() => systemCoreRegister.InitDatabase(serviceProvider));//Legacy comment translated to English.

                //await InitDatabaseAsync(() => systemManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => systemPermissionRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => xncfModuleManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => menuRegister.InitDatabase(_serviceProvider));


                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, installNow: false, addMenu: false);

                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, installNow: false, addMenu: false);

                try
                {
                    SysMenuService _sysMenuService = serviceProvider.GetService<SysMenuService>();

                    await _sysMenuService.InitAsync(null, adminUserInfoId);
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("_sysMenuService.Init 异常", ex.Message);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }

                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(xncfModuleManagerRegister, serviceProvider);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemCoreRegister, serviceProvider);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemManagerRegister, serviceProvider);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(areasBaseRegister, serviceProvider);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(installerRegister, serviceProvider);

                //Legacy comment translated to English.
                if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                {
                    await InstallAndOpenModuleAsync(tenantRegister, serviceProvider);
                }

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, true, true);

                //Legacy comment translated to English.
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, true, true);
            }

            //Legacy comment translated to English.
            {
                List<string> installedList = new List<string> { "00000000-0000-0000-0001-000000000001", "00000000-0000-0000-0001-000000000002", "00000000-0000-0000-0001-000000000003", "00000000-0000-0000-0001-000000000004"
                , "00000000-0000-0000-0001-000000000005", "00000000-0000-0000-0001-000000000006","00000000-0000-0001-0001-000000000001", "62FBB022-B04E-423F-82FE-926D418A0815"};
                var _xncfModuleService = _xncfModuleServiceExtension;// serviceProvider.GetService<XncfModuleServiceExtension>();
                if (needModelList != null)
                {
                    foreach (var needModelId in needModelList)
                    {
                        if (!installedList.Contains(needModelId))
                        {
                            //var docRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == needModel);
                            var docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                            if (docModule == null)
                            {
                                await _xncfModuleService.InstallModuleAsync(needModelId);
                                docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                            }
                            //Legacy comment translated to English.
                            if (docModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                            {
                                docModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                                await _xncfModuleService.SaveObjectAsync(docModule);
                            }
                        }
                    }
                }
            }


            {
                var _xncfModuleService = _xncfModuleServiceExtension; // serviceProvider.GetService<XncfModuleServiceExtension>();

                //Legacy comment translated to English.
                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister, serviceProvider);
                //Legacy comment translated to English.
                adminModule ??= await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);

                //Legacy comment translated to English.
                await _xncfModuleService.SaveObjectAsync(adminModule);//.ConfigureAwait(false);

                var _systemConfigService = serviceProvider.GetService<SystemConfigService>();
                _systemConfigService.Init(systemName);//Legacy comment translated to English.
            }

            {
                //Legacy comment translated to English.

            }

            //Legacy comment translated to English.
            //Legacy comment translated to English.
        }

        private async Task InitDatabaseAsync(Func<Task<(bool, string)>> initDatabaseFunc)
        {
            //initialize the database
            var (initDbSuccess, initDbMsg) = await initDatabaseFunc();

            Console.WriteLine($"完成 systemCoreRegister.InitDatabase，是否成功：{initDbSuccess}。数据库信息：{initDbMsg}");

            if (!initDbSuccess)
            {
                throw new NcfDatabaseException($"ServiceRegister.InitDatabase 失败：{initDbMsg}", DatabaseConfigurationFactory.Instance.Current.GetType());
            }
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="register"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, IServiceProvider serviceProvider, bool installNow = true, bool addMenu = true)
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
                var _xncfModuleService = _xncfModuleServiceExtension; //serviceProvider.GetService<XncfModuleServiceExtension>();

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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        private bool VerifyInstallRequest(InstallRequestDto installRequestDto)
        {
            if (installRequestDto.DbConnectionString.IsNullOrEmpty())
            {
                return false;
            }

            if (installRequestDto.SystemName.IsNullOrEmpty())
            {
                return false;
            }

            if (installRequestDto.AdminUserName.IsNullOrEmpty())
            {
                return false;
            }
            return true;
        }


        public InstallerService(IServiceProvider serviceProvider, InstallOptionsService installOptionsService, XncfModuleServiceExtension xncfModuleServiceExtension)
        {
            this._serviceProvider = serviceProvider;
            this._installOptionsService = installOptionsService;
            this._xncfModuleServiceExtension = xncfModuleServiceExtension;
        }

        public GetDefaultInstallOptionsResponseDto GetDefaultInstallOptions()
        {
            var result = new GetDefaultInstallOptionsResponseDto();

            //Legacy comment translated to English.
            result.DbConnectionString = _installOptionsService.GetDbConnectionString();
            result.SystemName = _installOptionsService.GetDefaultSystemName();
            result.AdminUserName = _installOptionsService.GetDefaultAdminUserName();
            result.NeedModelList = _installOptionsService.GetModules();

            return result;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public async Task<InstallResponseDto> InstallAsync(InstallRequestDto installRequestDto, IServiceProvider serviceProvider)
        {
            using (var sope = serviceProvider.CreateScope())
            {
                var installResponseDto = new InstallResponseDto();
                installResponseDto.StatCode = 404;

                if (!SiteConfig.IsInstalling || installRequestDto == null)
                {
                    return installResponseDto;
                }

                if (VerifyInstallRequest(installRequestDto) == false)
                {
                    return installResponseDto;
                }

                //Legacy comment translated to English.
                if (installRequestDto.DbConnectionString != _installOptionsService.GetDbConnectionString())
                {
                    _installOptionsService.ResetDbConnectionString(installRequestDto.DbConnectionString);
                }

                #region Legacy section
                //Legacy comment translated to English.
                //var adminService = sope.ServiceProvider.GetService<AdminUserInfoService>();

                //var database = adminService.BaseClientRepository.BaseDB.BaseDataContext.Database;
                //Legacy comment translated to English.

                //Legacy comment translated to English.
                #endregion

                //Legacy comment translated to English.
                {
                    //Legacy comment translated to English.
                    if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                    {
                        try
                        {
                            //initialize the database

                            //var (initDbSuccess, initDbMsg) = await systemCoreRegister.InitDatabase(_serviceProvider/*, _tenantInfoService, *//*_httpContextAccessor.Value.HttpContext*/);

                            //Legacy comment translated to English.
                            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();
                            await tenantRegister.InstallOrUpdateAsync(sope.ServiceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
                        }
                        catch (Exception ex)
                        {
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            Console.WriteLine(ex.Message);
                            throw;
                        }
                        finally
                        {

                        }
                    }

                    Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                    var adminModule = await InstallAndOpenModuleAsync(adminRegister, sope.ServiceProvider, installNow: false, addMenu: false);
                }

                //Legacy comment translated to English.
                {
                    var cacheStrategy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                    using (var cacheLock = await cacheStrategy.BeginCacheLockAsync("InstallerService", "Install"))
                    {
                        var _accountInfoService = sope.ServiceProvider.GetService<AdminUserInfoService>();

                        // Re-check inside the distributed install lock to prevent a
                        // second request from reinitializing an already installed site.
                        try
                        {
                            if (await _accountInfoService.GetObjectAsync(z => true) != null)
                            {
                                installResponseDto.StatCode = 404;
                                return installResponseDto;
                            }
                        }
                        catch (Exception ex) when (InstallDatabaseState.IsDatabaseUnavailableForInstallation(ex))
                        {
                            // The target database may not be available yet. The
                            // installation path will create/update its schema.
                        }

                        var adminUserInfoResult = await _accountInfoService.InitAsync(installRequestDto.AdminUserName);//Legacy comment translated to English.

                        var adminUserInfo = adminUserInfoResult.AdminUserInfo;
                        if (adminUserInfo == null)
                        {
                            installResponseDto.StatCode = 404;
                            return installResponseDto;
                        }
                        else
                        {
                            installResponseDto.Step = 1;

                            //Legacy comment translated to English.
                            await InitSystemAsync(installRequestDto.SystemName, sope.ServiceProvider, installRequestDto.NeedModelList, adminUserInfo.Id);

                            //IXncfRegister systemRegister = XncfRegisterManager.RegisterList.First(z => z.GetType() == typeof(Senparc.Areas.Admin.Register));
                            //Legacy comment translated to English.

                            installResponseDto.AdminUserName = installRequestDto.AdminUserName;
                            installResponseDto.AdminPassword = adminUserInfoResult.Password;//Legacy comment translated to English.
                            installResponseDto.StatCode = 0;
                        }
                    }
                }
                return installResponseDto;
            }
        }
    }
}
