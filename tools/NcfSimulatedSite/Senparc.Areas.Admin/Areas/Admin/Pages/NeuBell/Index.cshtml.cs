/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260914
    Change description: v0.7.1 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

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
    NeuBellWebHookLogService webHookLogService,
    IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
{
    private readonly NeuBellWebHookService _webHookService = webHookService;
    private readonly NeuBellWebHookDispatcher _webHookDispatcher = webHookDispatcher;
    private readonly NeuBellProviderCatalog _providerCatalog = providerCatalog;
    private readonly NeuBellWebHookLogService _webHookLogService = webHookLogService;
    private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public IActionResult OnGet()
    {
        return Page();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<IActionResult> OnGetListAsync()
    {
        var list = await _webHookService.GetFullListAsync(
            z => true, z => z.Id, OrderingType.Ascending).ConfigureAwait(false);
        return Ok(list.Select(ToDto).ToList());
    }

    /// <summary>
    ///Legacy comment translated to English.
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
    /// addorupdate WebHook settings
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
                request.IsEnabled,
                request.HttpMethod,
                request.BodyTemplate).ConfigureAwait(false);
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
            adminUserId,
            request.HttpMethod,
            request.BodyTemplate).ConfigureAwait(false);
        if (!addSuccess)
        {
            return Ok(false, addMessage);
        }
        return Ok(ToDto(added));
    }

    /// <summary>
    /// delete WebHook settings
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
    ///Legacy comment translated to English.
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

        var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var (success, message) = await _webHookDispatcher.SendTestAsync(
            webHook, adminUserId, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(new { success, message });
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<IActionResult> OnGetLogListAsync(int take = 200)
    {
        take = Math.Clamp(take <= 0 ? 200 : take, 1, 500);
        var list = await _webHookLogService.GetFullListAsync(
            z => true, z => z.AddTime, OrderingType.Descending).ConfigureAwait(false);
        return Ok(list
            .Take(take)
            .Select(ToLogDto)
            .ToList());
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<IActionResult> OnGetLogPayloadAsync(int id)
    {
        if (id <= 0)
        {
            return Ok(false, "日志 Id 无效");
        }
        var log = await _webHookLogService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);
        if (log == null)
        {
            return Ok(false, "日志不存在");
        }
        return Ok(new { id = log.Id, payload = log.Payload });
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteLogAsync([FromBody] WebHookIdRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return Ok(false, "日志 Id 无效");
        }
        await _webHookLogService.DeleteObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        return Ok(true);
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<IActionResult> OnPostClearLogsAsync([FromBody] WebHookKeepRequest request)
    {
        var keep = request == null || request.Keep < 0 ? 0 : Math.Min(request.Keep, 500);
        var list = await _webHookLogService.GetFullListAsync(
            z => true, z => z.AddTime, OrderingType.Descending).ConfigureAwait(false);
        var removed = 0;
        foreach (var log in list.Skip(keep))
        {
            await _webHookLogService.DeleteObjectAsync(z => z.Id == log.Id).ConfigureAwait(false);
            removed++;
        }
        return Ok(new { removed });
    }

    private static object ToDto(NeuBellWebHook item)
    {
        return new
        {
            item.Id,
            item.Name,
            item.WebHookUrl,
            item.HttpMethod,
            item.BodyTemplate,
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

    private static object ToLogDto(NeuBellWebHookLog item)
    {
        return new
        {
            item.Id,
            item.EventKind,
            item.HttpMethod,
            item.WebHookUrl,
            item.ProviderId,
            item.Title,
            item.Status,
            item.Result,
            item.StatusCode,
            item.ElapsedMilliseconds,
            item.AdminUserId,
            item.AddTime,
            item.FinishTime
        };
    }

    public sealed class WebHookSaveRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WebHookUrl { get; set; }
        public string HttpMethod { get; set; }
        public string BodyTemplate { get; set; }
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

    public sealed class WebHookKeepRequest
    {
        public int Keep { get; set; }
    }
}
