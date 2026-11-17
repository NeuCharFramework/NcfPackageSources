/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxPortMappings.cs
    File description: Related implementation.

    Created by: Senparc - 20260918
    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.4.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public static class SandboxPortMappings
{
    public static IReadOnlyList<SandboxPortMapping> Parse(string? input, int maxMappings)
    {
        if (maxMappings <= 0)
        {
            maxMappings = 8;
        }

        var result = new List<SandboxPortMapping>();
        if (string.IsNullOrWhiteSpace(input))
        {
            return result;
        }

        var seenHostPorts = new HashSet<int>();
        foreach (var rawEntry in input.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var entry = rawEntry.Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            if (result.Count >= maxMappings)
            {
                throw new InvalidOperationException($"附加端口映射最多 {maxMappings} 条。");
            }

            var mapping = ParseEntry(entry, seenHostPorts);
            result.Add(mapping);
        }

        return result;
    }

    private static SandboxPortMapping ParseEntry(string entry, HashSet<int> seenHostPorts)
    {
        var exposeExternally = entry.StartsWith("*", StringComparison.Ordinal);
        var parts = (exposeExternally ? entry[1..] : entry)
            .Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            return new SandboxPortMapping(0, RequirePort(parts[0], entry), exposeExternally);
        }

        if (parts.Length == 2)
        {
            var hostPort = RequirePort(parts[0], entry);
            if (!seenHostPorts.Add(hostPort))
            {
                throw new InvalidOperationException($"宿主端口重复：{hostPort}");
            }

            return new SandboxPortMapping(hostPort, RequirePort(parts[1], entry), exposeExternally);
        }

        throw new InvalidOperationException($"无法解析的端口映射：{entry}");
    }

    private static int RequirePort(string value, string entry)
    {
        if (!int.TryParse(value, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"端口无效（应为 1-65535）：{entry}");
        }

        return port;
    }
}
