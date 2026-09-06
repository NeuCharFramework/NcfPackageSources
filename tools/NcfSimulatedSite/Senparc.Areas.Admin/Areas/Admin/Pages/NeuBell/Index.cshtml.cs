/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：NeuBell WebHook（WebAPI）设置页：
    通知端点 CRUD、测试发送、Provider 目录

    创建标识：Senparc - 20260906

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using System;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuBell;

[IgnoreAuth]
[AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
public class IndexModel(
    IServiceProvider serviceProvider,
    NeuBellWebHookService webHookService,
    NeuBellWebHookDispatcher webHookDispatcher,
    NeuBellProviderCatalog providerCatalog,
    IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
{
    private readonly NeuBellWebHookService _webHookService = webHookService;
    private readonly NeuBellWebHookDispatcher _webHookDispatcher = webHookDispatcher;
    private readonly NeuBellProviderCatalog _providerCatalog = providerCatalog;
    private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

    /// <summary>
    /// 默认 GET：渲染 WebHook 设置页（JSON 数据由 handler=List/Providers 提供）
    /// </summary>
    public IActionResult OnGet()
    {
        return Page();
    }

    /// <summary>
    /// WebHook 设置列表
    /// </summary>
    public async Task<IActionResult> OnGetListAsync()
    {
        var list = await _webHookService.GetFullListAsync(
            z => true, z => z.Id, OrderingType.Ascending).ConfigureAwait(false);
        return Ok(list.Select(ToDto).ToList());
    }

    /// <summary>
    /// 当前可用的纽铃 Provider 目录（ProviderFilter 下拉选项）
    /// </summary>
    public async Task<IActionResult> OnGetProvidersAsync()
    {
        var providers = await _providerCatalog.GetAvailableProvidersAsync(HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return Ok(providers.Select(provider => new
        {
            provider.ProviderId,
            provider.ModuleUid,
            displayName = string.IsNullOrWhiteSpace(provider.ModuleUid)
                ? provider.ProviderId
                : provider.ModuleUid
        }).ToList());
    }

    /// <summary>
    /// 新增或更新 WebHook 设置
    /// </summary>
    public async Task<IActionResult> OnPostSaveAsync([FromBody] WebHookSaveRequest request)
    {
        if (request == null)
        {
            return Ok(false, "请求无效");
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Ok(false, "名称不能为空");
        }
        if (string.IsNullOrWhiteSpace(request.WebHookUrl))
        {
            return Ok(false, "WebHook 地址不能为空");
        }

        if (request.Id > 0)
        {
            var (success, message, item) = await _webHookService.UpdateAsync(
                request.Id,
                request.Name,
                request.WebHookUrl,
                request.ProviderFilter,
                request.Secret,
                request.NotifyOnAdd,
                request.NotifyOnRemove,
                request.IsEnabled).ConfigureAwait(false);
            if (!success)
            {
                return Ok(false, message);
            }
            return Ok(ToDto(item));
        }

        var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var (addSuccess, addMessage, added) = await _webHookService.AddAsync(
            request.Name,
            request.WebHookUrl,
            request.ProviderFilter,
            request.Secret,
            request.NotifyOnAdd,
            request.NotifyOnRemove,
            adminUserId).ConfigureAwait(false);
        if (!addSuccess)
        {
            return Ok(false, addMessage);
        }
        return Ok(ToDto(added));
    }

    /// <summary>
    /// 删除 WebHook 设置
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync([FromBody] WebHookIdRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return Ok(false, "WebHook Id 无效");
        }
        await _webHookService.DeleteObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        return Ok(true);
    }

    /// <summary>
    /// 测试发送：向指定 WebHook 发送一条 test 事件
    /// </summary>
    public async Task<IActionResult> OnPostTestAsync([FromBody] WebHookIdRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return Ok(false, "WebHook Id 无效");
        }

        var webHook = await _webHookService.GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        if (webHook == null)
        {
            return Ok(false, "WebHook 不存在");
        }

        var (success, message) = await _webHookDispatcher.SendTestAsync(webHook, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return Ok(new { success, message });
    }

    private static object ToDto(NeuBellWebHook item)
    {
        return new
        {
            item.Id,
            item.Name,
            item.WebHookUrl,
            item.ProviderFilter,
            hasSecret = !string.IsNullOrWhiteSpace(item.Secret),
            item.NotifyOnAdd,
            item.NotifyOnRemove,
            item.IsEnabled,
            item.AdminUserId,
            item.AddTime,
            item.LastUpdateTime
        };
    }

    public sealed class WebHookSaveRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WebHookUrl { get; set; }
        public string ProviderFilter { get; set; }
        public string Secret { get; set; }
        public bool NotifyOnAdd { get; set; } = true;
        public bool NotifyOnRemove { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
    }

    public sealed class WebHookIdRequest
    {
        public int Id { get; set; }
    }
}
