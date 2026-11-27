using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
using Senparc.Xncf.AIAgentsHub.Tests.Domain.Dto;
using Senparc.Xncf.AIAgentsHub.Tests.InstallServices;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.XncfModuleManager.Domain.Services;

namespace Senparc.Xncf.AIAgentsHub.Domain.Services
{
    /// <summary>
    /// Coordinates system initialization, module installation, and validation of
    /// the AIAgentsHub test installation workflow.
    /// </summary>
    public class InstallerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly InstallOptionsService _installOptionsService;
        private readonly XncfModuleServiceExtension _xncfModuleServiceExtension;

        /// <summary>Gets or sets the tenant context created during installation.</summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }
        /// <summary>Gets or sets the tenant-selection rule used by installation.</summary>
        public TenantRule TenantRule { get; set; }
        /// <summary>Gets or sets whether multi-tenant installation is enabled.</summary>
        public bool MultiTenantEnable { get; set; }
        /// <summary>
        /// Initializes core system modules, database state, menus, and configuration
        /// for the requested installation.
        /// </summary>
        /// <param name="systemName">The system name persisted in system configuration.</param>
        /// <param name="adminUserInfoId">The administrator identifier used for menu initialization.</param>
        /// <param name="serviceProvider">The scoped provider used by module initialization.</param>
        private async Task InitSystemAsync(string systemName, int adminUserInfoId, IServiceProvider serviceProvider)
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

                    _sysMenuService.Init(null, adminUserInfoId);
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
                //await InstallAndOpenModuleAsync(installerRegister, serviceProvider);

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

            {
                var _xncfModuleService = _xncfModuleServiceExtension;

                var _systemConfigService = serviceProvider.GetService<SystemConfigService>();
                _systemConfigService.Init(systemName);//Legacy comment translated to English.
            }

            {
                //Legacy comment translated to English.

            }

            //Legacy comment translated to English.
            //Legacy comment translated to English.
        }

        /// <summary>Runs a database initializer and converts failure into an NCF database exception.</summary>
        /// <param name="initDatabaseFunc">The initializer returning success and diagnostic text.</param>
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
        /// Installs or updates an XNCF register and optionally opens the persisted
        /// module and menu.
        /// </summary>
        /// <param name="register">The XNCF register to initialize.</param>
        /// <param name="serviceProvider">The provider used by the register.</param>
        /// <param name="installNow">Whether to create/open the persisted module immediately.</param>
        /// <param name="addMenu">Whether module installation should add its menu.</param>
        /// <returns>The persisted module when installation is performed.</returns>
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
                var _xncfModuleService = _xncfModuleServiceExtension;// serviceProvider.GetService<XncfModuleServiceExtension>();

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
        /// Validates the minimum database, system-name, and administrator fields
        /// required by installation.
        /// </summary>
        /// <param name="installRequestDto">The submitted installation request.</param>
        /// <returns><see langword="true"/> when all required values are present.</returns>
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


        /// <summary>Initializes the installer service and its module/options dependencies.</summary>
        /// <param name="serviceProvider">The installation service provider.</param>
        /// <param name="installOptionsService">The default/options persistence service.</param>
        /// <param name="xncfModuleServiceExtension">The XNCF module installation service.</param>
        public InstallerService(IServiceProvider serviceProvider, InstallOptionsService installOptionsService, XncfModuleServiceExtension xncfModuleServiceExtension)
        {
            this._serviceProvider = serviceProvider;
            this._installOptionsService = installOptionsService;
            this._xncfModuleServiceExtension = xncfModuleServiceExtension;
        }

        /// <summary>Gets the default values shown by the installation form.</summary>
        /// <returns>The default database, system-name, and administrator values.</returns>
        public GetDefaultInstallOptionsResponseDto GetDefaultInstallOptions()
        {
            var result = new GetDefaultInstallOptionsResponseDto();

            //Legacy comment translated to English.
            result.DbConnectionString = _installOptionsService.GetDbConnectionString();
            result.SystemName = _installOptionsService.GetDefaultSystemName();
            result.AdminUserName = _installOptionsService.GetDefaultAdminUserName();

            return result;
        }

        /// <summary>Executes installation after validating and applying the submitted options.</summary>
        /// <param name="installRequestDto">The submitted installation request.</param>
        /// <param name="serviceProvider">The provider used by installation operations.</param>
        /// <returns>The installation result and status code.</returns>
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

                    //Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                    //var adminModule = await InstallAndOpenModuleAsync(adminRegister, sope.ServiceProvider, installNow: false, addMenu: false);
                }

                //Legacy comment translated to English.
                //{
                //    var cacheStrategy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                //    using (var cacheLock = await cacheStrategy.BeginCacheLockAsync("InstallerService", "Install"))
                //    {
                //        var _accountInfoService = sope.ServiceProvider.GetService<AdminUserInfoService>();

                //Legacy comment translated to English.

                //        if (adminUserInfo == null)
                //        {
                //            installResponseDto.StatCode = 404;
                //            return installResponseDto;
                //        }
                //        else
                //        {
                //            installResponseDto.Step = 1;

                //Legacy comment translated to English.
                //            await InitSystemAsync(installRequestDto.SystemName, sope.ServiceProvider);

                //            //IXncfRegister systemRegister = XncfRegisterManager.RegisterList.First(z => z.GetType() == typeof(Senparc.Areas.Admin.Register));
                //Legacy comment translated to English.

                //            installResponseDto.AdminUserName = installRequestDto.AdminUserName;
                //Legacy comment translated to English.
                //            installResponseDto.StatCode = 0;
                //        }
                //    }
                //}
                return installResponseDto;
            }
        }
    }
}
