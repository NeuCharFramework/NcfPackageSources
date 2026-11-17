/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Index.cshtml.cs
    File description: Index.cshtml related feature implementation
    
    
    Created by: Senparc - 20241028
    
    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Utility;
using Senparc.Xncf.SystemManager.Domain.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class SystemConfig_IndexModel(IServiceProvider serviceProvider,
        SystemConfigService systemConfigService,
        IStringLocalizer<AdminResource> localizer)
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly SystemConfigService _systemConfigService = systemConfigService;
        private readonly IStringLocalizer<AdminResource> _localizer = localizer;

        public async Task<IActionResult> OnGetAsync()
        {
            await Task.CompletedTask;
            return Page();
        }

        //[Ncf.AreaBase.Admin.Filters.CustomerResource("admin-get-systemconfig")]
        public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        {
            var systemConfig = await _systemConfigService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Ascending);
            return Ok(new { TotalCount = systemConfig.TotalCount, PageIndex = systemConfig.PageIndex, List = systemConfig.AsEnumerable() });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] Senparc.Ncf.Core.Models.FullSystemConfig fullSystemConfig)
        {
            if (fullSystemConfig == null)
            {
                return Ok(false, _localizer["SystemConfig.RequestEmpty"].Value);
            }

            if (string.IsNullOrWhiteSpace(fullSystemConfig.SystemName))
            {
                return Ok(false, _localizer["SystemConfig.NameEmpty"].Value);
            }

            if (string.IsNullOrWhiteSpace(fullSystemConfig.FooterContent))
            {
                return Ok(false, "Footer 内容不能为空。");
            }

            if (fullSystemConfig.FooterContent.Length > 2000)
            {
                return Ok(false, "Footer 内容不能超过 2000 个字符。");
            }

            var systemConfig = await _systemConfigService.GetObjectAsync(z => true);
            if (systemConfig == null)
            {
                return Ok(false, _localizer["SystemConfig.ConfigNotFound"].Value);
            }

            systemConfig.Update(fullSystemConfig.SystemName,
                systemConfig.MchId,
                systemConfig.MchKey,
                systemConfig.TenPayAppId,
                systemConfig.HideModuleManager);
            systemConfig.UpdateFooterContent(FooterContentSanitizer.Sanitize(fullSystemConfig.FooterContent));

            await _systemConfigService.SaveObjectAsync(systemConfig);

            base.SetMessager(MessageType.success, _localizer["SystemConfig.Updated"].Value);
            return Ok(new
            {
                systemName = systemConfig.SystemName,
                footerContent = systemConfig.FooterContent
            });
        }
    }
}
