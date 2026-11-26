/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorAuthorization.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

namespace Senparc.Xncf.Connectors.Abstractions;

/// <summary>
/// Provides the authorization predicate shared by connector invocation surfaces.
/// </summary>
public static class ConnectorAuthorization
{
    /// <summary>
    /// Determines whether a connector instance is enabled and permits a requested action.
    /// </summary>
    /// <param name="bindings">The configured instance bindings to inspect.</param>
    /// <param name="instanceId">The connector instance identifier being requested.</param>
    /// <param name="actionKey">The action identifier being requested.</param>
    /// <param name="limitToSelection">
    /// Whether the request must also be limited to <paramref name="selectedInstanceIds"/>.
    /// </param>
    /// <param name="selectedInstanceIds">The instance identifiers selected by the caller.</param>
    /// <returns>
    /// <see langword="true"/> when the request has a valid selected instance, an enabled binding,
    /// and either no action restriction or a matching action key; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Allows(
        IEnumerable<AgentConnectorBinding>? bindings,
        string? instanceId,
        string? actionKey,
        bool limitToSelection,
        IEnumerable<string>? selectedInstanceIds)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(actionKey))
        {
            return false;
        }

        if (limitToSelection)
        {
            var selected = new HashSet<string>(
                selectedInstanceIds ?? [],
                StringComparer.OrdinalIgnoreCase);
            if (!selected.Contains(instanceId))
            {
                return false;
            }
        }

        var binding = bindings?.FirstOrDefault(item =>
            item != null
            && item.Enabled
            && string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        if (binding == null)
        {
            return false;
        }

        if (binding.ActionKeys == null || binding.ActionKeys.Count == 0)
        {
            return true;
        }

        return binding.ActionKeys.Any(key =>
            string.Equals(key, actionKey, StringComparison.OrdinalIgnoreCase));
    }
}
