/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningWorkerLaunch.cs
    文件功能描述：校验并生成微调 Worker 的本机启动参数


    创建标识：Senparc - 20261106

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.OHS.Local.PL;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public readonly record struct FineTuningWorkerPeer(string Alias, string? LaunchProfileJson);

/// <summary>
/// Builds a fixed <c>python -m worker</c> start plan. The API key is an environment value, never an argument.
/// </summary>
public sealed class FineTuningWorkerLaunch
{
    public string PythonPath { get; private set; } = "";
    public string WorkingDirectory { get; private set; } = "";
    public string ModelsRoot { get; private set; } = "";
    public string DataRoot { get; private set; } = "";
    public string BindHost { get; private set; } = "";
    public int Port { get; private set; }
    public IReadOnlyList<string> Arguments { get; private set; } = [];
    public IReadOnlyDictionary<string, string> Variables { get; private set; } = new Dictionary<string, string>();

    public static string DefaultDataRoot(string contentRoot, string alias) =>
        Path.Combine(contentRoot, "App_Data", "Senparc.Xncf.AIKernel", "fine-tuning", "workers", alias, "data");

    public static string DiscoverProject(string? start)
    {
        foreach (var root in new[] { start, AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            DirectoryInfo? directory;
            try
            {
                directory = new DirectoryInfo(root);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"[AIKernel] Skip fine-tuning worker discovery from '{root}': {exception.Message}");
                continue;
            }

            for (var depth = 0; depth < 8 && directory != null; depth++, directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "tools", "AIKernelFineTuning");
                if (File.Exists(Path.Combine(candidate, "worker", "__main__.py")))
                    return candidate;
            }
        }

