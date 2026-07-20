using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class XncfModuleIndexModel : BaseAdminPageModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx;
        private readonly SysMenuService _sysMenuService;
        private readonly IStringLocalizer<AdminResource> _localizer;

        //TODO:从其他模块获得，或独立到对应模块的API
        private readonly Lazy<SystemConfigService> _systemConfigService;

        public XncfModuleIndexModel(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleServiceEx,
            SysMenuService sysMenuService, Lazy<SystemConfigService> systemConfigService,
            IStringLocalizer<AdminResource> localizer)
            : base(serviceProvider)
        {
            CurrentMenu = "XncfModule";

            this._serviceProvider = serviceProvider;
            this._xncfModuleServiceEx = xncfModuleServiceEx;
            this._sysMenuService = sysMenuService;
            this._systemConfigService = systemConfigService;
            this._localizer = localizer;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 数据库已存的XncfModules
        /// </summary>
        public PagedList<XncfModule> XncfModules { get; set; }
        public List<IXncfRegister> NewXncfRegisters { get; set; }

        private void LoadNewXncfRegisters(PagedList<XncfModule> xncfModules)
        {
            NewXncfRegisters = XncfRegisterManager.RegisterList.Where(z => !z.IgnoreInstall && !xncfModules.Exists(m => m.Uid == z.Uid && m.Version == z.Version)).ToList() ?? new List<IXncfRegister>();
        }

        public async Task OnGetAsync()
        {
            //更新菜单缓存
            await _sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
            XncfModules = await _xncfModuleServiceEx.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            LoadNewXncfRegisters(XncfModules);
        }

        /// <summary>
        /// 扫描新模块
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetScanAsync(string uid)
        {
            var result = await _xncfModuleServiceEx.InstallModuleAsync(uid, true);
            XncfModules = result.Item1;
            base.SetMessager(Ncf.Core.Enums.MessageType.info, result.Item2, true);

            //if (backpage=="Start")
            return RedirectToPage("Start", new { uid = uid });//始终到详情页
            //return RedirectToPage("Index");
        }

        /// <summary>
        /// 隐藏“模块管理”功能
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostHideManagerAsync()
        {
            //TODO:使用DTO操作
            var systemConfig = _systemConfigService.Value.GetObject(z => true);
            systemConfig.Update(systemConfig.SystemName, systemConfig.MchId, systemConfig.MchKey, systemConfig.TenPayAppId,
                systemConfig.HideModuleManager.HasValue && systemConfig.HideModuleManager.Value == true ? false : true);
            await _systemConfigService.Value.SaveObjectAsync(systemConfig);
            if (systemConfig.HideModuleManager == true)
            {
                return RedirectToPage("../Index");
            }
            else
            {
                return RedirectToPage("./Index");
            }
        }

        /// <summary>
        /// 隐藏“模块管理”功能 handler=HideManagerAjax
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostHideManagerAjaxAsync()
        {
            //TODO:使用DTO操作
            var systemConfig = _systemConfigService.Value.GetObject(z => true);
            systemConfig.Update(systemConfig.SystemName, systemConfig.MchId, systemConfig.MchKey, systemConfig.TenPayAppId,
                            systemConfig.HideModuleManager.HasValue && systemConfig.HideModuleManager.Value == true ? false : true); await _systemConfigService.Value.SaveObjectAsync(systemConfig);
            //if (systemConfig.HideModuleManager == true)
            //{
            //    return RedirectToPage("../Index");
            //}
            //else
            //{
            //    return RedirectToPage("./Index");
            //}
            return Ok(new { systemConfig.HideModuleManager });
        }

        /// <summary>
        /// 获取已安装模块 handler=Modules
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMofulesAsync(int pageIndex = 0, int pageSize = 0)
        {
            //更新菜单缓存
            await _sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
            PagedList<XncfModule> xncfModules = await _xncfModuleServiceEx.GetObjectListAsync(pageIndex, pageSize, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            //xncfModules.FirstOrDefault().
            var xncfRegisterList = XncfRegisterList.Select(_ => new { _.Uid, homeUrl = _.GetAreaHomeUrl(), _.Icon });
            var result = from xncfModule in xncfModules
                         join xncfRegister in xncfRegisterList on xncfModule.Uid equals xncfRegister.Uid
                         into xncfRegister_left
                         from xncfRegister in xncfRegister_left.DefaultIfEmpty()
                         select new
                         {
                             xncfModule,
                             xncfRegister
                         };
            return Ok(new { result, FullSystemConfig.HideModuleManager });
        }

        /// <summary>
        /// 获取未安装模块 handler=UnModules
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetUnMofulesAsync()
        {
            //所有已安装的模块
            var oldXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, z => true, z => z.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            //未安装或版本已更新（不同）的模块
            //var newXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(oldXncfModules);
            var newXncfRegisters = _xncfModuleServiceEx.GetOnlyUnInstallXncfModule(oldXncfModules);

            return Ok(newXncfRegisters.Select(z => new
            {
                z.MenuName,
                z.Name,
                z.Uid,
                Version = _xncfModuleServiceEx.GetVersionDisplayName(oldXncfModules, z),
                z.Icon
            })); ;
        }

        /// <summary>
        /// 获取待更新模块 handler=UpdatedModules
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetUpdatedMofulesAsync()
        {
            //所有已安装的模块
            var oldXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, z => true, z => z.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            //未安装或版本已更新（不同）的模块
            var newXncfRegisters = _xncfModuleServiceEx.GetUpdatedInstallXncfModule(oldXncfModules);

            return Ok(newXncfRegisters.Select(z => new
            {
                z.MenuName,
                z.Name,
                z.Uid,
                Version = _xncfModuleServiceEx.GetVersionDisplayName(oldXncfModules, z),
                z.Icon
            })); ;
        }

        /// <summary>
        /// 扫描新模块 handler=ScanAjax
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetScanAjaxAsync(string uid)
        {
            var result = await _xncfModuleServiceEx.InstallModuleAsync(uid, true);
            //XncfModules = result.Item1;
            //base.SetMessager(Ncf.Core.Enums.MessageType.info, result.Item2, true);
            return Ok(result.XncfModuleList);
            //return RedirectToPage("Index");
        }

        /// <summary>
        /// 逐一更新所有待更新模块，并根据选项在每个模块更新成功后开启模块。
        /// handler=BatchUpdate
        /// </summary>
        /// <param name="request">批量更新选项</param>
        /// <returns>每个模块的更新结果</returns>
        public async Task<IActionResult> OnPostBatchUpdateAsync([FromBody] BatchUpdateXncfModulesRequest request)
        {
            var enableAfterUpdate = request?.EnableAfterUpdate ?? false;
            var installedModules = await _xncfModuleServiceEx
                .GetObjectListAsync(0, 0, z => true, z => z.AddTime, Ncf.Core.Enums.OrderingType.Descending)
                .ConfigureAwait(false);
            var modulesToUpdate = _xncfModuleServiceEx
                .GetUpdatedInstallXncfModule(installedModules)
                .OrderBy(z => z.MenuName)
                .ToList();
            var results = new List<BatchUpdateXncfModuleResult>();

            // 模块的安装/迁移过程不并行执行，避免多个模块同时修改数据库和菜单。
            foreach (var register in modulesToUpdate)
            {
                var installedModule = installedModules.First(z => z.Uid == register.Uid);
                var item = new BatchUpdateXncfModuleResult
                {
                    Uid = register.Uid,
                    ModuleName = register.MenuName,
                    CurrentVersion = installedModule.Version,
                    TargetVersion = register.Version
                };

                try
                {
                    await _xncfModuleServiceEx.InstallModuleAsync(register.Uid, true).ConfigureAwait(false);
                    item.Updated = true;

                    if (enableAfterUpdate)
                    {
                        var updatedModule = await _xncfModuleServiceEx
                            .GetObjectAsync(z => z.Uid == register.Uid)
                            .ConfigureAwait(false);
                        if (updatedModule == null)
                        {
                            throw new InvalidOperationException(_localizer["Xncf.ModuleNotInstalled"]);
                        }

                        if (updatedModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                        {
                            updatedModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                            await _xncfModuleServiceEx.SaveObjectAsync(updatedModule).ConfigureAwait(false);
                        }

                        item.Enabled = true;
                    }

                    item.Success = true;
                    item.Message = enableAfterUpdate
                        ? _localizer["Xncf.BatchUpdate.UpdateAndEnableSuccess"]
                        : _localizer["Common.UpdateSuccess"];
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog(
                        "批量更新 XNCF 模块失败",
                        $"模块：{register.MenuName} / {register.Uid}\r\n{ex}");
                    item.Message = item.Updated
                        ? _localizer["Xncf.BatchUpdate.EnableFailed", ex.Message]
                        : _localizer["Common.UpdateFailed", ex.Message];
                }

                results.Add(item);
            }

            if (results.Any(z => z.Updated))
            {
                await _sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
            }

            var successCount = results.Count(z => z.Success);
            return Ok(new
            {
                Success = successCount == results.Count,
                TotalCount = results.Count,
                SuccessCount = successCount,
                FailureCount = results.Count - successCount,
                EnableAfterUpdate = enableAfterUpdate,
                Items = results
            });
        }

        /// <summary>
        /// 根据名称安装模块
        /// </summary>
        /// <param name="xncfName"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetInstallModuleAsync(string xncfName)
        {
            bool success = true;
            string message = null;
            if (base.FullSystemConfig.HideModuleManager == true)
            {
                success = false;
                message = _localizer["Xncf.Install.PublishModeEnabled"];
            }
            else
            {
                var docRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Name == xncfName);
                if (docRegister == null)
                {
                    success = false;
                    message = _localizer["Xncf.Install.ModuleNotFound"];
                }
                else
                {
                    try
                    {
                        //查找并安装模块
                        var docModule = await _xncfModuleServiceEx.GetObjectAsync(z => z.Uid == docRegister.Uid);
                        if (docModule == null)
                        {
                            await _xncfModuleServiceEx.InstallModuleAsync(docRegister.Uid, true);
                            docModule = await _xncfModuleServiceEx.GetObjectAsync(z => z.Uid == docRegister.Uid);
                        }
                        //开启模块
                        if (docModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                        {
                            docModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                            await _xncfModuleServiceEx.SaveObjectAsync(docModule);
                        }

                        message = _localizer["Xncf.Install.Success"];
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = _localizer["Xncf.Install.Failed", ex.Message];
                    }
                }
            }

            return new JsonResult(new { success, message });

        }
    }

    public class BatchUpdateXncfModulesRequest
    {
        public bool EnableAfterUpdate { get; set; }
    }

    public class BatchUpdateXncfModuleResult
    {
        public string Uid { get; set; }
        public string ModuleName { get; set; }
        public string CurrentVersion { get; set; }
        public string TargetVersion { get; set; }
        public bool Updated { get; set; }
        public bool Enabled { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
