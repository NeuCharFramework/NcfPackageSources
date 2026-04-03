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
        /// Newly created RequestTenantInfo
        /// </summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }
        public TenantRule TenantRule { get; set; }
        public bool MultiTenantEnable { get; set; }
        /// <summary>
        ///Initialize the installation system
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
                await InitDatabaseAsync(() => systemCoreRegister.InitDatabase(serviceProvider));//TODO: It is currently invalid and no database code will be built. This is just a placeholder.

                //await InitDatabaseAsync(() => systemManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => systemPermissionRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => xncfModuleManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => menuRegister.InitDatabase(_serviceProvider));


                //Start installing permission module
                //(Must be placed first, other module operations need to rely on this module)
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, installNow: false, addMenu: false);

                //Start installing the menu management module
                //(Must be placed second, other module operations need to rely on this module)
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, installNow: false, addMenu: false);

                try
                {
                    SysMenuService _sysMenuService = serviceProvider.GetService<SysMenuService>();

                    _sysMenuService.Init(null, adminUserInfoId);
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("_sysMenuService.Init 异常", ex.Message);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }

                //Start installing the module management module
                //(Must be placed in the third position, other module operations need to rely on this module)
                await InstallAndOpenModuleAsync(xncfModuleManagerRegister, serviceProvider);

                //Start installing system basic modules
                await InstallAndOpenModuleAsync(systemCoreRegister, serviceProvider);

                //Start installing the system management management module
                await InstallAndOpenModuleAsync(systemManagerRegister, serviceProvider);

                //Start installing the Areas module
                await InstallAndOpenModuleAsync(areasBaseRegister, serviceProvider);

                //Start installing the Installer module
                await InstallAndOpenModuleAsync(installerRegister, serviceProvider);

                //Install tenant module
                if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                {
                    await InstallAndOpenModuleAsync(tenantRegister, serviceProvider);
                }

                //Install the permission module
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, true, true);

                //Install the menu module
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, true, true);
            }

            //TODO: Selectively install user-defined modules
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
                            //Open module
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

                //Start installing and enabling system modules (Admin)
                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister, serviceProvider);
                //Make sure it has been assigned
                adminModule ??= await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);

                //Save (all) changes at once
                await _xncfModuleService.SaveObjectAsync(adminModule);//.ConfigureAwait(false);

                var _systemConfigService = serviceProvider.GetService<SystemConfigService>();
                _systemConfigService.Init(systemName);//Initialize system information
            }

            {
                //Start installing user modules (if any) TODO: Selective installation

            }

            //((SenparcEntities)_accountInfoService.BaseData.BaseDB.BaseDataContext).ResetMigrate();//Reset merge status
            //((SenparcEntities)_accountInfoService.BaseData.BaseDB.BaseDataContext).Migrate();//Merging
        }

        private async Task InitDatabaseAsync(Func<Task<(bool, string)>> initDatabaseFunc)
        {
            //Initialize database
            var (initDbSuccess, initDbMsg) = await initDatabaseFunc();

            Console.WriteLine($"完成 systemCoreRegister.InitDatabase，是否成功：{initDbSuccess}。数据库信息：{initDbMsg}");

            if (!initDbSuccess)
            {
                throw new NcfDatabaseException($"ServiceRegister.InitDatabase 失败：{initDbMsg}", DatabaseConfigurationFactory.Instance.Current.GetType());
            }
        }


        /// <summary>
        ///Install and open the module
        /// </summary>
        /// <param name="register"></param>
        /// <param name="installNow">Whether to install now</param>
        /// <param name="addMenu">Whether to add a menu (must be installedNow is true to take effect)</param>
        /// <returns></returns>
        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, IServiceProvider serviceProvider, bool installNow = true, bool addMenu = true)
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

        /// <summary>
        /// Check whether the values ​​of various configurations in the installation request are legal.
        /// </summary>
        /// <param name="installRequestDto">Installation request</param>
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

            //Read the default value of an existing configuration
            result.DbConnectionString = _installOptionsService.GetDbConnectionString();
            result.SystemName = _installOptionsService.GetDefaultSystemName();
            result.AdminUserName = _installOptionsService.GetDefaultAdminUserName();
            result.NeedModelList = _installOptionsService.GetModules();

            return result;
        }

        /// <summary>
        /// Execute the installation command of the default package
        /// </summary>
        /// <returns></returns>
        public async Task<InstallResponseDto> InstallAsync(InstallRequestDto installRequestDto, IServiceProvider serviceProvider)
        {
            using (var sope = serviceProvider.CreateScope())
            {
                var installResponseDto = new InstallResponseDto();
                installResponseDto.StatCode = 404;

                if (VerifyInstallRequest(installRequestDto) == false)
                {
                    return installResponseDto;
                }

                //Compare the incoming and original database connection strings
                if (installRequestDto.DbConnectionString != _installOptionsService.GetDbConnectionString())
                {
                    _installOptionsService.ResetDbConnectionString(installRequestDto.DbConnectionString);
                }

                #region 请勿使用 EnsureCreatedAsync() 方法，系统将自动通过 database update 过程创建数据库
                //Re-establish the Service 
                //var adminService = sope.ServiceProvider.GetService<AdminUserInfoService>();

                //var database = adminService.BaseClientRepository.BaseDB.BaseDataContext.Database;
                //var created = await database.EnsureCreatedAsync();//Try to create a database

                //await Console.Out.WriteLineAsync("Attempt to create database:" + (created ? "Successfully created" : "Already exists, no need to create"));
                #endregion

                //Original Get request
                {
                    //Add initial multi-tenant information
                    if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                    {
                        try
                        {
                            //Initialize database

                            //var (initDbSuccess, initDbMsg) = await systemCoreRegister.InitDatabase(_serviceProvider/*, _tenantInfoService, *//*_httpContextAccessor.Value.HttpContext*/);

                            //Install multi-tenancy
                            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();
                            await tenantRegister.InstallOrUpdateAsync(sope.ServiceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
                        }
                        catch (Exception ex)
                        {
                            //If it has already been installed, it will not be processed.
                            //TODO:Specific Exception
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

                //Executed after the original Post
                {
                    var cacheStrategy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                    using (var cacheLock = await cacheStrategy.BeginCacheLockAsync("InstallerService", "Install"))
                    {
                        var _accountInfoService = sope.ServiceProvider.GetService<AdminUserInfoService>();

                        var adminUserInfoResult = await _accountInfoService.InitAsync(installRequestDto.AdminUserName);//Initialize administrator information

                        var adminUserInfo = adminUserInfoResult.AdminUserInfo;
                        if (adminUserInfo == null)
                        {
                            installResponseDto.StatCode = 404;
                            return installResponseDto;
                        }
                        else
                        {
                            installResponseDto.Step = 1;

                            //Perform system initialization installation
                            await InitSystemAsync(installRequestDto.SystemName, sope.ServiceProvider, installRequestDto.NeedModelList, adminUserInfo.Id);

                            //IXncfRegister systemRegister = XncfRegisterManager.RegisterList.First(z => z.GetType() == typeof(Senparc.Areas.Admin.Register));
                            //await _xncfModuleService.InstallMenuAsync(systemRegister, Ncf.Core.Enums.InstallOrUpdate.Install);//Install menu

                            installResponseDto.AdminUserName = installRequestDto.AdminUserName;
                            installResponseDto.AdminPassword = adminUserInfoResult.Password;//adminUserInfo.Password cannot be used here because this parameter is already encrypted information
                            installResponseDto.StatCode = 0;
                        }
                    }
                }
                return installResponseDto;
            }
        }
    }
}
