/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxPortMappings.cs
    文件功能描述：沙箱附加端口映射解析与校验

    创建标识：Senparc - 20260918
    修改标识：Senparc - 20260918
    修改描述：v0.3.3 新增附加端口映射解析器

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// 解析 Function/表单传入的附加端口映射字符串。
/// 支持格式（多条以 ; 或 , 分隔）：
/// containerPort —— 自动分配 loopback 宿主端口；
/// hostPort:containerPort —— 指定 loopback 宿主端口；
/// *:containerPort —— 自动分配 0.0.0.0 宿主端口（外部可访问）；
/// *:hostPort:containerPort —— 指定 0.0.0.0 宿主端口（外部可访问）。
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
