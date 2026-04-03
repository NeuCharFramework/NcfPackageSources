using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Senparc.AI.Kernel;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.OHS.Local.AppService;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.OHS.Remote;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.Instraller.Pages
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel //No base class is used because it cannot be automatically detected by installed programs
    {
        private readonly AdminUserInfoService _accountInfoService;
        private readonly InstallAppService _installAppService;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///system name
        /// </summary>
        public string SystemName { get; set; }
        /// <summary>
        ///admin username
        /// </summary>
        public string AdminUserName { get; set; }
        /// <summary>
        ///admin password
        /// </summary>
        public string AdminPassword { get; set; }
        /// <summary>
        /// database connection string
        /// </summary>
        public string DbConnectionString { get; set; }
        /// <summary>
        /// Namespace that needs to be modified
        /// </summary>
        public string Namespace { get; set; }
        public int Step { get; set; }

        /// <summary>
        /// Modules that need to be installed
        /// </summary>
        public List<XncfRegisterDto> NeedModelList { get; set; }


        public MultipleDatabaseType MultipleDatabaseType { get; set; }

        /// <summary>
        /// Newly created RequestTenantInfo
        /// </summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }

        public bool InstallingTenant { get; private set; }
        public TenantInfoDto TenantInfoDto { get; private set; }
        public TenantRule TenantRule { get; set; }
        public bool MultiTenantEnable { get; set; }

        public IndexModel(IServiceProvider serviceProvider,
            AdminUserInfoService accountService,
            XncfModuleServiceExtension xncfModuleServiceEx,
            InstallAppService installAppService)
        {
            _serviceProvider = serviceProvider;
            _accountInfoService = accountService;
            this._installAppService = installAppService;

            MultiTenantEnable = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
            TenantRule = SiteConfig.SenparcCoreSetting.TenantRule;
        }

        public async Task<IActionResult> OnGetAsync(string forceUpdateModule,int tenantId=0)
        {
            try
            {
                Console.WriteLine("进入安装程序，检测是否需要初始化");

                if (Request.IsLocal())
                {
                    if (!forceUpdateModule.IsNullOrEmpty())
                    {
                        //Force local installation
                        Console.WriteLine("强制升级模块：" + forceUpdateModule);

                        SenparcTrace.SendCustomLog("强制更新模块", $"开始：{forceUpdateModule}");
                        var register = Senparc.CO2NET.Helpers.ReflectionHelper.CreateInstance<IXncfRegister>(forceUpdateModule + ".Register", forceUpdateModule);
                        await register.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Update);
                        SenparcTrace.SendCustomLog("强制更新模块", $"完成：{forceUpdateModule}");

                        return Content($"强制手动升级已完成：{forceUpdateModule}，请继续更新其他模块，或重新打开首页。");
                    }
                }

                MultipleDatabaseType = DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
                var adminUserInfo = await _accountInfoService.GetObjectAsync(z => true);//Check if it has been initialized
                if (adminUserInfo == null)
                {
                    Console.WriteLine("需要初始化");
                    throw new Exception("需要初始化");
                }

                // try
                // {
                //     if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                //     {
                //         //Determine whether to enter from a new domain name
                //         RequestTenantInfo currentRequestTenantInfo = MultiTenantHelper.TryGetAndCheckRequestTenantInfo(_serviceProvider, null);
                //     }
                // }
                // catch (Exception)
                // {
                //     throw;
                // }

            }
            catch (Exception)
            {
                SiteConfig.IsInstalling = true;

                Console.WriteLine("开始初始化");

                //var database = _accountInfoService.BaseClientRepository.BaseDB.BaseDataContext.Database;
                //var created = await database.EnsureCreatedAsync();//Try to create a database

                //await Console.Out.WriteLineAsync("Attempt to create database:" + (created ? "Successfully created" : "Already exists, no need to create"));

                //Default values ​​of configuration items displayed on the initialization page
                var result = await _installAppService.GetInstallOptionsAsync();
                SystemName = result.Data.SystemName;
                AdminUserName = result.Data.AdminUserName;
                DbConnectionString = result.Data.DbConnectionString;
                NeedModelList = result.Data.NeedModelList;

                return Page();
            }

            //base.Response.StatusCode = 404;

            SenparcTrace.SendCustomLog("风险提示", "Install 被访问，已返回 404 进行混淆。如果您已经确保完成项目初始化，建议移除 Senparc.Xncf.Install 模块");

            return new StatusCodeResult(404);//If the installation has been completed and there is an administrator, the installation will not proceed.
        }

        public async Task<IActionResult> OnPostAsync([FromBody] InstallRequestDto installRequestDto)
        {
            //Start installation
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

            //Add initial multi-tenant information
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                var tenantReulst = await _installAppService.GetTenantInfoAsync();
                TenantInfoDto = tenantReulst.Data;
            }

            //Undo installation status
            SiteConfig.IsInstalling = false;
            SiteConfig.SetInstallFinished();
            TenantMiddleware.FirstRunAndInstalling = false;

            return Page();
        }

        public IActionResult OnGetDefaultOptions()
        {
            return new JsonResult(_installAppService.GetInstallOptionsAsync());
        }
    }
}