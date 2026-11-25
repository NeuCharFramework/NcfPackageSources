/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: connector admin page model

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Service;
using Senparc.Xncf.Connectors.Domain.Services;

namespace Senparc.Xncf.Connectors.Areas.Admin.Pages.Connectors;

[AdminAuthorize("AdminSuperAdmin")]
/// <summary>
/// Provides the administrator page and JSON handlers for connector catalog and instance management.
/// </summary>
public sealed class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Default
    };

    private readonly ConnectorGateway _gateway;

    /// <summary>
    /// Initializes the connector administration page.
    /// </summary>
    /// <param name="xncfModuleService">The lazy XNCF module service used by the base page.</param>
    /// <param name="gateway">The connector gateway used for catalog and instance operations.</param>
    public Index(Lazy<XncfModuleService> xncfModuleService, ConnectorGateway gateway)
        : base(xncfModuleService)
    {
        _gateway = gateway;
    }

    public string InitialStateJson { get; private set; } = "{}";

    /// <summary>
    /// Loads the initial serialized connector catalog and configured instances.
    /// </summary>
    /// <returns>A task that completes after the initial state has been prepared.</returns>
    public async Task OnGetAsync()
    {
        _ = XncfRegister;
        InitialStateJson = await BuildStateJsonAsync(HttpContext.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns enabled connector instances for selection controls.
    /// </summary>
    /// <returns>A JSON result containing non-secret instance metadata.</returns>
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

    /// <summary>
    /// Saves a connector instance configuration.
    /// </summary>
    /// <param name="input">The instance name, connector key, configuration, and enabled actions.</param>
    /// <returns>A JSON result containing operation status, message, and instance identifier.</returns>
    public async Task<IActionResult> OnPostSaveAsync([FromBody] ConnectorSaveInput input)
    {
        var result = await _gateway.SaveAsync(input, HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(new { success = result.Success, message = result.Message, instanceId = result.InstanceId });
    }

    /// <summary>
    /// Deletes the connector instance identified by the request.
    /// </summary>
    /// <param name="input">The connector instance identifier to delete.</param>
    /// <returns>A JSON result containing operation status and message.</returns>
    public async Task<IActionResult> OnPostDeleteAsync([FromBody] ConnectorIdInput input)
    {
        var result = await _gateway.DeleteAsync(input?.InstanceId ?? string.Empty, HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Tests the stored configuration for one connector instance.
    /// </summary>
    /// <param name="input">The connector instance identifier to test.</param>
    /// <returns>A JSON result containing the connector test status and message.</returns>
    public async Task<IActionResult> OnPostTestAsync([FromBody] ConnectorIdInput input)
    {
        var result = await _gateway.TestAsync(input?.InstanceId ?? string.Empty, HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Serializes the connector catalog and configured instances for the initial page state.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel catalog or instance loading.</param>
    /// <returns>A JSON string safe for embedding in the page model.</returns>
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
