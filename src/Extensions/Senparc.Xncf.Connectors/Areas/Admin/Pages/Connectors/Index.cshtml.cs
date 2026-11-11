/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：连接器后台管理页模型
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 提供连接器后台管理页

----------------------------------------------------------------*/

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Service;
using Senparc.Xncf.Connectors.Domain.Services;

namespace Senparc.Xncf.Connectors.Areas.Admin.Pages.Connectors;

[AdminAuthorize("AdminSuperAdmin")]
public sealed class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Default
    };

    private readonly ConnectorGateway _gateway;

    public Index(Lazy<XncfModuleService> xncfModuleService, ConnectorGateway gateway)
        : base(xncfModuleService)
    {
        _gateway = gateway;
    }

    public string InitialStateJson { get; private set; } = "{}";

    public async Task OnGetAsync()
    {
        _ = XncfRegister;
        InitialStateJson = await BuildStateJsonAsync(HttpContext.RequestAborted).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetEnabledAsync()
    {
        var configured = await _gateway.ListEnabledAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        Console.WriteLine($"[Connector] enabled options={configured.Count}");
        return new JsonResult(configured.Select(item => new
        {
            instanceId = item.InstanceId,
            name = item.Name,
            connectorName = item.ConnectorName,
            description = item.Description
        }));
    }

    public async Task<IActionResult> OnPostSaveAsync([FromBody] ConnectorSaveInput input)
    {
        var result = await _gateway.SaveAsync(input, HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(new { success = result.Success, message = result.Message, instanceId = result.InstanceId });
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] ConnectorIdInput input)
    {
        var result = await _gateway.DeleteAsync(input?.InstanceId ?? string.Empty, HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(new { success = result.Success, message = result.Message });
    }

    public async Task<IActionResult> OnPostTestAsync([FromBody] ConnectorIdInput input)
    {
        var result = await _gateway.TestAsync(input?.InstanceId ?? string.Empty, HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(new { success = result.Success, message = result.Message });
    }

    private async Task<string> BuildStateJsonAsync(CancellationToken cancellationToken)
    {
        var catalog = await _gateway.ListCatalogAsync(cancellationToken).ConfigureAwait(false);
        var instances = await _gateway.ListInstancesAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { catalog, instances }, JsonOptions);
    }
}

public sealed class ConnectorIdInput
{
    public string InstanceId { get; set; } = string.Empty;
}
