/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DiagnosticAIFunction.cs
    File description: Related implementation.
    -

    Created by: Senparc - 20260817

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Records the real function invocation boundary. Approval only confirms intent; this wrapper
/// distinguishes a successful invocation from argument binding or plugin execution failures.
/// </summary>
internal sealed class DiagnosticAIFunction : DelegatingAIFunction
{
    private readonly int _agentTemplateId;
    private readonly string _agentName;
    private readonly string _correlationId;

    /// <summary>
    /// Initializes a diagnostic wrapper around an AI function invocation.
    /// </summary>
    /// <param name="innerFunction">The underlying function to invoke.</param>
    /// <param name="agentTemplateId">The Agent template identifier for diagnostics.</param>
    /// <param name="agentName">The Agent display name.</param>
    /// <param name="correlationId">The invocation correlation identifier.</param>
    public DiagnosticAIFunction(
        AIFunction innerFunction,
        int agentTemplateId,
        string agentName,
        string correlationId)
        : base(innerFunction)
    {
        _agentTemplateId = agentTemplateId;
        _agentName = agentName ?? string.Empty;
        _correlationId = correlationId ?? string.Empty;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Logs redacted arguments and result metadata while allowing invocation exceptions
    /// to propagate unchanged to the caller.
    /// </remarks>
    protected override async ValueTask<object> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var argumentSummary = FormatArguments(arguments);
        SenparcTrace.SendCustomLog(
            "AgentsManager.ToolInvocation.Start",
            $"Correlation={_correlationId}; Agent={_agentTemplateId}:{_agentName}; " +
            $"Tool={Name}; Arguments={argumentSummary}");

        try
        {
            var result = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);
            SenparcTrace.SendCustomLog(
                "AgentsManager.ToolInvocation.Completed",
                $"Correlation={_correlationId}; Agent={_agentTemplateId}:{_agentName}; " +
                $"Tool={Name}; Result={DescribeResult(result)}");
            return result;
        }
        catch (Exception ex)
        {
            var root = ex.GetBaseException();
            SenparcTrace.SendCustomLog(
                "AgentsManager.ToolInvocation.Failed",
                $"Correlation={_correlationId}; Agent={_agentTemplateId}:{_agentName}; " +
                $"Tool={Name}; Arguments={argumentSummary}; Error={root.GetType().Name}: {root.Message}");
            throw;
        }
    }

    /// <summary>
    /// Formats invocation arguments into a bounded diagnostic string.
    /// </summary>
    /// <param name="arguments">The function arguments to format.</param>
    /// <returns>A comma-separated summary or <c>(none)</c>.</returns>
    private static string FormatArguments(IEnumerable<KeyValuePair<string, object>> arguments)
    {
        if (arguments == null)
        {
            return "(none)";
        }

        var values = arguments
            .Select(pair => $"{pair.Key}={FormatArgumentValue(pair.Key, pair.Value)}")
            .ToList();
        return values.Count == 0 ? "(none)" : string.Join(", ", values);
    }

    /// <summary>
    /// Formats one argument value, redacting sensitive keys and truncating long text.
    /// </summary>
    /// <param name="key">The argument key.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>A safe diagnostic representation.</returns>
    private static string FormatArgumentValue(string key, object value)
    {
        if (IsSensitiveKey(key))
        {
            return "[redacted]";
        }

        var text = value?.ToString() ?? "null";
        text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 500 ? text : text[..500] + "...";
    }

    /// <summary>
    /// Determines whether an argument key indicates credentials or authorization material.
    /// </summary>
    /// <param name="key">The argument key.</param>
    /// <returns><see langword="true"/> when the value must be redacted.</returns>
    private static bool IsSensitiveKey(string key)
    {
        var normalized = key ?? string.Empty;
        return normalized.Contains("key", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("token", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("secret", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("password", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("authorization", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Describes a result by type and text length without logging its content.
    /// </summary>
    /// <param name="result">The function result.</param>
    /// <returns>A type-and-length diagnostic description.</returns>
    private static string DescribeResult(object result)
    {
        if (result == null)
        {
            return "null";
        }

        var text = result.ToString() ?? string.Empty;
        return $"{result.GetType().Name}(length={text.Length})";
    }
}
