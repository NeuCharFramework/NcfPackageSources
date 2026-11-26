/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpEndpointSecretProtector.cs
    File description: protects MCP authentication configuration

    Created by: Senparc - 20261126
    Change description: v0.5.15 protect MCP credentials with ASP.NET Core Data Protection

----------------------------------------------------------------*/

using System;
using Microsoft.AspNetCore.DataProtection;

namespace Senparc.Xncf.MCP.Domain.Services;

public sealed class McpEndpointSecretProtector
{
    private const string Prefix = "mcp-dp:";
    private readonly IDataProtector _protector;

    public McpEndpointSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Senparc.Xncf.MCP.EndpointAuth.v1");
    }

    public string? Protect(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : Prefix + _protector.Protect(value.Trim());
    }

    public string? Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.StartsWith(Prefix, StringComparison.Ordinal)
            ? _protector.Unprotect(value[Prefix.Length..])
            : value;
    }
}