        return "";
    }

    public static FineTuningLaunchProfile Prepare(AIFineTuningWorker_CreateOrEditRequest request, string contentRoot,
        IEnumerable<FineTuningWorkerPeer> peers)
    {
        var profile = new FineTuningLaunchProfile
        {
            Managed = request.Managed,
            BindHost = string.IsNullOrWhiteSpace(request.BindHost) ? "127.0.0.1" : request.BindHost.Trim().Trim('[', ']'),
            Port = request.Port == 0 ? 8091 : request.Port,
            ModelsRoot = ExpandHome(request.ModelsRoot),
            DataRoot = ExpandHome(request.DataRoot),
            PythonPath = ExpandHome(request.PythonPath),
            WorkerProjectPath = ExpandHome(request.WorkerProjectPath),
            AutoStart = request.Managed && request.AutoStart
        };
        if (!profile.Managed)
        {
            profile.ToJson();
            return profile;
        }

        if (!FineTuningWorkerOptions.IsLoopbackHost(profile.BindHost))
            throw new NcfExceptionBase("A worker started by NCF must bind to a loopback address (127.0.0.1, localhost, or ::1).");
        if (profile.Port is < 1024 or > 65535)
            throw new NcfExceptionBase("The managed worker port must be between 1024 and 65535.");
        if (string.IsNullOrWhiteSpace(profile.DataRoot))
            profile.DataRoot = DefaultDataRoot(contentRoot, request.Alias);
        profile.ModelsRoot = NormalizeDirectory(profile.ModelsRoot, "models");
        profile.DataRoot = NormalizeDirectory(profile.DataRoot, "data");
        if (PathsOverlap(profile.ModelsRoot, profile.DataRoot))
            throw new NcfExceptionBase("The models directory and the data directory must not overlap.");
        var secrets = FineTuningSecretStore.DirectoryFor(contentRoot);
        if (PathsOverlap(profile.ModelsRoot, secrets) || PathsOverlap(profile.DataRoot, secrets))
            throw new NcfExceptionBase("Model and data directories must not contain the worker secret directory.");
        profile.WorkerProjectPath = ResolveProject(profile.WorkerProjectPath, contentRoot);
        if (!string.IsNullOrWhiteSpace(profile.PythonPath))
            profile.PythonPath = NormalizePython(profile.PythonPath);
        EnsurePeers(request.Alias, profile, peers);
        request.Endpoint = new UriBuilder(Uri.UriSchemeHttp, profile.BindHost, profile.Port).Uri.AbsoluteUri;
        profile.ToJson();
        return profile;
    }

    public static FineTuningWorkerLaunch Create(string alias, FineTuningLaunchProfile profile, string apiKey,
        IEnumerable<FineTuningWorkerPeer> peers)
    {
        if (!profile.Managed)
            throw new NcfExceptionBase("This fine-tuning worker is not configured to be started by NCF.");
        if (!FineTuningWorkerOptions.IsValidApiKey(apiKey))
            throw new NcfExceptionBase($"No valid fine-tuning worker secret is configured for '{alias}'.");
        if (!FineTuningWorkerOptions.IsLoopbackHost(profile.BindHost))
            throw new NcfExceptionBase("A worker started by NCF must bind to a loopback address (127.0.0.1, localhost, or ::1).");
        EnsurePeers(alias, profile, peers);
        Directory.CreateDirectory(profile.ModelsRoot);
        Directory.CreateDirectory(profile.DataRoot);
        RejectSymlink(profile.ModelsRoot, "models");
        RejectSymlink(profile.DataRoot, "data");
        var python = string.IsNullOrWhiteSpace(profile.PythonPath) ? ResolvePython(profile.WorkerProjectPath) : profile.PythonPath;
        if (!python.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal) && !python.Contains('/', StringComparison.Ordinal))
        {
            // PATH lookup is intentional only for the bare python/python3 fallback.
        }
        else if (!File.Exists(python))
        {
            throw new NcfExceptionBase($"Python was not found at '{python}'.");
        }

        Debug.WriteLine($"[AIKernel] Prepared fine-tuning worker '{alias}' command: '{python}' -m worker in '{profile.WorkerProjectPath}'.");
        return new FineTuningWorkerLaunch
        {
            PythonPath = python,
            WorkingDirectory = profile.WorkerProjectPath,
            ModelsRoot = profile.ModelsRoot,
            DataRoot = profile.DataRoot,
            BindHost = profile.BindHost,
            Port = profile.Port,
            Arguments = ["-m", "worker"],
            Variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["NCF_WORKER_KEY"] = apiKey,
                ["NCF_MODELS_ROOT"] = profile.ModelsRoot,
                ["NCF_DATA_ROOT"] = profile.DataRoot,
                ["NCF_BIND_HOST"] = profile.BindHost,
                ["NCF_WORKER_PORT"] = profile.Port.ToString(CultureInfo.InvariantCulture),
                ["PYTHONUNBUFFERED"] = "1"
            }
        };
    }

    public static string Preview(FineTuningLaunchProfile profile, string secretPath)
    {
        if (string.IsNullOrWhiteSpace(profile.ModelsRoot) || string.IsNullOrWhiteSpace(profile.DataRoot))
            return "";
        var python = profile.PythonPath;
        if (string.IsNullOrWhiteSpace(python) && !string.IsNullOrWhiteSpace(profile.WorkerProjectPath))
        {
            var venv = Path.Combine(profile.WorkerProjectPath, ".venv", OperatingSystem.IsWindows() ? Path.Combine("Scripts", "python.exe") : Path.Combine("bin", "python"));
            if (File.Exists(venv))
                python = venv;
        }
        if (string.IsNullOrWhiteSpace(python))
            python = OperatingSystem.IsWindows() ? "python" : "python3";
        var lines = new List<string>
        {
            "export NCF_WORKER_KEY=\"$(cat " + Quote(secretPath) + ")\"",
            "export NCF_MODELS_ROOT=" + Quote(profile.ModelsRoot),
            "export NCF_DATA_ROOT=" + Quote(profile.DataRoot),
            "export NCF_BIND_HOST=" + Quote(profile.BindHost),
            "export NCF_WORKER_PORT=\"" + profile.Port.ToString(CultureInfo.InvariantCulture) + "\""
        };
        if (!string.IsNullOrWhiteSpace(profile.WorkerProjectPath))
            lines.Add("cd " + Quote(profile.WorkerProjectPath));
        lines.Add(Quote(python) + " -m worker");
        return string.Join('\n', lines);
    }

    private static void EnsurePeers(string alias, FineTuningLaunchProfile profile, IEnumerable<FineTuningWorkerPeer> peers)
    {
        foreach (var peer in peers)
        {
            if (string.Equals(peer.Alias, alias, StringComparison.OrdinalIgnoreCase)
                || !FineTuningLaunchProfile.TryFromJson(peer.LaunchProfileJson, out var other)
                || !other.Managed)
                continue;
            if (other.Port == profile.Port)
                throw new NcfExceptionBase($"Port {profile.Port} is already assigned to fine-tuning worker '{peer.Alias}'.");
            if (PathsOverlap(other.DataRoot, profile.DataRoot))
                throw new NcfExceptionBase($"The data directory overlaps fine-tuning worker '{peer.Alias}'.");
        }
    }

    private static string ResolveProject(string configured, string contentRoot)
    {
        var project = string.IsNullOrWhiteSpace(configured) ? DiscoverProject(contentRoot) : Path.GetFullPath(configured);
        if (string.IsNullOrWhiteSpace(project) || !File.Exists(Path.Combine(project, "worker", "__main__.py")))
            throw new NcfExceptionBase("The fine-tuning worker project was not found. Set the absolute path of tools/AIKernelFineTuning.");
        return project;
    }

    private static string ResolvePython(string project)
    {
        var venv = Path.Combine(project, ".venv", OperatingSystem.IsWindows() ? Path.Combine("Scripts", "python.exe") : Path.Combine("bin", "python"));
        return File.Exists(venv) ? venv : OperatingSystem.IsWindows() ? "python" : "python3";
    }

    private static string NormalizePython(string path)
    {
        if (!Path.IsPathRooted(path))
            throw new NcfExceptionBase("PythonPath must be an absolute path.");
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new NcfExceptionBase($"Python was not found at '{full}'.");
        return full;
    }

    private static string NormalizeDirectory(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new NcfExceptionBase($"The {label} directory is required.");
        if (path.IndexOfAny(['\'', '\r', '\n', '\0']) >= 0)
            throw new NcfExceptionBase($"The {label} directory contains unsupported characters.");
        if (!Path.IsPathRooted(path))
            throw new NcfExceptionBase($"The {label} directory must be an absolute path.");
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full);
        if (string.Equals(TrimSlash(full), TrimSlash(root ?? ""), StringComparison.OrdinalIgnoreCase))
            throw new NcfExceptionBase($"The {label} directory must not be a filesystem root.");
        if (full.Length > 500)
            throw new NcfExceptionBase($"The {label} directory path is too long.");
        RejectSymlink(full, label);
        return full;
    }

    private static void RejectSymlink(string path, string label)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
            return;
        var info = new DirectoryInfo(path);
        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new NcfExceptionBase($"The {label} directory must not be a symbolic link.");
    }

    public static bool PathsOverlap(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;
        try
        {
            var first = TrimSlash(Path.GetFullPath(left)) + Path.DirectorySeparatorChar;
            var second = TrimSlash(Path.GetFullPath(right)) + Path.DirectorySeparatorChar;
            return first.StartsWith(second, StringComparison.OrdinalIgnoreCase)
                || second.StartsWith(first, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[AIKernel] Could not compare fine-tuning directories: {exception.Message}");
            return false;
        }
    }

    private static string ExpandHome(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        path = path.Trim();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path == "~")
            return home;
        if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
            return Path.Combine(home, path[2..]);
        return path;
    }

    private static string Quote(string value)
    {
        if (value.IndexOfAny(['\'', '\r', '\n', '\0']) >= 0)
            throw new NcfExceptionBase("Paths used by the fine-tuning worker cannot contain quotes or line breaks.");
        return "'" + value + "'";
    }

    private static string TrimSlash(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
