/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningSecretStore.cs
    File description: Related implementation.

    Created by: Senparc - 20261106

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Xncf.AIKernel.Domain.Services;

/// <summary>
/// One 0600 key file per worker alias. The database and the admin UI only learn that the file exists.
/// </summary>
public sealed class FineTuningSecretStore
{
    private static readonly Regex AliasPattern = new("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,49}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string DirectoryPath { get; }

    [ActivatorUtilitiesConstructor]
    /// <summary>
    /// Initializes the secret store below the host content root.
    /// </summary>
    /// <param name="environment">Host environment used to resolve the secret directory.</param>
    public FineTuningSecretStore(IHostEnvironment environment) : this(DirectoryFor(environment.ContentRootPath))
    {
    }

    /// <summary>
    /// Initializes the secret store with an absolute directory path.
    /// </summary>
    /// <param name="directory">The absolute secret directory.</param>
    /// <exception cref="ArgumentException">Thrown when the directory is empty or relative.</exception>
    public FineTuningSecretStore(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathRooted(directory))
            throw new ArgumentException("The fine-tuning secret directory must be an absolute path.", nameof(directory));
        DirectoryPath = Path.GetFullPath(directory);
    }

    /// <summary>
    /// Computes the default application-data secret directory.
    /// </summary>
    /// <param name="contentRoot">The application content root.</param>
    /// <returns>The absolute secret directory path.</returns>
    public static string DirectoryFor(string contentRoot) =>
        Path.GetFullPath(Path.Combine(contentRoot, "App_Data", "Senparc.Xncf.AIKernel", "fine-tuning", "secrets"));

    /// <summary>
    /// Resolves a validated worker alias to its key-file path.
    /// </summary>
    /// <param name="alias">The worker alias.</param>
    /// <returns>The key-file path inside the configured secret directory.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the alias could address a path outside the secret directory.</exception>
    public string PathFor(string alias)
    {
        var safeAlias = NormalizeAlias(alias);
        var path = Path.GetFullPath(Path.Combine(DirectoryPath, safeAlias + ".key"));
        if (!path.StartsWith(DirectoryPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new NcfExceptionBase("The fine-tuning worker alias cannot address a path outside the secret directory.");
        return path;
    }

    /// <summary>
    /// Reads and validates a worker secret file.
    /// </summary>
    /// <param name="alias">The worker alias.</param>
    /// <param name="key">Receives the valid secret when found.</param>
    /// <returns><see langword="true"/> when a valid secret exists.</returns>
    public bool TryRead(string alias, out string key)
    {
        key = "";
        string path;
        try
        {
            path = PathFor(alias);
        }
        catch (NcfExceptionBase)
        {
            return false;
        }

        if (!File.Exists(path))
            return false;
        var text = File.ReadAllText(path).TrimEnd('\r', '\n');
        if (!FineTuningWorkerOptions.IsValidApiKey(text))
            return false;
        key = text;
        return true;
    }

    /// <summary>
    /// Resolves a worker secret from configured options first, then the protected file store.
    /// </summary>
    /// <param name="options">Fine-tuning worker options.</param>
    /// <param name="alias">The worker alias.</param>
    /// <returns>A valid worker secret.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when no valid secret is available.</exception>
    public string Resolve(FineTuningWorkerOptions options, string alias)
    {
        if (options.WorkerApiKeys.TryGetValue(alias, out var configured) && FineTuningWorkerOptions.IsValidApiKey(configured))
            return configured;
        if (TryRead(alias, out var stored))
            return stored;
        throw new NcfExceptionBase($"No valid fine-tuning worker secret is configured for '{alias}'.");
    }

    /// <summary>
    /// Lists aliases with valid key files in the secret directory.
    /// </summary>
    /// <returns>Configured worker aliases.</returns>
    public IReadOnlyList<string> ListConfiguredAliases()
    {
        if (!Directory.Exists(DirectoryPath))
            return [];
        var aliases = new List<string>();
        foreach (var file in Directory.EnumerateFiles(DirectoryPath, "*.key"))
        {
            var alias = Path.GetFileNameWithoutExtension(file);
            if (TryRead(alias, out _))
                aliases.Add(alias);
        }
        return aliases;
    }

    /// <summary>
    /// Creates a random 256-bit worker secret file when one does not already exist.
    /// </summary>
    /// <param name="alias">The worker alias.</param>
    /// <returns>Whether a new file was created and its path.</returns>
    public (bool Created, string Path) Generate(string alias)
    {
        var path = PathFor(alias);
        Directory.CreateDirectory(DirectoryPath);
        Protect(DirectoryPath, directory: true);
        if (File.Exists(path))
        {
            Debug.WriteLine($"[AIKernel] Fine-tuning worker secret already exists for '{alias}' at '{path}'.");
            return (false, path);
        }

        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var temporary = Path.Combine(DirectoryPath, "." + alias + "." + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(temporary, key);
        Protect(temporary, directory: false);
        File.Move(temporary, path);
        Debug.WriteLine($"[AIKernel] Fine-tuning worker secret created for '{alias}' at '{path}'.");
        return (true, path);
    }

    /// <summary>
    /// Validates the worker alias format used for secret-file names.
    /// </summary>
    /// <param name="alias">The worker alias.</param>
    /// <returns>The validated alias.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the alias is empty or contains unsafe characters.</exception>
    private static string NormalizeAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias) || !AliasPattern.IsMatch(alias))
            throw new NcfExceptionBase("Fine-tuning worker alias must match ^[a-zA-Z0-9][a-zA-Z0-9_-]{0,49}$.");
        return alias;
    }

    /// <summary>
    /// Applies owner-only Unix permissions where supported; permission failures are diagnostic-only.
    /// </summary>
    /// <param name="path">The file or directory path.</param>
    /// <param name="directory">Whether the path is a directory.</param>
    private static void Protect(string path, bool directory)
    {
        if (OperatingSystem.IsWindows())
            return;
        try
        {
            var mode = directory
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                : UnixFileMode.UserRead | UnixFileMode.UserWrite;
            File.SetUnixFileMode(path, mode);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[AIKernel] Could not set fine-tuning secret permissions for '{path}': {exception.Message}");
        }
    }
}
