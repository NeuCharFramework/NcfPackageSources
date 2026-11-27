/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorGateway.cs
    File description: invoke configured connector actions by catalog and authorization

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.1.1.4 Extend MCP connector management and localization coverage.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.Connectors;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.Connectors.Domain.Models.DatabaseModel;
using Senparc.Xncf.MCP.Abstractions;

namespace Senparc.Xncf.Connectors.Domain.Services;

/// <summary>
/// Validates, persists, tests, and invokes configured connector instances.
/// Secret fields are protected before persistence and unprotected only at provider boundaries.
/// </summary>
public sealed class ConnectorGateway : IConnectorGateway
{
    private readonly ConnectorsSenparcEntities _db;
    private readonly IReadOnlyDictionary<string, IConnectorProvider> _providers;
    private readonly ConnectorSecretProtector _secrets;
    private readonly ILogger<ConnectorGateway> _logger;
    private readonly IMcpEndpointGateway? _mcpGateway;
    private const string McpKeyPrefix = "mcp.endpoint.";

    /// <summary>
    /// Initializes the connector gateway.
    /// </summary>
    /// <param name="db">Connector database context.</param>
    /// <param name="providers">Connector providers available to the module.</param>
    /// <param name="secrets">Secret protection service.</param>
    /// <param name="logger">Logger for connector operations.</param>
    public ConnectorGateway(
        ConnectorsSenparcEntities db,
        IEnumerable<IConnectorProvider> providers,
        ConnectorSecretProtector secrets,
        ILogger<ConnectorGateway> logger,
        IServiceProvider serviceProvider)
    {
        _db = db;
        _secrets = secrets;
        _logger = logger;
        _mcpGateway = serviceProvider.GetService<IMcpEndpointGateway>();
        _providers = providers
            .GroupBy(item => item.Describe().Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConnectorDescriptor>> ListCatalogAsync(CancellationToken cancellationToken)
    {
        var catalogItems = _providers.Values.Select(item => item.Describe()).ToList();
        if (_mcpGateway != null)
        {
            var endpoints = await _mcpGateway.ListEndpointsAsync(onlyEnabled: true, cancellationToken)
                .ConfigureAwait(false);
            catalogItems.AddRange(endpoints.Select(ToMcpDescriptor));
        }
        IReadOnlyList<ConnectorDescriptor> catalog = catalogItems
            .OrderBy(item => item.Name, StringComparer.CurrentCulture)
            .ToArray();
        _logger.LogInformation("[Connector] catalog count={Count}", catalog.Count);
        return catalog;
    }

    /// <inheritdoc />
    /// <summary>
    /// Lists enabled connector instances with only their permitted action metadata.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>Enabled configured connectors whose provider types are installed.</returns>
    public async Task<IReadOnlyList<ConfiguredConnector>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        var instances = await _db.ConnectorInstances
            .AsNoTracking()
            .Where(item => item.Enabled)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<ConfiguredConnector>();
        foreach (var instance in instances)
        {
            var configured = await ToConfiguredAsync(instance, cancellationToken).ConfigureAwait(false);
            if (configured != null)
            {
                result.Add(configured);
            }
        }
        return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Lists all connector instances for administration without exposing secret values.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>Administrative connector views with secret-field names but no secret contents.</returns>
    public async Task<IReadOnlyList<ConnectorInstanceView>> ListInstancesAsync(CancellationToken cancellationToken)
    {
        var instances = await _db.ConnectorInstances
            .AsNoTracking()
            .OrderByDescending(item => item.AddTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<ConnectorInstanceView>();
        foreach (var instance in instances)
        {
            result.Add(await ToViewAsync(instance, cancellationToken).ConfigureAwait(false));
        }
        return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Validates and persists a connector configuration, protecting secret fields before storage.
    /// </summary>
    /// <param name="input">The connector name, type, field values, actions, and enabled state.</param>
    /// <param name="cancellationToken">Token used to cancel validation or persistence.</param>
    /// <returns>Operation status, localized message, and the saved public instance identifier.</returns>
    public async Task<(bool Success, string Message, string? InstanceId)> SaveAsync(
        ConnectorSaveInput input,
        CancellationToken cancellationToken)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Name))
        {
            return (false, ConnectorsResource.Get("Error.NameRequired", "Please enter a connector name."), null);
        }

        if (string.IsNullOrWhiteSpace(input.ConnectorKey))
        {
            return (false, ConnectorsResource.Get("Error.TypeNotFound", "No connector type was found. Confirm that the corresponding module is installed."), null);
        }

        _providers.TryGetValue(input.ConnectorKey.Trim(), out var provider);
        var descriptor = await FindDescriptorAsync(input.ConnectorKey.Trim(), cancellationToken).ConfigureAwait(false);
        if (descriptor == null)
        {
            return (false, ConnectorsResource.Get("Error.TypeNotFound", "No connector type was found. Confirm that the corresponding module is installed."), null);
        }
        var existing = string.IsNullOrWhiteSpace(input.InstanceId)
            ? null
            : await _db.ConnectorInstances.FirstOrDefaultAsync(
                item => item.PublicId == input.InstanceId,
                cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(input.InstanceId) && existing == null)
        {
            return (false, ConnectorsResource.Get("Error.InstanceNotFound", "The connector to update was not found."), null);
        }

        if (existing != null && !string.Equals(existing.ConnectorKey, descriptor.Key, StringComparison.OrdinalIgnoreCase))
        {
            return (false, ConnectorsResource.Get("Error.TypeChangeForbidden", "A saved connector cannot be changed to another type. Create a new connector instead."), null);
        }

        var previous = ConnectorConfigurationCodec.Read(existing?.ConfigJson);
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in descriptor.Fields)
        {
            input.Values.TryGetValue(field.Key, out var incoming);
            incoming = incoming?.Trim() ?? string.Empty;
            if (field.Kind == ConnectorFieldKind.Secret)
            {
                if (string.IsNullOrEmpty(incoming))
                {
                    if (previous.TryGetValue(field.Key, out var stored) && !string.IsNullOrEmpty(stored))
                    {
                        merged[field.Key] = stored;
                    }
                    else if (field.Required)
                    {
                        return (false, ConnectorsResource.Format("Error.FieldRequired", "Please fill in {0}.", field.Label), null);
                    }
                }
                else
                {
                    merged[field.Key] = _secrets.Protect(incoming);
                }

                continue;
            }

            if (string.IsNullOrEmpty(incoming))
            {
                if (field.Required)
                {
                    return (false, ConnectorsResource.Format("Error.FieldRequired", "Please fill in {0}.", field.Label), null);
                }

                continue;
            }

            if (field.Kind == ConnectorFieldKind.Select
                && field.Options.Count > 0
                && field.Options.All(option => !string.Equals(option.Value, incoming, StringComparison.Ordinal)))
            {
                return (false, ConnectorsResource.Format("Error.OptionInvalid", "The selected option for {0} is invalid.", field.Label), null);
            }

            merged[field.Key] = incoming;
        }

        var knownActions = descriptor.Actions.Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var enabledActions = (input.ActionKeys ?? [])
            .Where(knownActions.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (enabledActions.Length == 0)
        {
            return (false, ConnectorsResource.Get("Error.ActionRequired", "Select at least one action that can be given to AI."), null);
        }

        var entity = existing ?? new ConnectorInstance(input.Name, descriptor.Key, input.Description);
        if (existing == null)
        {
            _db.ConnectorInstances.Add(entity);
        }
        else
        {
            entity.Rename(input.Name, input.Description);
        }

        entity.SetConfiguration(
            descriptor.Key,
            ConnectorConfigurationCodec.Write(merged),
            ConnectorConfigurationCodec.WriteList(enabledActions),
            input.Enabled);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "[Connector] saved instance={InstanceId} type={ConnectorKey} enabled={Enabled} actions={ActionCount}",
            entity.PublicId,
            entity.ConnectorKey,
            entity.Enabled,
            enabledActions.Length);
        return (true, ConnectorsResource.Get("Message.Saved", "Connector saved."), entity.PublicId);
    }

