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

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// Parses the compact additional-port mapping syntax used by Sandbox requests.
/// Each entry may be a container port, a host/container pair, and may begin with
/// <c>*</c> to request external exposure.
/// </summary>
public static class SandboxPortMappings
{
    /// <summary>
    /// Parses all supplied port entries and rejects malformed, duplicate, or over-limit mappings.
    /// </summary>
    /// <param name="input">A comma- or semicolon-separated mapping string.</param>
    /// <param name="maxMappings">The maximum number of mappings; non-positive values use the default limit of eight.</param>
    /// <returns>The normalized port mappings in input order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an entry is invalid, duplicated, or exceeds the limit.</exception>
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

    /// <summary>
    /// Parses one host/container port mapping entry and records duplicate host ports.
    /// </summary>
    /// <param name="entry">The textual mapping entry.</param>
    /// <param name="seenHostPorts">The host-port set used for duplicate detection.</param>
    /// <returns>The parsed port mapping.</returns>
    /// <exception cref="InvalidOperationException">Thrown when syntax, ports, or duplication are invalid.</exception>
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

    /// <summary>
    /// Parses and validates a TCP port number.
    /// </summary>
    /// <param name="value">The port text.</param>
    /// <param name="entry">The original mapping entry for diagnostics.</param>
    /// <returns>A port between 1 and 65535.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the port is invalid.</exception>
    private static int RequirePort(string value, string entry)
    {
        if (!int.TryParse(value, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"端口无效（应为 1-65535）：{entry}");
        }

        return port;
    }
}
