/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningWorkerLaunch.cs
    File description: Related implementation.

    Created by: Senparc - 20261106

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

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

    /// <summary>
    /// Computes the default tenant-local data root for a worker alias.
    /// </summary>
    /// <param name="contentRoot">The application content root.</param>
    /// <param name="alias">The worker alias.</param>
    /// <returns>The default data directory path.</returns>
    public static string DefaultDataRoot(string contentRoot, string alias) =>
        Path.Combine(contentRoot, "App_Data", "Senparc.Xncf.AIKernel", "fine-tuning", "workers", alias, "data");

    /// <summary>
    /// Searches upward from the supplied path and application base directory for the bundled worker project.
    /// </summary>
    /// <param name="start">Optional starting directory.</param>
    /// <returns>The worker project directory, or an empty string when it cannot be found.</returns>
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

    /// <summary>
    /// Normalizes and validates a persisted launch profile request without starting a process.
    /// </summary>
    /// <param name="request">The worker launch configuration request.</param>
    /// <param name="contentRoot">The application content root used for defaults.</param>
    /// <param name="peers">Other worker profiles used for collision checks.</param>
    /// <returns>A normalized launch profile.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when loopback, port, path, secret-directory, or peer constraints fail.</exception>
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

    /// <summary>
    /// Builds the fixed Python worker process launch plan and environment variables.
    /// </summary>
    /// <param name="alias">The worker alias.</param>
    /// <param name="profile">The validated managed launch profile.</param>
    /// <param name="apiKey">The secret passed only through the process environment.</param>
    /// <param name="peers">Other worker profiles used for collision checks.</param>
    /// <returns>A launch plan containing executable, arguments, paths, and environment values.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when managed mode, API key, loopback, path, Python, or peer validation fails.</exception>
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

    /// <summary>
    /// Produces a shell command preview for a validated launch profile without exposing the secret value.
    /// </summary>
    /// <param name="profile">The launch profile to render.</param>
    /// <param name="secretPath">The path from which the shell reads the secret.</param>
    /// <returns>A newline-delimited shell command preview, or an empty string for incomplete paths.</returns>
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

    /// <summary>
    /// Checks a worker profile against managed peers for alias, port, and data-directory conflicts.
    /// </summary>
    /// <param name="alias">The worker alias being prepared.</param>
    /// <param name="profile">The normalized launch profile.</param>
    /// <param name="peers">Other persisted worker profiles.</param>
    /// <exception cref="NcfExceptionBase">Thrown when a peer conflicts with the profile.</exception>
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

    /// <summary>
    /// Resolves and validates the bundled fine-tuning worker project directory.
    /// </summary>
    /// <param name="configured">An optional configured project path.</param>
    /// <param name="contentRoot">The application content root used for discovery.</param>
    /// <returns>The worker project directory.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the worker project cannot be found.</exception>
    private static string ResolveProject(string configured, string contentRoot)
    {
        var project = string.IsNullOrWhiteSpace(configured) ? DiscoverProject(contentRoot) : Path.GetFullPath(configured);
        if (string.IsNullOrWhiteSpace(project) || !File.Exists(Path.Combine(project, "worker", "__main__.py")))
            throw new NcfExceptionBase("The fine-tuning worker project was not found. Set the absolute path of tools/AIKernelFineTuning.");
        return project;
    }

    /// <summary>
    /// Resolves the project virtual-environment Python executable or a platform fallback.
    /// </summary>
    /// <param name="project">The worker project directory.</param>
    /// <returns>The Python executable path/name.</returns>
    private static string ResolvePython(string project)
    {
        var venv = Path.Combine(project, ".venv", OperatingSystem.IsWindows() ? Path.Combine("Scripts", "python.exe") : Path.Combine("bin", "python"));
        return File.Exists(venv) ? venv : OperatingSystem.IsWindows() ? "python" : "python3";
    }

    /// <summary>
    /// Normalizes and validates an explicitly configured absolute Python path.
    /// </summary>
    /// <param name="path">The configured Python path.</param>
    /// <returns>The normalized absolute path.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the path is relative or missing.</exception>
    private static string NormalizePython(string path)
    {
        if (!Path.IsPathRooted(path))
            throw new NcfExceptionBase("PythonPath must be an absolute path.");
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new NcfExceptionBase($"Python was not found at '{full}'.");
        return full;
    }

    /// <summary>
    /// Validates and normalizes a model/data directory, rejecting roots, unsafe characters,
    /// excessive lengths, and symbolic links.
    /// </summary>
    /// <param name="path">The configured directory path.</param>
    /// <param name="label">The path label used in errors.</param>
    /// <returns>The normalized absolute directory path.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the directory violates safety constraints.</exception>
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

    /// <summary>
    /// Rejects an existing directory or file when it is a reparse point/symbolic link.
    /// </summary>
    /// <param name="path">The path to inspect.</param>
    /// <param name="label">The path label used in errors.</param>
    /// <exception cref="NcfExceptionBase">Thrown when the path is a symbolic link.</exception>
    private static void RejectSymlink(string path, string label)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
            return;
        var info = new DirectoryInfo(path);
        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new NcfExceptionBase($"The {label} directory must not be a symbolic link.");
    }

    /// <summary>
    /// Determines whether two normalized paths overlap by containment.
    /// Invalid paths return <see langword="false"/> after diagnostic logging.
    /// </summary>
    /// <param name="left">The first path.</param>
    /// <param name="right">The second path.</param>
    /// <returns><see langword="true"/> when either path contains the other.</returns>
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

    /// <summary>
    /// Expands a leading tilde or tilde-slash path using the current user's home directory.
    /// </summary>
    /// <param name="path">The path to expand.</param>
    /// <returns>The expanded path, or an empty string for blank input.</returns>
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

    /// <summary>
    /// Quotes a path for the generated shell preview and rejects shell-sensitive characters.
    /// </summary>
    /// <param name="value">The path value.</param>
    /// <returns>A single-quoted shell-safe path.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the path contains quotes or line breaks.</exception>
    private static string Quote(string value)
    {
        if (value.IndexOfAny(['\'', '\r', '\n', '\0']) >= 0)
            throw new NcfExceptionBase("Paths used by the fine-tuning worker cannot contain quotes or line breaks.");
        return "'" + value + "'";
    }

    /// <summary>Removes trailing platform path separators.</summary>
    /// <param name="path">The path to trim.</param>
    /// <returns>The path without trailing separators.</returns>
    private static string TrimSlash(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
