/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Connectors.Abstractions;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class ConnectorDescriptor
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public string Icon { get; init; } = "fa fa-plug";

    public IReadOnlyList<ConnectorFieldDescriptor> Fields { get; init; } = [];

    public IReadOnlyList<ConnectorActionDescriptor> Actions { get; init; } = [];
}

public enum ConnectorFieldKind
{
    Text = 0,
    Secret = 1,
    Select = 2,
    TextArea = 3
}

public sealed class ConnectorFieldDescriptor
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public string Description { get; init; } = string.Empty;

    public ConnectorFieldKind Kind { get; init; } = ConnectorFieldKind.Text;

    public bool Required { get; init; }

    public string Placeholder { get; init; } = string.Empty;

    public IReadOnlyList<ConnectorOption> Options { get; init; } = [];
}

public sealed class ConnectorOption
{
    public required string Value { get; init; }

    public required string Label { get; init; }
}

public sealed class ConnectorActionDescriptor
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<ConnectorParameterDescriptor> Parameters { get; init; } = [];
}

public sealed class ConnectorParameterDescriptor
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool Required { get; init; }
}

public sealed class ConnectorInvocationContext
{
    public required string InstanceId { get; init; }

    public required string InstanceName { get; init; }

    public required string ActionKey { get; init; }

    public required IReadOnlyDictionary<string, string> Configuration { get; init; }

    public required IReadOnlyDictionary<string, string> Arguments { get; init; }
}

public sealed record ConnectorInvocationResult(bool Success, string Output, string? Error = null)
{
    public static ConnectorInvocationResult Ok(string output) => new(true, output);

    public static ConnectorInvocationResult Fail(string error) => new(false, string.Empty, error);
}

public sealed record ConnectorTestResult(bool Success, string Message);

/// <summary>
///Legacy comment translated to English.
/// </summary>
public interface IConnectorProvider
{
    ConnectorDescriptor Describe();

    Task<ConnectorInvocationResult> InvokeAsync(
        ConnectorInvocationContext context,
        CancellationToken cancellationToken);

    Task<ConnectorTestResult> TestAsync(
        IReadOnlyDictionary<string, string> configuration,
        CancellationToken cancellationToken);
}

public sealed class ConfiguredConnector
{
    public required string InstanceId { get; init; }

    public required string Name { get; init; }

    public required string ConnectorKey { get; init; }

    public required string ConnectorName { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<ConnectorActionDescriptor> Actions { get; init; } = [];
}

public interface IConnectorGateway
{
    Task<IReadOnlyList<ConnectorDescriptor>> ListCatalogAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ConfiguredConnector>> ListEnabledAsync(CancellationToken cancellationToken);

    Task<ConnectorInvocationResult> InvokeAsync(
        string instanceId,
        string actionKey,
        IReadOnlyDictionary<string, string?> arguments,
        CancellationToken cancellationToken);
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class AgentConnectorBinding
{
    public string InstanceId { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public List<string> ActionKeys { get; set; } = [];
}
