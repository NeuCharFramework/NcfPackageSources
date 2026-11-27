/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.1.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Connectors.Abstractions;

/// <summary>
/// Defines the provider contract used to describe, test, and invoke a connector implementation.
/// Stores the connector binding rules used to restrict an agent or chat to selected instances
/// and optionally selected action keys.
/// </summary>
public sealed class ConnectorDescriptor
{
    /// <summary>
    /// Gets the stable provider key used to select this connector implementation.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the human-readable connector name shown in configuration and selection views.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the connector's purpose and supported integration boundary.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the optional icon identifier used by the connector UI.
    /// </summary>
    public string Icon { get; init; } = "fa fa-plug";

    /// <summary>
    /// Gets the configuration fields required to create or validate a connector instance.
    /// </summary>
    public IReadOnlyList<ConnectorFieldDescriptor> Fields { get; init; } = [];

    /// <summary>
    /// Gets the actions that can be invoked after a connector instance is configured.
    /// </summary>
    public IReadOnlyList<ConnectorActionDescriptor> Actions { get; init; } = [];
}

/// <summary>
/// Identifies the editor control and value-handling rules for a connector configuration field.
/// </summary>
public enum ConnectorFieldKind
{
    Text = 0,
    Secret = 1,
    Select = 2,
    TextArea = 3
}

public sealed class ConnectorFieldDescriptor
{
    /// <summary>
    /// Gets the stable field key used in configuration dictionaries.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the field label shown to administrators.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the field-specific explanation shown beside the editor.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the control kind used to edit this field.
    /// </summary>
    public ConnectorFieldKind Kind { get; init; } = ConnectorFieldKind.Text;

    /// <summary>
    /// Gets a value indicating whether the field must contain a value before testing or invoking.
    /// </summary>
    public bool Required { get; init; }

    /// <summary>
    /// Gets the placeholder text displayed when the field has no value.
    /// </summary>
    public string Placeholder { get; init; } = string.Empty;

    /// <summary>
    /// Gets the selectable values used when <see cref="Kind"/> is <see cref="ConnectorFieldKind.Select"/>.
    /// </summary>
    public IReadOnlyList<ConnectorOption> Options { get; init; } = [];
}

public sealed class ConnectorOption
{
    /// <summary>
    /// Gets the machine-readable value submitted for this option.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the human-readable option label.
    /// </summary>
    public required string Label { get; init; }
}

public sealed class ConnectorActionDescriptor
{
    /// <summary>
    /// Gets the stable action key used when an invocation is requested.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the action name shown to users.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the action description and its intended effect.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the arguments that may or must be supplied to the action.
    /// </summary>
    public IReadOnlyList<ConnectorParameterDescriptor> Parameters { get; init; } = [];
}

public sealed class ConnectorParameterDescriptor
{
    /// <summary>
    /// Gets the stable parameter key used in invocation argument dictionaries.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the parameter label shown to users.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the parameter explanation used by configuration or invocation UIs.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the action requires this parameter.
    /// </summary>
    public bool Required { get; init; }
}

public sealed class ConnectorInvocationContext
{
    /// <summary>
    /// Gets the configured connector instance identifier.
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// Gets the configured connector instance name.
    /// </summary>
    public required string InstanceName { get; init; }

    /// <summary>
    /// Gets the action key requested for this invocation.
    /// </summary>
    public required string ActionKey { get; init; }

    /// <summary>
    /// Gets the connector configuration values associated with the instance.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Configuration { get; init; }

    /// <summary>
    /// Gets the action arguments supplied for this invocation.
    /// </summary>
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

/// <summary>
/// Represents the result of validating a connector configuration without invoking an action.
/// </summary>
/// <param name="Success">Whether the configuration passed the provider's validation.</param>
/// <param name="Message">The human-readable validation result.</param>
public sealed record ConnectorTestResult(bool Success, string Message);

/// <summary>
/// Defines the provider contract used to describe, validate, and invoke one connector type.
/// Implementations own provider-specific validation and outbound integration behavior.
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
    /// <summary>
    /// Gets the stable identifier of the configured connector instance.
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// Gets the administrator-defined instance name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provider key used to resolve the connector implementation.
    /// </summary>
    public required string ConnectorKey { get; init; }

    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    public required string ConnectorName { get; init; }

    /// <summary>
    /// Gets the description of the configured connector type.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the actions exposed by this configured connector instance.
    /// </summary>
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
/// Binds a connector instance to an agent, chat, or other host capability.
/// An empty action-key list allows the host to use the connector's complete action set.
/// </summary>
public sealed class AgentConnectorBinding
{
    /// <summary>
    /// Gets or sets the configured connector instance identifier.
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the binding may be used by the host.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the action keys allowed through this binding.
    /// </summary>
    public List<string> ActionKeys { get; set; } = [];
}
