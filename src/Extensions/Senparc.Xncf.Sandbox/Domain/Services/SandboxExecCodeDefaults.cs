/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxExecCodeDefaults.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxExecCodeDefaults
{
    public const string PythonHello = "print('hello from NCF Sandbox')";
    public const string CsharpHello = "Console.WriteLine(\"hello from NCF Sandbox\");";

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static string Normalize(string templateKey, string? code)
    {
        var trimmed = (code ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return IsCsharp(templateKey) ? CsharpHello : PythonHello;
        }

        if (IsCsharp(templateKey) && IsPythonHelloSample(trimmed))
        {
            return CsharpHello;
        }

        return code!;
    }

    public static bool IsPythonHelloSample(string code)
    {
        var normalized = code.Trim().Replace("\r\n", "\n").TrimEnd(';');
        return string.Equals(normalized, PythonHello, StringComparison.Ordinal)
               || string.Equals(normalized, "print(\"hello from NCF Sandbox\")", StringComparison.Ordinal);
    }

    private static bool IsCsharp(string templateKey) =>
        string.Equals(templateKey, SandboxTemplateKeys.CsharpExec, StringComparison.OrdinalIgnoreCase);
}
