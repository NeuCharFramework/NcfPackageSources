/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowEngine.cs
    文件功能描述：新手友好的 NeuChar Workflow 声明式存储与执行引擎
----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Shared.Abstractions.ChatAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class NeuCharWorkflowGraph
{
    public List<NeuCharWorkflowNode> Nodes { get; set; } = new();
    public List<NeuCharWorkflowEdge> Edges { get; set; } = new();
}

public sealed class NeuCharWorkflowNode
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public JsonObject Config { get; set; } = new();
}

public sealed class NeuCharWorkflowEdge
{
    public string Id { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public string SourceHandle { get; set; }
}

public sealed record NeuCharWorkflowRunResult(
    bool Success,
    string Output,
    IReadOnlyList<string> Trace,
    string ErrorMessage = null);

public sealed class NeuCharWorkflowEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "manual-trigger", "interval-trigger", "function", "delay", "condition", "agent", "agent-group", "end"
    };

    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharPivotFunctionService _functionEntityService;
    private readonly NeuCharFunctionService _functionService;
    private readonly NeuCharExecutionLogService _logService;
    private readonly NeuCharParameterProtector _parameterProtector;
    private readonly IReadOnlyDictionary<string, IWorkflowObjectProvider> _objectProviders;

    public NeuCharWorkflowEngine(
        NeuCharWorkflowService workflowService,
        NeuCharPivotFunctionService functionEntityService,
        NeuCharFunctionService functionService,
        NeuCharExecutionLogService logService,
        NeuCharParameterProtector parameterProtector,
        IEnumerable<IWorkflowObjectProvider> objectProviders)
    {
        _workflowService = workflowService;
        _functionEntityService = functionEntityService;
        _functionService = functionService;
        _logService = logService;
        _parameterProtector = parameterProtector;
        _objectProviders = objectProviders
            .GroupBy(z => z.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(z => z.Key, z => z.First(), StringComparer.OrdinalIgnoreCase);
    }

    public NeuCharWorkflowGraph ParseAndValidateGraph(string graphJson)
    {
        if (graphJson?.Length > 1_000_000)
        {
            throw new InvalidOperationException("工作流图不能超过 1000000 个字符。");
        }

        NeuCharWorkflowGraph graph;
        try
        {
            graph = JsonSerializer.Deserialize<NeuCharWorkflowGraph>(
                string.IsNullOrWhiteSpace(graphJson) ? "{}" : graphJson,
                JsonOptions) ?? new NeuCharWorkflowGraph();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("工作流图不是有效的 JSON。", ex);
        }

        graph.Nodes ??= new List<NeuCharWorkflowNode>();
        graph.Edges ??= new List<NeuCharWorkflowEdge>();
        foreach (var node in graph.Nodes)
        {
            node.Config ??= new JsonObject();
        }
        if (graph.Nodes.Count > 100 || graph.Edges.Count > 200)
        {
            throw new InvalidOperationException("单个工作流最多允许 100 个节点和 200 条连接。");
        }

        if (graph.Nodes.Any(z => string.IsNullOrWhiteSpace(z.Id) || !AllowedNodeTypes.Contains(z.Type)))
        {
            throw new InvalidOperationException("工作流包含无效节点类型或空节点 ID。");
        }

        var ids = graph.Nodes.Select(z => z.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != graph.Nodes.Count)
        {
            throw new InvalidOperationException("工作流节点 ID 不能重复。");
        }
        if (graph.Edges.Any(z => !ids.Contains(z.Source) || !ids.Contains(z.Target)))
        {
            throw new InvalidOperationException("工作流连接引用了不存在的节点。");
        }
        if (graph.Nodes.Count == 0)
        {
            throw new InvalidOperationException("工作流至少需要一个触发器节点。");
        }
        var triggerNodes = graph.Nodes
            .Where(z => z.Type.EndsWith("trigger", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (triggerNodes.Count != 1)
        {
            throw new InvalidOperationException("工作流必须且只能包含一个触发器节点。");
        }

        var edgeIds = graph.Edges.Select(z => z.Id).ToList();
        if (edgeIds.Any(string.IsNullOrWhiteSpace) ||
            edgeIds.Distinct(StringComparer.Ordinal).Count() != edgeIds.Count)
        {
            throw new InvalidOperationException("工作流连接 ID 不能为空或重复。");
        }
        if (graph.Edges.Any(z => string.Equals(z.Source, z.Target, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("工作流节点不能连接到自身。");
        }

        var nodeMap = graph.Nodes.ToDictionary(z => z.Id, StringComparer.Ordinal);
        foreach (var edge in graph.Edges)
        {
            var sourceNode = nodeMap[edge.Source];
            edge.SourceHandle = NormalizeSourceHandle(sourceNode.Type, edge.SourceHandle);
        }

        if (graph.Edges.Any(z => string.Equals(z.Target, triggerNodes[0].Id, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("触发器节点不能有输入连接。");
        }

        foreach (var node in graph.Nodes)
        {
            var outgoing = graph.Edges.Where(z => z.Source == node.Id).ToList();
            if (node.Type.Equals("condition", StringComparison.OrdinalIgnoreCase))
            {
                if (outgoing.Any(z => z.SourceHandle is not ("true" or "false")) ||
                    outgoing.GroupBy(z => z.SourceHandle, StringComparer.OrdinalIgnoreCase).Any(z => z.Count() > 1))
                {
                    throw new InvalidOperationException($"条件节点“{node.Name ?? node.Id}”的真/假分支各只能连接一个节点。");
                }
            }
            else if (node.Type.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                if (outgoing.Count > 0)
                {
                    throw new InvalidOperationException($"结束节点“{node.Name ?? node.Id}”不能有输出连接。");
                }
            }
            else if (outgoing.Count > 1)
            {
                throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”只能连接一个后续节点。");
            }
        }

        EnsureAcyclic(graph);
        EnsureAllNodesReachable(graph, triggerNodes[0].Id);
        return graph;
    }

    public async Task<string> ValidateReferencesAsync(
        NeuCharWorkflowGraph graph,
        CancellationToken cancellationToken = default)
    {
        foreach (var node in graph.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node.Type.Equals("function", StringComparison.OrdinalIgnoreCase))
            {
                var functionId = GetInt(node.Config, "functionId", 0);
                var function = await _functionEntityService.GetObjectAsync(z => z.Id == functionId)
                    .ConfigureAwait(false);
                if (function == null || !function.Visible)
                {
                    return $"节点“{node.Name ?? node.Id}”引用的 Function 不存在或已失效。";
                }

                var catalog = await _functionService.GetCatalogAsync(
                    function.ModuleUid,
                    true,
                    cancellationToken).ConfigureAwait(false);
                var descriptor = catalog.FirstOrDefault(z =>
                    string.Equals(z.FunctionKey, function.FunctionKey, StringComparison.OrdinalIgnoreCase));
                if (descriptor == null || !descriptor.ModuleAvailable)
                {
                    return $"节点“{node.Name ?? function.FunctionName}”所属模块未安装、未加载或未开启。";
                }

                var parameterJson = node.Config?["parameters"]?.ToJsonString() ?? function.DefaultParametersJson;
                var validationError = NeuCharFunctionService.ValidateRequiredParameters(
                    descriptor.Parameters,
                    parameterJson);
                if (validationError != null)
                {
                    return $"节点“{node.Name ?? function.FunctionName}”：{validationError}";
                }
            }
            else if (node.Type.Equals("agent", StringComparison.OrdinalIgnoreCase) ||
                     node.Type.Equals("agent-group", StringComparison.OrdinalIgnoreCase))
            {
                var providerId = GetString(node.Config, "providerId");
                var objectId = GetString(node.Config, "objectId");
                if (string.IsNullOrWhiteSpace(providerId) ||
                    !_objectProviders.TryGetValue(providerId, out var provider))
                {
                    return $"节点“{node.Name ?? node.Id}”的 Provider 不可用。";
                }

                var objects = await provider.GetObjectsAsync(cancellationToken).ConfigureAwait(false);
                var descriptor = objects.FirstOrDefault(z =>
                    string.Equals(z.ObjectId, objectId, StringComparison.Ordinal));
                if (descriptor == null || !descriptor.Enabled)
                {
                    return $"节点“{node.Name ?? node.Id}”引用的 Agent 或组不存在、未启用，或所属模块已关闭。";
                }
            }
        }

        return null;
    }

    public async Task MergeExistingSecretsAsync(
        NeuCharWorkflowGraph graph,
        string existingGraphJson,
        CancellationToken cancellationToken = default)
    {
        NeuCharWorkflowGraph existingGraph = null;
        if (!string.IsNullOrWhiteSpace(existingGraphJson))
        {
            try
            {
                existingGraph = ParseAndValidateGraph(existingGraphJson);
            }
            catch (InvalidOperationException)
            {
                // 历史异常图不参与密文复用，保存时必须重新输入敏感参数。
            }
        }

        foreach (var node in graph.Nodes.Where(z =>
                     z.Type.Equals("function", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var functionId = GetInt(node.Config, "functionId", 0);
            var function = await _functionEntityService.GetObjectAsync(z => z.Id == functionId)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”引用的 Function 不存在或已失效。");
            var secretNames = GetSecretParameterNames(function.UiSchemaJson);
            if (secretNames.Count == 0)
            {
                continue;
            }

            var existingNode = existingGraph?.Nodes.FirstOrDefault(z =>
                string.Equals(z.Id, node.Id, StringComparison.Ordinal) &&
                z.Type.Equals("function", StringComparison.OrdinalIgnoreCase) &&
                GetInt(z.Config, "functionId", 0) == functionId);
            var submittedJson = node.Config?["parameters"]?.ToJsonString() ?? "{}";
            var existingJson = existingNode?.Config?["parameters"]?.ToJsonString();
            var mergedJson = _parameterProtector.MergeWithExisting(
                submittedJson,
                existingJson,
                secretNames);
            node.Config["parameters"] = JsonNode.Parse(mergedJson);
        }
    }

    public async Task ProtectSecretsAsync(
        NeuCharWorkflowGraph graph,
        CancellationToken cancellationToken = default)
    {
        foreach (var node in graph.Nodes.Where(z =>
                     z.Type.Equals("function", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var functionId = GetInt(node.Config, "functionId", 0);
            var function = await _functionEntityService.GetObjectAsync(z => z.Id == functionId)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”引用的 Function 不存在或已失效。");
            var secretNames = GetSecretParameterNames(function.UiSchemaJson);
            if (secretNames.Count == 0)
            {
                continue;
            }

            var protectedJson = _parameterProtector.Protect(
                node.Config?["parameters"]?.ToJsonString() ?? "{}",
                secretNames);
            node.Config["parameters"] = JsonNode.Parse(protectedJson);
        }
    }

    public async Task<string> BuildEditableGraphJsonAsync(
        string storedGraphJson,
        CancellationToken cancellationToken = default)
    {
        var graph = ParseAndValidateGraph(storedGraphJson);
        foreach (var node in graph.Nodes.Where(z =>
                     z.Type.Equals("function", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var functionId = GetInt(node.Config, "functionId", 0);
            var function = await _functionEntityService.GetObjectAsync(z => z.Id == functionId)
                .ConfigureAwait(false);
            if (function == null)
            {
                node.Config["parameters"] = new JsonObject();
                continue;
            }

            var maskedJson = _parameterProtector.MaskForClient(
                node.Config?["parameters"]?.ToJsonString() ?? "{}",
                GetSecretParameterNames(function.UiSchemaJson));
            node.Config["parameters"] = JsonNode.Parse(maskedJson);
        }
        return JsonSerializer.Serialize(graph, JsonOptions);
    }

    public async Task<NeuCharWorkflowRunResult> RunAsync(
        NeuCharWorkflow workflow,
        string input,
        CancellationToken cancellationToken = default)
    {
        var graph = ParseAndValidateGraph(workflow.GraphJson);
        var trace = new List<string>();
        var correlationId = $"workflow-{workflow.Id}-{Guid.NewGuid():N}";
        var workflowLog = new NeuCharExecutionLog(
            "workflow",
            workflow.Id,
            null,
            null,
            workflow.Name,
            correlationId);
        await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);

        try
        {
            var nodes = graph.Nodes.ToDictionary(z => z.Id, StringComparer.Ordinal);
            var trigger = graph.Nodes.Single(z =>
                z.Type.EndsWith("trigger", StringComparison.OrdinalIgnoreCase));
            var queue = new Queue<(NeuCharWorkflowNode node, string value)>();
            queue.Enqueue((trigger, input ?? string.Empty));
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var finalOutput = input ?? string.Empty;
            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (node, currentValue) = queue.Dequeue();
                if (!visited.Add(node.Id))
                {
                    continue;
                }

                var execution = await ExecuteNodeAsync(node, currentValue, correlationId, cancellationToken)
                    .ConfigureAwait(false);
                trace.Add($"{node.Name ?? node.Type}: {(execution.success ? "OK" : "FAILED")}");
                if (!execution.success)
                {
                    throw new InvalidOperationException(execution.error);
                }
                finalOutput = execution.output ?? string.Empty;

                var outgoing = graph.Edges.Where(z => z.Source == node.Id);
                if (node.Type.Equals("condition", StringComparison.OrdinalIgnoreCase))
                {
                    var branch = execution.condition == true ? "true" : "false";
                    trace.Add($"{node.Name ?? node.Type}: branch={branch}");
                    outgoing = outgoing.Where(z =>
                        string.Equals(z.SourceHandle, branch, StringComparison.OrdinalIgnoreCase));
                }
                foreach (var edge in outgoing)
                {
                    queue.Enqueue((nodes[edge.Target], finalOutput));
                }
            }

            workflowLog.Complete(true, finalOutput, null);
            await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);
            return new NeuCharWorkflowRunResult(true, finalOutput, trace);
        }
        catch (Exception ex)
        {
            workflowLog.Complete(false, null, ex.ToString());
            await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);
            return new NeuCharWorkflowRunResult(false, null, trace, ex.Message);
        }
    }

    public async Task<IReadOnlyList<WorkflowObjectDescriptor>> GetWorkflowObjectsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<WorkflowObjectDescriptor>();
        foreach (var provider in _objectProviders.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.AddRange(await provider.GetObjectsAsync(cancellationToken).ConfigureAwait(false));
        }
        return result;
    }

    public static DateTime? CalculateNextRun(string triggerType, string triggerConfigJson, DateTime fromUtc)
    {
        if (!string.Equals(triggerType, "interval", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        try
        {
            var config = JsonNode.Parse(triggerConfigJson) as JsonObject;
            var seconds = config?["intervalSeconds"]?.GetValue<int>() ?? 300;
            return fromUtc.AddSeconds(Math.Clamp(seconds, 60, 31_536_000));
        }
        catch
        {
            return fromUtc.AddMinutes(5);
        }
    }

    private async Task<(bool success, string output, bool? condition, string error)> ExecuteNodeAsync(
        NeuCharWorkflowNode node,
        string input,
        string correlationId,
        CancellationToken cancellationToken)
    {
        switch (node.Type.ToLowerInvariant())
        {
            case "manual-trigger":
            case "interval-trigger":
                return (true, input, null, null);
            case "delay":
                var delaySeconds = Math.Clamp(GetInt(node.Config, "seconds", 1), 0, 30);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
                return (true, input, null, null);
            case "condition":
                var condition = EvaluateCondition(node.Config, input);
                return (true, input, condition, null);
            case "function":
                return await ExecuteFunctionNodeAsync(node, input, cancellationToken).ConfigureAwait(false);
            case "agent":
            case "agent-group":
                return await ExecuteWorkflowObjectNodeAsync(node, input, correlationId, cancellationToken)
                    .ConfigureAwait(false);
            case "end":
                return (true, input, null, null);
            default:
                return (false, null, null, $"不支持的节点类型：{node.Type}");
        }
    }

    private async Task<(bool success, string output, bool? condition, string error)> ExecuteFunctionNodeAsync(
        NeuCharWorkflowNode node,
        string input,
        CancellationToken cancellationToken)
    {
        var functionId = GetInt(node.Config, "functionId", 0);
        var function = await _functionEntityService.GetObjectAsync(z => z.Id == functionId).ConfigureAwait(false);
        if (function == null || !function.Visible)
        {
            return (false, null, null, "工作流引用的 NeuCharPivot Function 不存在或已失效。");
        }

        var parameterNode = node.Config?["parameters"]?.DeepClone();
        if (parameterNode == null)
        {
            try
            {
                parameterNode = JsonNode.Parse(function.DefaultParametersJson ?? "{}");
            }
            catch (JsonException)
            {
                parameterNode = new JsonObject();
            }
        }
        var parameterJson = ReplaceInputPlaceholders(parameterNode, input)?.ToJsonString() ?? "{}";
        parameterJson = _parameterProtector.Unprotect(parameterJson);
        var result = await _functionService.ExecuteAsync(
            function.ModuleUid,
            function.FunctionKey,
            parameterJson,
            cancellationToken).ConfigureAwait(false);
        return result.Success
            ? (true, result.Data?.ToString() ?? string.Empty, null, null)
            : (false, null, null, result.ErrorMessage);
    }

    private async Task<(bool success, string output, bool? condition, string error)> ExecuteWorkflowObjectNodeAsync(
        NeuCharWorkflowNode node,
        string input,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var providerId = GetString(node.Config, "providerId");
        var objectId = GetString(node.Config, "objectId");
        if (string.IsNullOrWhiteSpace(providerId) ||
            !_objectProviders.TryGetValue(providerId, out var provider))
        {
            return (false, null, null, "工作流对象 Provider 不可用，请确认对应模块已安装并开启。");
        }

        var objects = await provider.GetObjectsAsync(cancellationToken).ConfigureAwait(false);
        var descriptor = objects.FirstOrDefault(z => z.ObjectId == objectId);
        if (descriptor == null || !descriptor.Enabled)
        {
            return (false, null, null, "Agent 或组不存在、未启用，或所属模块已关闭。");
        }

        var prompt = ReplaceInputPlaceholder(GetString(node.Config, "prompt") ?? "{{input}}", input);
        var result = await provider.ExecuteAsync(
            new WorkflowObjectExecutionRequest(
                objectId,
                prompt,
                GetInt(node.Config, "aiModelId", 0),
                correlationId),
            cancellationToken).ConfigureAwait(false);
        return result.Success
            ? (true, result.Output ?? input, null, null)
            : (false, null, null, result.ErrorMessage);
    }

    private static bool EvaluateCondition(JsonObject config, string input)
    {
        var left = ReplaceInputPlaceholder(GetString(config, "left") ?? "{{input}}", input);
        var right = ReplaceInputPlaceholder(GetString(config, "right") ?? string.Empty, input);
        return (GetString(config, "operator") ?? "equals").ToLowerInvariant() switch
        {
            "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "not-equals" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "starts-with" => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
            "ends-with" => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static string ReplaceInputPlaceholder(string value, string input) =>
        (value ?? string.Empty).Replace("{{input}}", input ?? string.Empty, StringComparison.Ordinal);

    private static JsonNode ReplaceInputPlaceholders(JsonNode node, string input)
    {
        if (node is JsonObject jsonObject)
        {
            var result = new JsonObject();
            foreach (var property in jsonObject)
            {
                result[property.Key] = ReplaceInputPlaceholders(property.Value, input);
            }
            return result;
        }
        if (node is JsonArray jsonArray)
        {
            var result = new JsonArray();
            foreach (var item in jsonArray)
            {
                result.Add(ReplaceInputPlaceholders(item, input));
            }
            return result;
        }
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            return JsonValue.Create(ReplaceInputPlaceholder(text, input));
        }
        return node?.DeepClone();
    }

    private static int GetInt(JsonObject config, string name, int fallback)
    {
        try { return config?[name]?.GetValue<int>() ?? fallback; }
        catch { return fallback; }
    }

    private static string GetString(JsonObject config, string name)
    {
        try { return config?[name]?.GetValue<string>(); }
        catch { return null; }
    }

    private static IReadOnlyList<string> GetSecretParameterNames(string uiSchemaJson)
    {
        try
        {
            return (JsonSerializer.Deserialize<List<NeuCharPivotParameterSchema>>(
                        uiSchemaJson ?? "[]",
                        JsonOptions)
                    ?? new List<NeuCharPivotParameterSchema>())
                .Where(z => z.ParameterType == (int)Senparc.Ncf.XncfBase.ParameterType.Password)
                .Select(z => z.Name)
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static void EnsureAcyclic(NeuCharWorkflowGraph graph)
    {
        var indegree = graph.Nodes.ToDictionary(z => z.Id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in graph.Edges)
        {
            indegree[edge.Target]++;
        }
        var queue = new Queue<string>(indegree.Where(z => z.Value == 0).Select(z => z.Key));
        var visited = 0;
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            visited++;
            foreach (var edge in graph.Edges.Where(z => z.Source == id))
            {
                indegree[edge.Target]--;
                if (indegree[edge.Target] == 0)
                {
                    queue.Enqueue(edge.Target);
                }
            }
        }
        if (visited != graph.Nodes.Count)
        {
            throw new InvalidOperationException("当前版本不允许工作流形成循环，请使用 Loop Task 或间隔触发器。");
        }
    }

    private static string NormalizeSourceHandle(string sourceType, string sourceHandle)
    {
        if (sourceType.Equals("condition", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(sourceHandle, "false", StringComparison.OrdinalIgnoreCase)
                ? "false"
                : "true";
        }
        return "default";
    }

    private static void EnsureAllNodesReachable(NeuCharWorkflowGraph graph, string triggerId)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(triggerId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }
            foreach (var target in graph.Edges.Where(z => z.Source == current).Select(z => z.Target))
            {
                queue.Enqueue(target);
            }
        }

        if (visited.Count != graph.Nodes.Count)
        {
            var unreachable = graph.Nodes
                .Where(z => !visited.Contains(z.Id))
                .Select(z => z.Name ?? z.Id)
                .Take(5);
            throw new InvalidOperationException($"工作流包含未连接到触发器的节点：{string.Join("、", unreachable)}。");
        }
    }
}

public sealed class NeuCharWorkflowHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NeuCharWorkflowHostedService> _logger;

    public NeuCharWorkflowHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<NeuCharWorkflowHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var workflowService = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowService>();
                var engine = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowEngine>();
                var now = DateTime.UtcNow;
                var workflows = await workflowService.GetFullListAsync(
                    z => z.Enabled && z.TriggerType == "interval" && z.NextRunAt != null && z.NextRunAt <= now,
                    z => z.NextRunAt,
                    OrderingType.Ascending).ConfigureAwait(false);
                foreach (var workflow in workflows.Take(10))
                {
                    var nextRun = NeuCharWorkflowEngine.CalculateNextRun(
                        workflow.TriggerType,
                        workflow.TriggerConfigJson,
                        now);
                    workflow.MarkStarted(nextRun);
                    await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
                    var result = await engine.RunAsync(workflow, string.Empty, stoppingToken).ConfigureAwait(false);
                    workflow.MarkCompleted(result.Success, result.ErrorMessage);
                    await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描 NeuChar Workflow 失败，将在下一个周期重试。");
            }
        }
    }
}
