/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: connector admin page model

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.1.1.4 Extend MCP connector management and localization coverage.

----------------------------------------------------------------*/

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Service;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.Connectors.Domain.Services;
using Senparc.Xncf.MCP.Abstractions;

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
    private readonly IMcpEndpointGateway? _mcpGateway;

    /// <summary>
    /// Initializes the connector administration page.
    /// </summary>
    /// <param name="xncfModuleService">The lazy XNCF module service used by the base page.</param>
    /// <param name="gateway">The connector gateway used for catalog and instance operations.</param>
    public Index(
        Lazy<XncfModuleService> xncfModuleService,
        ConnectorGateway gateway,
        IServiceProvider serviceProvider)
        : base(xncfModuleService)
    {
        _gateway = gateway;
        _mcpGateway = serviceProvider.GetService<IMcpEndpointGateway>();
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
        var state = result.Success
            ? await BuildStateAsync(HttpContext.RequestAborted).ConfigureAwait(false)
            : null;
        return new JsonResult(new { success = result.Success, message = result.Message, instanceId = result.InstanceId, state });
    }

    /// <summary>
    /// Returns the latest connector catalog and configured instance list for in-page refreshes.
    /// </summary>
    public async Task<IActionResult> OnGetStateAsync()
    {
        return new JsonResult(await BuildStateAsync(HttpContext.RequestAborted).ConfigureAwait(false));
    }

    /// <summary>
    /// Deletes the connector instance identified by the request.
    /// </summary>
    /// <param name="input">The connector instance identifier to delete.</param>
    /// <returns>A JSON result containing operation status and message.</returns>
    public async Task<IActionResult> OnPostDeleteAsync([FromBody] ConnectorIdInput input)
    {
        var result = await _gateway.DeleteAsync(input?.InstanceId ?? string.Empty, HttpContext.RequestAborted).ConfigureAwait(false);
        var state = result.Success
            ? await BuildStateAsync(HttpContext.RequestAborted).ConfigureAwait(false)
            : null;
        return new JsonResult(new { success = result.Success, message = result.Message, state });
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
    /// Creates a secure MCP endpoint, discovers its tools, and exposes the tools as a Connector.
    /// </summary>
    public async Task<IActionResult> OnPostQuickMcpAsync([FromBody] ConnectorQuickMcpInput input)
    {
        if (input == null
            || string.IsNullOrWhiteSpace(input.TemplateKey)
            || string.IsNullOrWhiteSpace(input.McpName)
            || string.IsNullOrWhiteSpace(input.Endpoint))
        {
            return new JsonResult(new
            {
                success = false,
                message = ConnectorsResource.Get(
                    "Frontend.Connectors.McpFieldsRequired",
                    "Enter the MCP connection name and endpoint.")
            });
        }

        if (_mcpGateway == null)
        {
            return new JsonResult(new
            {
                success = false,
                message = ConnectorsResource.Get(
                    "Error.McpModuleNotInstalled",
                    "Enable the MCP module before creating an MCP-backed connector.")
            });
        }

        var template = ConnectorTemplateCatalog.All.FirstOrDefault(item =>
            string.Equals(item.Key, input.TemplateKey, StringComparison.OrdinalIgnoreCase));
        if (template == null)
        {
            return new JsonResult(new
            {
                success = false,
                message = ConnectorsResource.Get("Error.TemplateNotFound", "The connector template was not found.")
            });
        }

        var endpointResult = await _mcpGateway.SaveEndpointAsync(new McpEndpointSaveRequest
        {
            Name = input.McpName.Trim(),
            Endpoint = input.Endpoint.Trim(),
            EndpointType = string.IsNullOrWhiteSpace(input.EndpointType)
                ? template.EndpointType
                : input.EndpointType.Trim(),
            Description = input.Description,
            Enabled = true,
            AuthConfig = string.IsNullOrWhiteSpace(input.Token)
                ? null
                : JsonSerializer.Serialize(new { token = input.Token.Trim() })
        }, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!endpointResult.Success)
        {
            return new JsonResult(new { success = false, message = endpointResult.Message });
        }

        var test = await _mcpGateway.TestEndpointAsync(
            endpointResult.EndpointId,
            HttpContext.RequestAborted).ConfigureAwait(false);
        if (!test.Success)
        {
            return new JsonResult(new
            {
                success = false,
                message = ConnectorsResource.Format(
                    "Error.McpCreatedButTestFailed",
                    "The MCP connection was saved, but tool discovery failed: {0}",
                    test.Message)
            });
        }

        var connectorResult = await _gateway.SaveAsync(new ConnectorSaveInput
        {
            Name = string.IsNullOrWhiteSpace(input.ConnectorName) ? input.McpName.Trim() : input.ConnectorName.Trim(),
            ConnectorKey = "mcp.endpoint." + endpointResult.EndpointId,
            Description = input.Description,
            Enabled = true,
            ActionKeys = test.Tools.Select(tool => tool.Name).ToList()
        }, HttpContext.RequestAborted).ConfigureAwait(false);

        return new JsonResult(new
        {
            success = connectorResult.Success,
            message = connectorResult.Message,
            instanceId = connectorResult.InstanceId,
            endpointId = endpointResult.EndpointId
        });
    }

    /// <summary>
    /// Serializes the connector catalog and configured instances for the initial page state.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel catalog or instance loading.</param>
    /// <returns>A JSON string safe for embedding in the page model.</returns>
    private async Task<string> BuildStateJsonAsync(CancellationToken cancellationToken)
        => JsonSerializer.Serialize(await BuildStateAsync(cancellationToken).ConfigureAwait(false), JsonOptions);

    private async Task<ConnectorAdminState> BuildStateAsync(CancellationToken cancellationToken)
    {
        var catalog = await _gateway.ListCatalogAsync(cancellationToken).ConfigureAwait(false);
        var instances = await _gateway.ListInstancesAsync(cancellationToken).ConfigureAwait(false);
        return new ConnectorAdminState
        {
            Catalog = catalog,
            Instances = instances,
            Templates = _mcpGateway == null ? Array.Empty<ConnectorTemplate>() : ConnectorTemplateCatalog.All,
            McpAvailable = _mcpGateway != null
        };
    }
}

public sealed class ConnectorAdminState
{
    public IReadOnlyList<ConnectorDescriptor> Catalog { get; init; } = [];
    public IReadOnlyList<ConnectorInstanceView> Instances { get; init; } = [];
    public IReadOnlyList<ConnectorTemplate> Templates { get; init; } = [];
    public bool McpAvailable { get; init; }
}

public sealed class ConnectorIdInput
{
    public string InstanceId { get; set; } = string.Empty;
}

public sealed class ConnectorQuickMcpInput
{
    public string TemplateKey { get; set; } = string.Empty;
    public string McpName { get; set; } = string.Empty;
    public string ConnectorName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string EndpointType { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? Description { get; set; }
}
