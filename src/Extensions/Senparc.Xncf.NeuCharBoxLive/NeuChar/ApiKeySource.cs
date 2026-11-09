/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ApiKeySource.cs
    文件功能描述：NeuCharBox Live NeuChar 模型客户端：ApiKeySource


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.NeuChar;

/// <summary>
/// Where the NeuChar key comes from: env var NEUCHAR_API_KEY, never config. Callers pass the value straight to
/// <see cref="NeuCharChatClientFactory.Create"/> and print only whether it is present.
/// </summary>
public static class ApiKeySource
{
    public const string Variable = "NEUCHAR_API_KEY";

    /// <summary>
    /// The process environment's NEUCHAR_API_KEY, trimmed. On Windows only, when the process has none, the user-level
    /// variable (a process started before Leo set it, or by a service host, doesn't inherit it). Null when missing.
    /// </summary>
    public static string? Read() => Read(() => Environment.GetEnvironmentVariable(Variable),
        OperatingSystem.IsWindows() ? () => Environment.GetEnvironmentVariable(Variable, EnvironmentVariableTarget.User) : null);

    /// <summary>The lookup order, with the reads passed in so tests never touch the real variable.</summary>
    internal static string? Read(Func<string?> process, Func<string?>? user)
    {
        if (Clean(process()) is { } p) return p;
        return OperatingSystem.IsWindows() && user is not null ? Clean(user()) : null;
    }

    static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
