/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: DatabaseUpgradeCommandLineOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.35.0 Updated module behavior and related support.

----------------------------------------------------------------*/

namespace Senparc.Web.Infrastructure.Database;

public sealed record DatabaseUpgradeCommandLineOptions(bool Enabled, string[] HostArguments)
{
    public const string SwitchName = "--database-upgrade";

    public static DatabaseUpgradeCommandLineOptions Parse(IEnumerable<string> arguments)
    {
        var enabled = false;
        var hostArguments = new List<string>();

        foreach (var argument in arguments ?? Array.Empty<string>())
        {
            if (string.Equals(argument, SwitchName, StringComparison.OrdinalIgnoreCase))
            {
                enabled = true;
                continue;
            }

            hostArguments.Add(argument);
        }

        return new DatabaseUpgradeCommandLineOptions(enabled, hostArguments.ToArray());
    }
}
