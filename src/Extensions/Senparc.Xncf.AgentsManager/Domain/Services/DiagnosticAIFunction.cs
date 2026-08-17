/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DiagnosticAIFunction.cs
    文件功能描述：DiagnosticAIFunction.cs 功能实现
    
    
    创建标识：Senparc - 20260817
    
    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略
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

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key ?? string.Empty;
        return normalized.Contains("key", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("token", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("secret", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("password", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("authorization", StringComparison.OrdinalIgnoreCase);
    }

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
