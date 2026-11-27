/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxExecCodeDefaults.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxExecCodeDefaults
{
    public const string PythonHello = "print('hello from NCF Sandbox')";
    public const string CsharpHello = "Console.WriteLine(\"hello from NCF Sandbox\");";

    /// <summary>
    /// Returns the supplied code after applying template-specific default and compatibility
    /// normalization rules. Empty code receives the language's hello-world sample; the Python
    /// sample is translated to the C# sample for the C# execution template.
    /// </summary>
    /// <param name="templateKey">The Sandbox template key used to select language behavior.</param>
    /// <param name="code">The caller-provided source code, which may be empty.</param>
    /// <returns>The normalized source code to execute.</returns>
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

    /// <summary>
    /// Determines whether source matches one of the supported Python hello-world samples.
    /// </summary>
    /// <param name="code">The source code to compare after line-ending and semicolon normalization.</param>
    /// <returns><see langword="true"/> when the source is a recognized Python sample.</returns>
    public static bool IsPythonHelloSample(string code)
    {
        var normalized = code.Trim().Replace("\r\n", "\n").TrimEnd(';');
        return string.Equals(normalized, PythonHello, StringComparison.Ordinal)
               || string.Equals(normalized, "print(\"hello from NCF Sandbox\")", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether the supplied template key represents the C# execution template.
    /// </summary>
    /// <param name="templateKey">The Sandbox template key.</param>
    /// <returns><see langword="true"/> for the C# execution template.</returns>
    private static bool IsCsharp(string templateKey) =>
        string.Equals(templateKey, SandboxTemplateKeys.CsharpExec, StringComparison.OrdinalIgnoreCase);
}
