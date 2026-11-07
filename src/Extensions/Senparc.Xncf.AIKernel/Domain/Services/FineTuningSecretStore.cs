/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningSecretStore.cs
    文件功能描述：把微调 Worker 密钥保存在站点指定目录


    创建标识：Senparc - 20261106

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.18.0 新增微调密钥存储服务
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
    public FineTuningSecretStore(IHostEnvironment environment) : this(DirectoryFor(environment.ContentRootPath))
    {
    }

    public FineTuningSecretStore(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathRooted(directory))
            throw new ArgumentException("The fine-tuning secret directory must be an absolute path.", nameof(directory));
        DirectoryPath = Path.GetFullPath(directory);
    }

    public static string DirectoryFor(string contentRoot) =>
        Path.GetFullPath(Path.Combine(contentRoot, "App_Data", "Senparc.Xncf.AIKernel", "fine-tuning", "secrets"));

    public string PathFor(string alias)
    {
        var safeAlias = NormalizeAlias(alias);
        var path = Path.GetFullPath(Path.Combine(DirectoryPath, safeAlias + ".key"));
        if (!path.StartsWith(DirectoryPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new NcfExceptionBase("The fine-tuning worker alias cannot address a path outside the secret directory.");
        return path;
    }

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

    public string Resolve(FineTuningWorkerOptions options, string alias)
    {
        if (options.WorkerApiKeys.TryGetValue(alias, out var configured) && FineTuningWorkerOptions.IsValidApiKey(configured))
            return configured;
        if (TryRead(alias, out var stored))
            return stored;
        throw new NcfExceptionBase($"No valid fine-tuning worker secret is configured for '{alias}'.");
    }

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

    private static string NormalizeAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias) || !AliasPattern.IsMatch(alias))
            throw new NcfExceptionBase("Fine-tuning worker alias must match ^[a-zA-Z0-9][a-zA-Z0-9_-]{0,49}$.");
        return alias;
    }

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
