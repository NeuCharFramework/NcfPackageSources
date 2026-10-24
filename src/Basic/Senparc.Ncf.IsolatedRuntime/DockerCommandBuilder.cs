/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DockerCommandBuilder.cs
    文件功能描述：结构化隔离运行时请求到 Docker CLI 参数

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Globalization;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Ncf.IsolatedRuntime;

internal static class DockerCommandBuilder
{
    public static IReadOnlyList<string> BuildRunArguments(IsolatedRuntimeSpec specification)
    {
        Validate(specification);
        var arguments = new List<string> { "run" };
        if (specification.Detached)
        {
            arguments.Add("-d");
        }

        if (specification.AutoRemove)
        {
            arguments.Add("--rm");
        }

        if (specification.OpenStandardInput)
        {
            arguments.Add("-i");
        }

        AddPair(arguments, "--name", specification.Name);
        AddPair(arguments, "--cpus", Math.Max(0.05, specification.CpuLimit).ToString("0.###", CultureInfo.InvariantCulture));
        AddPair(arguments, "--memory", $"{Math.Max(16, specification.MemoryMegabytes)}m");
        AddPair(arguments, "--pids-limit", Math.Max(16, specification.PidsLimit).ToString(CultureInfo.InvariantCulture));
        AddPair(arguments, "--network", specification.NetworkMode);
        AddPair(arguments, "--workdir", specification.WorkingDirectory);
        AddPair(arguments, "--user", specification.User);

        if (specification.ReadOnlyRootFileSystem)
        {
            arguments.Add("--read-only");
        }

        if (specification.DropAllCapabilities)
        {
            arguments.Add("--cap-drop");
            arguments.Add("ALL");
        }

        if (specification.NoNewPrivileges)
        {
            arguments.Add("--security-opt");
            arguments.Add("no-new-privileges");
        }

        foreach (var label in specification.Labels.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            arguments.Add("--label");
            arguments.Add($"{label.Key}={label.Value}");
        }

        foreach (var environment in specification.Environment.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            arguments.Add("--env");
            arguments.Add($"{environment.Key}={environment.Value}");
        }

        foreach (var mount in specification.Mounts)
        {
            arguments.Add("--mount");
            arguments.Add(
                $"type=bind,source={Path.GetFullPath(mount.Source)},target={mount.Target}" +
                (mount.ReadOnly ? ",readonly" : string.Empty));
        }

        foreach (var port in specification.Ports)
        {
            arguments.Add("--publish");
            var hostAddress = port.HostAddress.Contains(':')
                ? $"[{port.HostAddress}]"
                : port.HostAddress;
            arguments.Add($"{hostAddress}:{port.HostPort}:{port.ContainerPort}");
        }

        foreach (var tmpfs in specification.TmpfsMounts)
        {
            arguments.Add("--tmpfs");
            arguments.Add(string.IsNullOrWhiteSpace(tmpfs.Options)
                ? tmpfs.Target
                : $"{tmpfs.Target}:{tmpfs.Options}");
        }

        arguments.Add(specification.Image);
        arguments.AddRange(specification.Command);
        return arguments;
    }

    public static IReadOnlyList<string> BuildExecArguments(IsolatedRuntimeExecRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RuntimeHandle))
        {
            throw new ArgumentException("Runtime handle is required.", nameof(request));
        }

        if (request.Command.Count == 0 || request.Command.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Runtime command is required.", nameof(request));
        }

        var arguments = new List<string> { "exec" };
        if (request.StandardInput != null)
        {
            arguments.Add("-i");
        }

        AddPair(arguments, "--workdir", request.WorkingDirectory);
        if (request.Environment != null)
        {
            foreach (var environment in request.Environment.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                arguments.Add("--env");
                arguments.Add($"{environment.Key}={environment.Value}");
            }
        }

        arguments.Add(request.RuntimeHandle);
        arguments.AddRange(request.Command);
        return arguments;
    }

    private static void Validate(IsolatedRuntimeSpec specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        if (string.IsNullOrWhiteSpace(specification.Image))
        {
            throw new ArgumentException("Isolated runtime image is required.", nameof(specification));
        }

        if (specification.Command.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Isolated runtime command contains an empty argument.", nameof(specification));
        }

        foreach (var mount in specification.Mounts)
        {
            if (string.IsNullOrWhiteSpace(mount.Source) ||
                string.IsNullOrWhiteSpace(mount.Target) ||
                !Path.IsPathRooted(mount.Source) ||
                !mount.Target.StartsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Isolated runtime mounts require absolute host and container paths.");
            }
        }

        foreach (var port in specification.Ports)
        {
            if (port.HostPort is < 1 or > 65535 || port.ContainerPort is < 1 or > 65535 ||
                !IsSafeHostAddress(port.HostAddress))
            {
                throw new ArgumentException("Isolated runtime port binding is invalid.");
            }
        }

        if (!string.IsNullOrWhiteSpace(specification.NetworkMode) &&
            !specification.NetworkMode.All(character =>
                char.IsLetterOrDigit(character) || character is '-' or '_' or '.'))
        {
            throw new ArgumentException("Isolated runtime network name is invalid.");
        }
    }

    private static bool IsSafeHostAddress(string value) =>
        value is "127.0.0.1" or "0.0.0.0" or "::1";

    private static void AddPair(ICollection<string> arguments, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        arguments.Add(name);
        arguments.Add(value);
    }
}
