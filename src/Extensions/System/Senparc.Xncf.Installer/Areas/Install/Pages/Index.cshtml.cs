/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260729
    Change description: v0.4.1 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.12 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.AgentKernel;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.Domain.Services;
using Senparc.Xncf.Installer.OHS.Local.AppService;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.OHS.Remote;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Senparc.Xncf.Installer;

namespace Senparc.Xncf.Instraller.Pages
{
    [AutoValidateAntiforgeryToken]
    public class IndexModel : PageModel //Legacy comment translated to English.
    {
        private readonly AdminUserInfoService _accountInfoService;
        private readonly InstallAppService _installAppService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IStringLocalizer<InstallerResource> _localizer;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SystemName { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AdminUserName { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AdminPassword { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DbConnectionString { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Namespace { get; set; }
        public int Step { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<XncfRegisterDto> NeedModelList { get; set; }


        public MultipleDatabaseType MultipleDatabaseType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }

        public bool InstallingTenant { get; private set; }
        public TenantInfoDto TenantInfoDto { get; private set; }
        public TenantRule TenantRule { get; set; }
        public bool MultiTenantEnable { get; set; }

        public IndexModel(IServiceProvider serviceProvider,
            AdminUserInfoService accountService,
            XncfModuleServiceExtension xncfModuleServiceEx,
            InstallAppService installAppService,
            IStringLocalizer<InstallerResource> localizer)
        {
            _serviceProvider = serviceProvider;
            _accountInfoService = accountService;
            this._installAppService = installAppService;
            _localizer = localizer;

            MultiTenantEnable = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
            TenantRule = SiteConfig.SenparcCoreSetting.TenantRule;
        }

        public async Task<IActionResult> OnGetAsync(string forceUpdateModule,int tenantId=0)
        {
            Console.WriteLine("进入安装程序，检测是否需要初始化");

            if (Request.IsLocal() && !forceUpdateModule.IsNullOrEmpty())
            {
                //Legacy comment translated to English.
                Console.WriteLine("强制升级模块：" + forceUpdateModule);

                SenparcTrace.SendCustomLog("强制更新模块", $"开始：{forceUpdateModule}");
                var register = Senparc.CO2NET.Helpers.ReflectionHelper.CreateInstance<IXncfRegister>(forceUpdateModule + ".Register", forceUpdateModule);
                await register.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Update);
                SenparcTrace.SendCustomLog("强制更新模块", $"完成：{forceUpdateModule}");

                return Content(_localizer["Install.ForceUpgrade.Completed", forceUpdateModule]);
            }

            var installFinished = SiteConfig.CheckInstallFinishedFileExisted();

            try
            {
                MultipleDatabaseType = DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
                var adminUserInfo = await _accountInfoService.GetObjectAsync(z => true);//Legacy comment translated to English.
                if (adminUserInfo != null)
                {
                    // A completed database can outlive the local marker file
                    // (for example after a deployment or a copied App_Data
                    // directory). Reconcile that state before deciding that the
                    // installer should be hidden.
                    if (!installFinished && IsSystemInitialized())
                    {
                        SiteConfig.IsInstalling = false;
                        await SiteConfig.SetInstallFinishedAsync();
                        return new RedirectResult("/");
                    }

                    SenparcTrace.SendCustomLog("风险提示", "Install 被访问，已返回 404 进行混淆。如果您已经确保完成项目初始化，建议移除 Senparc.Xncf.Install 模块");
                    return new StatusCodeResult(404);
                }

                SiteConfig.IsInstalling = true;
                Console.WriteLine("需要初始化，开始加载安装选项");
                var result = await _installAppService.GetInstallOptionsAsync();
                SystemName = result.Data.SystemName;
                AdminUserName = result.Data.AdminUserName;
                DbConnectionString = result.Data.DbConnectionString;
                NeedModelList = result.Data.NeedModelList;
                return Page();
            }
            catch (Exception ex) when (DatabaseInstallState.IsSchemaUpgradeRequired(ex))
            {
                //Legacy comment translated to English.
                return new RedirectResult("/Maintenance/DatabaseUpgrade");
            }
            catch (Exception ex) when (InstallDatabaseState.IsDatabaseUnavailableForInstallation(ex))
            {
                // Preserve the original installation entry behavior: an
                // unavailable or not-yet-created database is an expected state
                // while the installer is being opened. The POST operation still
                // has to create/update the database successfully.
                SiteConfig.IsInstalling = true;
                Console.WriteLine("开始初始化");

                var result = await _installAppService.GetInstallOptionsAsync();
                SystemName = result.Data.SystemName;
                AdminUserName = result.Data.AdminUserName;
                DbConnectionString = result.Data.DbConnectionString;
                NeedModelList = result.Data.NeedModelList;
                return Page();
            }
            catch (Exception ex)
            {
                SiteConfig.IsInstalling = false;
                SenparcTrace.BaseExceptionLog(ex);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        public async Task<IActionResult> OnPostAsync([FromBody] InstallRequestDto installRequestDto)
        {
            if (!SiteConfig.IsInstalling || installRequestDto == null)
            {
                return new StatusCodeResult(404);
            }

            try
            {
                if (await _accountInfoService.GetObjectAsync(z => true) != null)
                {
                    SiteConfig.IsInstalling = false;
                    return new StatusCodeResult(404);
                }
            }
            catch (Exception ex) when (InstallDatabaseState.IsDatabaseUnavailableForInstallation(ex))
            {
                // InstallerService creates/updates the schema before its guarded
                // re-check. A missing schema or unavailable SQL Server target
                // therefore remains a valid first-install request.
            }
            catch (Exception ex)
            {
                SiteConfig.IsInstalling = false;
                SenparcTrace.BaseExceptionLog(ex);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (string.IsNullOrWhiteSpace(installRequestDto.PreferredCulture))
            {
                installRequestDto.PreferredCulture =
                    HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture?.Name;
            }

            //Legacy comment translated to English.
            var result = await _installAppService.InstallAsync(installRequestDto);
            if (result.Success != true)
            {
                if (result.Data == null)
                {
                    return new StatusCodeResult(505);
                }
                return new StatusCodeResult(result.Data.StatCode);
            }

            AdminUserName = result.Data.AdminUserName;
            AdminPassword = result.Data.AdminPassword;
            Step = result.Data.Step;

            MultiTenantEnable = SiteConfig.SenparcCoreSetting.EnableMultiTenant;

            //Legacy comment translated to English.
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                var tenantReulst = await _installAppService.GetTenantInfoAsync();
                TenantInfoDto = tenantReulst.Data;
            }

            //Legacy comment translated to English.
            SiteConfig.IsInstalling = false;
            await SiteConfig.SetInstallFinishedAsync();
            TenantMiddleware.FirstRunAndInstalling = false;

            return Page();
        }

        public IActionResult OnGetDefaultOptions()
        {
            return new JsonResult(_installAppService.GetInstallOptionsAsync());
        }

        private bool IsSystemInitialized()
        {
            try
            {
                return _serviceProvider.GetService<FullSystemConfigCache>()?.Data != null;
            }
            catch (NcfUninstallException)
            {
                return false;
            }
        }
    }
}
