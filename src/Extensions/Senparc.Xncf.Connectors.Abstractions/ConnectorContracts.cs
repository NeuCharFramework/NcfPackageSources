/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

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
    /// <summary>
    /// Creates a successful invocation result containing the connector output.
    /// </summary>
    /// <param name="output">The output returned by the connector.</param>
    /// <returns>A successful result with an empty error value.</returns>
    public static ConnectorInvocationResult Ok(string output) => new(true, output);

    /// <summary>
    /// Creates a failed invocation result containing the connector error.
    /// </summary>
    /// <param name="error">The error description to expose to the caller.</param>
    /// <returns>A failed result with an empty output value.</returns>
    public static ConnectorInvocationResult Fail(string error) => new(false, string.Empty, error);
}

public sealed record ConnectorTestResult(bool Success, string Message);

/// <summary>
///Legacy comment translated to English.
/// </summary>
public interface IConnectorProvider
{
    /// <summary>
    /// Describes the connector fields and actions exposed by this provider.
    /// </summary>
    /// <returns>The static connector descriptor used by configuration and invocation surfaces.</returns>
    ConnectorDescriptor Describe();

    /// <summary>
    /// Invokes one connector action using the supplied instance context and arguments.
    /// </summary>
    /// <param name="context">The instance, action, configuration, and invocation arguments.</param>
    /// <param name="cancellationToken">The token used to cancel outbound or long-running work.</param>
    /// <returns>A result containing connector output or a failure message.</returns>
    Task<ConnectorInvocationResult> InvokeAsync(
        ConnectorInvocationContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies whether a connector configuration can be used before an action is invoked.
    /// </summary>
    /// <param name="configuration">The configured field values to validate or test.</param>
    /// <param name="cancellationToken">The token used to cancel the test operation.</param>
    /// <returns>A result containing the test status and an explanatory message.</returns>
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
    /// <summary>
    /// Lists connector types registered in the current application.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel catalog discovery.</param>
    /// <returns>The available connector descriptors.</returns>
    Task<IReadOnlyList<ConnectorDescriptor>> ListCatalogAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lists enabled connector instances available to the current caller.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel instance discovery.</param>
    /// <returns>The enabled configured connector instances.</returns>
    Task<IReadOnlyList<ConfiguredConnector>> ListEnabledAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Invokes an action on one enabled connector instance.
    /// </summary>
    /// <param name="instanceId">The configured connector instance identifier.</param>
    /// <param name="actionKey">The action identifier declared by the connector.</param>
    /// <param name="arguments">The action arguments keyed by connector parameter name.</param>
    /// <param name="cancellationToken">The token used to cancel invocation.</param>
    /// <returns>A result containing connector output or a failure message.</returns>
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
