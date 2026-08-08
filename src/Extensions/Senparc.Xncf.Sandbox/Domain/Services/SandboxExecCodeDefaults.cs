/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxExecCodeDefaults.cs
    文件功能描述：Exec 示例代码与按模板归一化（避免 Python 默认值误用于 C#）

    创建标识：Senparc - 20260808

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxExecCodeDefaults
{
    public const string PythonHello = "print('hello from NCF Sandbox')";
    public const string CsharpHello = "Console.WriteLine(\"hello from NCF Sandbox\");";

    /// <summary>
    /// Function 表单共用一个 Code 字段；若用户未改 Python 默认值却在 csharp-exec 会话中执行，自动换成 C# 示例。
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