    /// <inheritdoc />
    /// <summary>
    /// Soft-deletes a configured connector instance by setting its entity flag.
    /// </summary>
    /// <param name="instanceId">The public connector instance identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>Operation status and a localized result message.</returns>
    public async Task<(bool Success, string Message)> DeleteAsync(string instanceId, CancellationToken cancellationToken)
    {
        var entity = await _db.ConnectorInstances
            .FirstOrDefaultAsync(item => item.PublicId == instanceId, cancellationToken)
            .ConfigureAwait(false);
        if (entity == null)
        {
            return (false, ConnectorsResource.Get("Error.InstanceNotFound", "The connector was not found."));
        }

        entity.Flag = true;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("[Connector] deleted instance={InstanceId}", instanceId);
        return (true, ConnectorsResource.Get("Message.Deleted", "Connector deleted."));
    }

    /// <inheritdoc />
    /// <summary>
    /// Tests one configured connector after unprotecting its secret fields at the provider boundary.
    /// </summary>
    /// <param name="instanceId">The public connector instance identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the provider test.</param>
    /// <returns>The provider test result, persisted as the instance's latest test status.</returns>
    public async Task<ConnectorTestResult> TestAsync(string instanceId, CancellationToken cancellationToken)
    {
        var entity = await _db.ConnectorInstances
            .FirstOrDefaultAsync(item => item.PublicId == instanceId, cancellationToken)
            .ConfigureAwait(false);
        if (entity == null)
        {
            return new ConnectorTestResult(false, ConnectorsResource.Get("Error.TypeNotInstalled", "The connector does not exist or its type is not installed."));
        }

        if (TryParseMcpEndpointId(entity.ConnectorKey, out var endpointId) && _mcpGateway != null)
        {
            var test = await _mcpGateway.TestEndpointAsync(endpointId, cancellationToken).ConfigureAwait(false);
            var mcpResult = new ConnectorTestResult(test.Success, test.Message);
            entity.RecordTest(mcpResult.Success, mcpResult.Message);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return mcpResult;
        }

        if (!_providers.TryGetValue(entity.ConnectorKey, out var provider))
        {
            return new ConnectorTestResult(false, ConnectorsResource.Get("Error.TypeNotInstalled", "The connector type is not installed."));
        }

        ConnectorTestResult result;
        try
        {
            result = await provider.TestAsync(Unprotect(entity), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "[Connector] test failed instance={InstanceId}", instanceId);
            result = new ConnectorTestResult(false, ConnectorsResource.Get("Error.TestFailed", "Connection test failed. Check the account and secret."));
        }

        entity.RecordTest(result.Success, result.Message);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[Connector] test instance={instanceId} success={result.Success} message={result.Message}");
        return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Invokes an enabled, authorized action on a configured connector instance.
    /// </summary>
    /// <param name="instanceId">The public connector instance identifier.</param>
    /// <param name="actionKey">The provider action key.</param>
    /// <param name="arguments">Optional action arguments; blank keys/values are ignored.</param>
    /// <param name="cancellationToken">Token used to cancel provider invocation.</param>
    /// <returns>Connector output or a localized failure result.</returns>
    public async Task<ConnectorInvocationResult> InvokeAsync(
        string instanceId,
        string actionKey,
        IReadOnlyDictionary<string, string?> arguments,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ConnectorInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PublicId == instanceId, cancellationToken)
            .ConfigureAwait(false);
        if (entity == null || !entity.Enabled)
        {
            return ConnectorInvocationResult.Fail(ConnectorsResource.Get("Error.Disabled", "The connector does not exist or is disabled."));
        }

        var providerAvailable = _providers.TryGetValue(entity.ConnectorKey, out var provider);
        var descriptor = providerAvailable
            ? provider!.Describe()
            : await FindDescriptorAsync(entity.ConnectorKey, cancellationToken).ConfigureAwait(false);
        if (descriptor == null)
        {
            return ConnectorInvocationResult.Fail(ConnectorsResource.Get("Error.TypeNotInstalled", "The connector type is not installed."));
        }

        var action = descriptor.Actions.FirstOrDefault(item =>
            string.Equals(item.Key, actionKey, StringComparison.OrdinalIgnoreCase));
        if (action == null)
        {
            return ConnectorInvocationResult.Fail(ConnectorsResource.Get("Error.ActionNotFound", "This connector does not have the requested action."));
        }

        var enabledActions = ConnectorConfigurationCodec.ReadList(entity.EnabledActionsJson);
        if (enabledActions.Count > 0
            && enabledActions.All(item => !string.Equals(item, action.Key, StringComparison.OrdinalIgnoreCase)))
        {
            return ConnectorInvocationResult.Fail(ConnectorsResource.Get("Error.ActionNotEnabled", "This action is not enabled for the connector."));
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in arguments ?? new Dictionary<string, string?>())
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                normalized[pair.Key] = pair.Value.Trim();
            }
        }
        var missingParameter = action.Parameters.FirstOrDefault(parameter =>
            parameter.Required
            && (!normalized.TryGetValue(parameter.Key, out var value) || string.IsNullOrWhiteSpace(value)));
        if (missingParameter != null)
        {
            return ConnectorInvocationResult.Fail(ConnectorsResource.Format(
                "Error.ParameterRequired",
                "The action requires {0}.",
                missingParameter.Label));
        }

        Console.WriteLine($"[Connector] invoke instance={entity.PublicId} action={action.Key} args={normalized.Count}");
        try
        {
            ConnectorInvocationResult result;
            if (TryParseMcpEndpointId(entity.ConnectorKey, out var endpointId) && _mcpGateway != null)
            {
                var mcpResult = await _mcpGateway.CallToolAsync(
                    endpointId,
                    action.Key,
                    normalized.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.OrdinalIgnoreCase),
                    cancellationToken).ConfigureAwait(false);
                result = mcpResult.Success
                    ? ConnectorInvocationResult.Ok(mcpResult.Output)
                    : ConnectorInvocationResult.Fail(mcpResult.Error ?? "MCP tool invocation failed.");
            }
            else
            {
                if (!providerAvailable || provider == null)
                {
                    return ConnectorInvocationResult.Fail(ConnectorsResource.Get("Error.TypeNotInstalled", "The connector type is not installed."));
                }

                result = await provider.InvokeAsync(new ConnectorInvocationContext
                {
                    InstanceId = entity.PublicId,
                    InstanceName = entity.Name,
                    ActionKey = action.Key,
                    Configuration = Unprotect(entity),
                    Arguments = normalized
                }, cancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation(
                "[Connector] invoke finished instance={InstanceId} action={ActionKey} success={Success}",
                entity.PublicId,
                action.Key,
                result.Success);
            return result;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "[Connector] invoke failed instance={InstanceId} action={ActionKey}", entity.PublicId, action.Key);
            return ConnectorInvocationResult.Fail(ConnectorsResource.Get("Error.InvocationFailed", "Connector invocation failed. Try again later or check account permissions."));
        }
    }

    /// <summary>
    /// Finds the catalog descriptor for a connector key.
    /// </summary>
    /// <param name="connectorKey">The case-insensitive connector key.</param>
    /// <returns>The descriptor, or <see langword="null"/> when no provider is registered.</returns>
    public ConnectorDescriptor? FindDescriptor(string connectorKey)
        => _providers.TryGetValue(connectorKey ?? string.Empty, out var provider) ? provider.Describe() : null;

    public async Task<ConnectorDescriptor?> FindDescriptorAsync(
        string connectorKey,
        CancellationToken cancellationToken)
    {
        var registered = FindDescriptor(connectorKey);
        if (registered != null || _mcpGateway == null
            || !TryParseMcpEndpointId(connectorKey, out var endpointId))
        {
            return registered;
        }

        var endpoint = await _mcpGateway.GetEndpointAsync(endpointId, cancellationToken).ConfigureAwait(false);
        return endpoint == null || !endpoint.Enabled ? null : ToMcpDescriptor(endpoint);
    }

    /// <summary>
    /// Unprotects only secret fields required by the configured provider.
    /// </summary>
    /// <param name="entity">The persisted connector entity.</param>
    /// <returns>A mutable configuration dictionary for provider-bound use.</returns>
    private Dictionary<string, string> Unprotect(ConnectorInstance entity)
    {
        var values = ConnectorConfigurationCodec.Read(entity.ConfigJson);
        if (!_providers.TryGetValue(entity.ConnectorKey, out var provider))
        {
            return values;
        }

        foreach (var field in provider.Describe().Fields.Where(item => item.Kind == ConnectorFieldKind.Secret))
        {
            if (values.TryGetValue(field.Key, out var stored) && ConnectorSecretProtector.IsProtected(stored))
            {
                values[field.Key] = _secrets.Unprotect(stored);
            }
        }

        return values;
    }

    /// <summary>
    /// Projects a persisted entity into the enabled-connector contract.
    /// </summary>
    /// <param name="entity">The persisted connector entity.</param>
    /// <returns>The configured connector, or <see langword="null"/> when its provider is unavailable.</returns>
    private async Task<ConfiguredConnector?> ToConfiguredAsync(
        ConnectorInstance entity,
        CancellationToken cancellationToken)
    {
        var descriptor = await FindDescriptorAsync(entity.ConnectorKey, cancellationToken).ConfigureAwait(false);
        if (descriptor == null)
        {
            return null;
        }

        var enabled = ConnectorConfigurationCodec.ReadList(entity.EnabledActionsJson);
        var actions = descriptor.Actions
            .Where(action => enabled.Count == 0
                || enabled.Any(item => string.Equals(item, action.Key, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        return new ConfiguredConnector
        {
            InstanceId = entity.PublicId,
            Name = entity.Name,
            ConnectorKey = descriptor.Key,
            ConnectorName = descriptor.Name,
            Description = string.IsNullOrWhiteSpace(entity.Description) ? descriptor.Description : entity.Description,
            Actions = actions
        };
    }

    /// <summary>
    /// Projects a persisted entity into an administration view with secrets redacted.
    /// </summary>
    /// <param name="entity">The persisted connector entity.</param>
    /// <returns>The redacted connector instance view.</returns>
    private async Task<ConnectorInstanceView> ToViewAsync(
        ConnectorInstance entity,
        CancellationToken cancellationToken)
    {
        var descriptor = await FindDescriptorAsync(entity.ConnectorKey, cancellationToken).ConfigureAwait(false);
        var values = ConnectorConfigurationCodec.Read(entity.ConfigJson);
        var publicValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var secretFields = new List<string>();
        foreach (var field in descriptor?.Fields ?? [])
        {
            values.TryGetValue(field.Key, out var stored);
            if (field.Kind == ConnectorFieldKind.Secret)
            {
                if (!string.IsNullOrEmpty(stored))
                {
                    secretFields.Add(field.Key);
                }

                continue;
            }

            if (!string.IsNullOrEmpty(stored))
            {
                publicValues[field.Key] = stored;
            }
        }

        return new ConnectorInstanceView
        {
            InstanceId = entity.PublicId,
            Name = entity.Name,
            ConnectorKey = entity.ConnectorKey,
            ConnectorName = descriptor?.Name ?? entity.ConnectorKey,
            Description = entity.Description ?? string.Empty,
            Enabled = entity.Enabled,
            ActionKeys = ConnectorConfigurationCodec.ReadList(entity.EnabledActionsJson),
            Values = publicValues,
            SecretFields = secretFields,
            LastTestSucceeded = entity.LastTestSucceeded,
            LastTestMessage = entity.LastTestMessage ?? string.Empty,
            LastTestedAt = entity.LastTestedAt
        };
    }

    private static ConnectorDescriptor ToMcpDescriptor(McpEndpointDescriptor endpoint) => new()
    {
        Key = McpKeyPrefix + endpoint.Id,
        Name = endpoint.Name,
        Description = endpoint.Description
            ?? "MCP endpoint with centrally managed authentication and discovered tools.",
        Icon = "fa fa-cubes",
        Fields = [],
        Actions = endpoint.Tools.Select(tool => new ConnectorActionDescriptor
        {
            Key = tool.Name,
            Name = tool.Title ?? tool.Name,
            Description = tool.Description ?? string.Empty,
            Parameters = tool.Parameters.Select(parameter => new ConnectorParameterDescriptor
            {
                Key = parameter.Name,
                Label = parameter.Name,
                Description = parameter.Description ?? string.Empty,
                Required = parameter.Required
            }).ToArray()
        }).ToArray()
    };

    private static bool TryParseMcpEndpointId(string connectorKey, out int endpointId)
    {
        endpointId = 0;
        return connectorKey != null
            && connectorKey.StartsWith(McpKeyPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(connectorKey[McpKeyPrefix.Length..], out endpointId)
            && endpointId > 0;
    }
}

public sealed class ConnectorSaveInput
{
    public string? InstanceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ConnectorKey { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> ActionKeys { get; set; } = [];
}

public sealed class ConnectorInstanceView
{
    public string InstanceId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ConnectorKey { get; set; } = string.Empty;

    public string ConnectorName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public List<string> ActionKeys { get; set; } = [];

    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> SecretFields { get; set; } = [];

    public bool? LastTestSucceeded { get; set; }

    public string LastTestMessage { get; set; } = string.Empty;

    public DateTime? LastTestedAt { get; set; }
}
