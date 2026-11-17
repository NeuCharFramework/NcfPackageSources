/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorAuthorization.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Connectors.Abstractions;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public static class ConnectorAuthorization
{
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
