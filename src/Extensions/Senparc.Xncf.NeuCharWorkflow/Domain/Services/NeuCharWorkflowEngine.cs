/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowEngine.cs
    文件功能描述：新手友好的 NeuChar Workflow 声明式存储与执行引擎
----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

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

public sealed record NeuCharWorkflowProgress(
    string NodeId,
    string NodeName,
    string Status,
    string Message,
    string Output,
    DateTimeOffset Timestamp);

public sealed class NeuCharWorkflowEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "manual-trigger", "interval-trigger", "webhook-trigger", "function", "delay", "condition", "agent", "agent-group",
        "aggregate", "console", "end"
    };

    private sealed record ResolvedFunctionReference(
        NeuCharFunctionDescriptor Descriptor,
        string DefaultParametersJson);

    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharWorkflowFunctionService _functionService;
    private readonly NeuCharWorkflowExecutionLogService _logService;
    private readonly NeuCharWorkflowParameterProtector _parameterProtector;
    private readonly IReadOnlyDictionary<string, IWorkflowObjectProvider> _objectProviders;

    public NeuCharWorkflowEngine(
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowFunctionService functionService,
        NeuCharWorkflowExecutionLogService logService,
        NeuCharWorkflowParameterProtector parameterProtector,
        IEnumerable<IWorkflowObjectProvider> objectProviders)
    {
        _workflowService = workflowService;
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

        EnsureAcyclic(graph);
        foreach (var node in graph.Nodes)
        {
            var outgoing = graph.Edges.Where(z => z.Source == node.Id).ToList();
            var incoming = graph.Edges.Where(z => z.Target == node.Id).ToList();
            if (!node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase) &&
                !node.Type.Equals("function", StringComparison.OrdinalIgnoreCase) &&
                incoming.Count > 1)
            {
                throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”只允许一个上游连接；多对一目标请使用 Function 或聚合节点。");
            }
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
            var nodeBindingError = await ValidateNodeBindingsAsync(
                graph,
                node,
                cancellationToken).ConfigureAwait(false);
            if (nodeBindingError != null)
            {
                return nodeBindingError;
            }
            if (node.Type.Equals("function", StringComparison.OrdinalIgnoreCase))
            {
                var reference = await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false);
                if (reference == null)
                {
                    return $"节点“{node.Name ?? node.Id}”引用的 Function 不存在或已失效。";
                }
                if (!reference.Descriptor.ModuleAvailable)
                {
                    return $"节点“{node.Name ?? reference.Descriptor.Name}”所属模块未安装、未加载或未开启。";
                }

                var parameterJson = node.Config?["parameters"]?.ToJsonString() ?? reference.DefaultParametersJson;
                var validationError = NeuCharWorkflowFunctionService.ValidateRequiredParameters(
                    reference.Descriptor.Parameters,
                    parameterJson);
                if (validationError != null)
                {
                    return $"节点“{node.Name ?? reference.Descriptor.Name}”：{validationError}";
                }
                var bindingError = await ValidateFunctionBindingsAsync(
                    graph,
                    node,
                    reference.Descriptor,
                    cancellationToken).ConfigureAwait(false);
                if (bindingError != null)
                {
                    return bindingError;
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
            var reference = await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”引用的 Function 不存在或已失效。");
            var secretNames = reference.Descriptor.Parameters
                .Where(z => z.ParameterType == Senparc.Ncf.XncfBase.ParameterType.Password)
                .Select(z => z.Name)
                .ToArray();
            if (secretNames.Length == 0)
            {
                continue;
            }

            var existingNode = existingGraph?.Nodes.FirstOrDefault(z =>
                string.Equals(z.Id, node.Id, StringComparison.Ordinal) &&
                z.Type.Equals("function", StringComparison.OrdinalIgnoreCase) &&
                IsSameFunctionReference(z, node));
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
            var reference = await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”引用的 Function 不存在或已失效。");
            var secretNames = reference.Descriptor.Parameters
                .Where(z => z.ParameterType == Senparc.Ncf.XncfBase.ParameterType.Password)
                .Select(z => z.Name)
                .ToArray();
            if (secretNames.Length == 0)
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
            var reference = await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false);
            if (reference == null)
            {
                node.Config["parameters"] = new JsonObject();
                continue;
            }

            var maskedJson = _parameterProtector.MaskForClient(
                node.Config?["parameters"]?.ToJsonString() ?? "{}",
                reference.Descriptor.Parameters
                    .Where(z => z.ParameterType == Senparc.Ncf.XncfBase.ParameterType.Password)
                    .Select(z => z.Name));
            node.Config["parameters"] = JsonNode.Parse(maskedJson);
        }
        return JsonSerializer.Serialize(graph, JsonOptions);
    }

    public async Task<NeuCharWorkflowRunResult> RunAsync(
        WorkflowEntity workflow,
        string input,
        CancellationToken cancellationToken = default,
        Action<NeuCharWorkflowProgress> progress = null)
    {
        var graph = ParseAndValidateGraph(workflow.GraphJson);
        var trace = new List<string>();
        var correlationId = $"workflow-{workflow.Id}-{Guid.NewGuid():N}";
        var workflowLog = new NeuCharWorkflowExecutionLog(
            workflow.Id,
            workflow.Name,
            correlationId);
        await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);

        try
        {
            var nodes = graph.Nodes.ToDictionary(z => z.Id, StringComparer.Ordinal);
            var trigger = graph.Nodes.Single(z =>
                z.Type.EndsWith("trigger", StringComparison.OrdinalIgnoreCase));
            var queue = new Queue<(NeuCharWorkflowNode node, JsonNode value)>();
            var triggerInput = JsonValue.Create(input ?? string.Empty) as JsonNode;
            if (trigger.Type.Equals("webhook-trigger", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    triggerInput = JsonNode.Parse(input) ?? triggerInput;
                }
                catch (JsonException)
                {
                    // Webhook 输入始终由入口序列化为 JSON；对历史调用或非 JSON 请求保留原始文本。
                }
            }
            queue.Enqueue((trigger, triggerInput));
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var outputs = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
            JsonNode finalOutput = JsonValue.Create(input ?? string.Empty);
            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (node, currentValue) = queue.Dequeue();
                if (!visited.Add(node.Id))
                {
                    continue;
                }

                if (node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase))
                {
                    var aggregate = new JsonArray();
                    foreach (var sourceId in graph.Edges.Where(z => z.Target == node.Id).Select(z => z.Source))
                    {
                        if (outputs.TryGetValue(sourceId, out var sourceOutput))
                        {
                            aggregate.Add(sourceOutput?.DeepClone());
                        }
                    }
                    currentValue = aggregate;
                }

                Report(progress, node, "running", "开始执行节点。", null);
                var execution = await ExecuteNodeAsync(
                        node,
                        currentValue,
                        outputs,
                        correlationId,
                        cancellationToken)
                    .ConfigureAwait(false);
                trace.Add($"{node.Name ?? node.Type}: {(execution.success ? "OK" : "FAILED")}");
                if (!execution.success)
                {
                    Report(progress, node, "failed", execution.error, null);
                    throw new InvalidOperationException(execution.error);
                }
                finalOutput = execution.output ?? JsonNode.Parse("null");
                outputs[node.Id] = finalOutput?.DeepClone();
                var outputText = NodeToText(finalOutput);
                Report(progress, node, "success", "节点执行完成。", outputText);
                if (node.Type.Equals("console", StringComparison.OrdinalIgnoreCase))
                {
                    Report(progress, node, "console", "Console 输出", outputText);
                }

                var outgoing = graph.Edges.Where(z => z.Source == node.Id);
                if (node.Type.Equals("condition", StringComparison.OrdinalIgnoreCase))
                {
                    var branch = execution.condition == true ? "true" : "false";
                    trace.Add($"{node.Name ?? node.Type}: branch={branch}");
                    Report(progress, node, "branch", $"选择{(branch == "true" ? "真" : "假")}分支。", branch);
                    outgoing = outgoing.Where(z =>
                        string.Equals(z.SourceHandle, branch, StringComparison.OrdinalIgnoreCase));
                }
                foreach (var edge in outgoing)
                {
                    queue.Enqueue((nodes[edge.Target], finalOutput));
                }
            }

            var finalOutputText = NodeToText(finalOutput);
            workflowLog.Complete(true, finalOutputText, null);
            await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);
            return new NeuCharWorkflowRunResult(true, finalOutputText, trace);
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

    private async Task<(bool success, JsonNode output, bool? condition, string error)> ExecuteNodeAsync(
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        string correlationId,
        CancellationToken cancellationToken)
    {
        switch (node.Type.ToLowerInvariant())
        {
            case "manual-trigger":
            case "interval-trigger":
            case "webhook-trigger":
                return (true, input, null, null);
            case "delay":
                var delaySeconds = Math.Clamp(GetInt(node.Config, "seconds", 1), 0, 30);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
                return (true, input, null, null);
            case "condition":
                var condition = EvaluateCondition(node.Config, input, outputs);
                return (true, input, condition, null);
            case "function":
                return await ExecuteFunctionNodeAsync(node, input, outputs, cancellationToken).ConfigureAwait(false);
            case "agent":
            case "agent-group":
                return await ExecuteWorkflowObjectNodeAsync(node, input, outputs, correlationId, cancellationToken)
                    .ConfigureAwait(false);
            case "aggregate":
            case "console":
            case "end":
                return (true, input, null, null);
            default:
                return (false, null, null, $"不支持的节点类型：{node.Type}");
        }
    }

    private async Task<(bool success, JsonNode output, bool? condition, string error)> ExecuteFunctionNodeAsync(
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        CancellationToken cancellationToken)
    {
        var reference = await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false);
        if (reference == null)
        {
            return (false, null, null, "工作流引用的 NeuCharPivot Function 不存在或已失效。");
        }

        var parameterNode = node.Config?["parameters"]?.DeepClone();
        if (parameterNode == null)
        {
            try
            {
                parameterNode = JsonNode.Parse(reference.DefaultParametersJson ?? "{}");
            }
            catch (JsonException)
            {
                parameterNode = new JsonObject();
            }
        }
        parameterNode = JsonNode.Parse(_parameterProtector.Unprotect(parameterNode.ToJsonString())) ?? new JsonObject();
        var parameterJson = ResolveRuntimeValue(parameterNode, input, outputs)?.ToJsonString() ?? "{}";
        var result = await _functionService.ExecuteAsync(
            reference.Descriptor.ModuleUid,
            reference.Descriptor.FunctionKey,
            parameterJson,
            cancellationToken).ConfigureAwait(false);
        return result.Success
            ? (true, ToJsonNode(result.Data), null, null)
            : (false, null, null, result.ErrorMessage);
    }

    private async Task<(bool success, JsonNode output, bool? condition, string error)> ExecuteWorkflowObjectNodeAsync(
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
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

        var promptValue = ResolveRuntimeValue(
            node.Config?["prompt"]?.DeepClone() ?? JsonValue.Create("{{input}}"),
            input,
            outputs);
        var prompt = NodeToText(promptValue);
        var result = await provider.ExecuteAsync(
            new WorkflowObjectExecutionRequest(
                objectId,
                prompt,
                GetInt(node.Config, "aiModelId", 0),
                correlationId),
            cancellationToken).ConfigureAwait(false);
        return result.Success
            ? (true, JsonValue.Create(result.Output ?? NodeToText(input)), null, null)
            : (false, null, null, result.ErrorMessage);
    }

    private static bool EvaluateCondition(
        JsonObject config,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs)
    {
        var left = NodeToText(ResolveRuntimeValue(
            config?["left"]?.DeepClone() ?? JsonValue.Create("{{input}}"),
            input,
            outputs));
        var right = NodeToText(ResolveRuntimeValue(
            config?["right"]?.DeepClone() ?? JsonValue.Create(string.Empty),
            input,
            outputs));
        return (GetString(config, "operator") ?? "equals").ToLowerInvariant() switch
        {
            "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "not-equals" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "starts-with" => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
            "ends-with" => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static JsonNode ResolveRuntimeValue(
        JsonNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs)
    {
        if (node is JsonObject jsonObject)
        {
            if (jsonObject["$source"] is JsonObject binding)
            {
                return ResolveBinding(binding, outputs)?.DeepClone();
            }
            var result = new JsonObject();
            foreach (var property in jsonObject)
            {
                result[property.Key] = ResolveRuntimeValue(property.Value, input, outputs);
            }
            return result;
        }
        if (node is JsonArray jsonArray)
        {
            var result = new JsonArray();
            foreach (var item in jsonArray)
            {
                result.Add(ResolveRuntimeValue(item, input, outputs));
            }
            return result;
        }
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            return JsonValue.Create((text ?? string.Empty).Replace(
                "{{input}}",
                NodeToText(input),
                StringComparison.Ordinal));
        }
        return node?.DeepClone();
    }

    private static JsonNode ResolveBinding(
        JsonObject binding,
        IReadOnlyDictionary<string, JsonNode> outputs)
    {
        var nodeId = GetString(binding, "nodeId");
        var path = GetString(binding, "path") ?? "$";
        if (string.IsNullOrWhiteSpace(nodeId) || !outputs.TryGetValue(nodeId, out var value))
        {
            throw new InvalidOperationException($"上游节点“{nodeId}”尚未产生输出。");
        }

        value = value?.DeepClone();
        var collectionIndex = GetNullableInt(binding, "collectionIndex");
        var collectionIndexApplied = false;
        if (!string.Equals(path, "$", StringComparison.Ordinal))
        {
            foreach (var segment in path.TrimStart('$', '.').Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (collectionIndex.HasValue && !collectionIndexApplied && value is JsonArray)
                {
                    value = SelectArrayIndex(value, collectionIndex.Value, "上游列表");
                    collectionIndexApplied = true;
                }
                if (value is not JsonObject obj)
                {
                    throw new InvalidOperationException($"输出路径“{path}”无法从当前值读取。");
                }
                var key = obj.Select(z => z.Key).FirstOrDefault(z =>
                    string.Equals(z, segment, StringComparison.OrdinalIgnoreCase));
                if (key == null)
                {
                    throw new InvalidOperationException($"输出路径“{path}”不存在。");
                }
                value = obj[key]?.DeepClone();
            }
        }
        if (collectionIndex.HasValue && !collectionIndexApplied && value is JsonArray)
        {
            value = SelectArrayIndex(value, collectionIndex.Value, "上游列表");
        }
        var itemIndex = GetNullableInt(binding, "itemIndex");
        if (itemIndex.HasValue)
        {
            value = SelectArrayIndex(value, itemIndex.Value, "输出数组");
        }
        return value;
    }

    private static JsonNode SelectArrayIndex(JsonNode value, int index, string label)
    {
        if (value is not JsonArray array || index < 0 || index >= array.Count)
        {
            throw new InvalidOperationException($"{label}索引 {index} 超出范围。");
        }
        return array[index]?.DeepClone();
    }

    private static JsonNode ToJsonNode(object value)
    {
        if (value == null)
        {
            return JsonNode.Parse("null");
        }
        try
        {
            return JsonSerializer.SerializeToNode(value, value.GetType(), JsonOptions)
                   ?? JsonNode.Parse("null");
        }
        catch
        {
            return JsonValue.Create(value.ToString());
        }
    }

    private static string NodeToText(JsonNode value)
    {
        if (value == null)
        {
            return string.Empty;
        }
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            return text ?? string.Empty;
        }
        return value.ToJsonString(JsonOptions);
    }

    private static void Report(
        Action<NeuCharWorkflowProgress> progress,
        NeuCharWorkflowNode node,
        string status,
        string message,
        string output)
    {
        progress?.Invoke(new NeuCharWorkflowProgress(
            node.Id,
            node.Name ?? node.Type,
            status,
            message,
            output?.Length > 8_000 ? output[..8_000] : output,
            DateTimeOffset.UtcNow));
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

    private static int? GetNullableInt(JsonObject config, string name)
    {
        try { return config?[name]?.GetValue<int>(); }
        catch { return null; }
    }

    private async Task<ResolvedFunctionReference> ResolveFunctionReferenceAsync(
        NeuCharWorkflowNode node,
        CancellationToken cancellationToken)
    {
        var moduleUid = GetString(node.Config, "moduleUid");
        var functionKey = GetString(node.Config, "functionKey");
        if (string.IsNullOrWhiteSpace(moduleUid) || string.IsNullOrWhiteSpace(functionKey))
        {
            return null;
        }
        var catalog = await _functionService.GetCatalogAsync(moduleUid, true, cancellationToken)
            .ConfigureAwait(false);
        var descriptor = catalog.FirstOrDefault(z =>
            string.Equals(z.FunctionKey, functionKey, StringComparison.OrdinalIgnoreCase));
        return descriptor == null
            ? null
            : new ResolvedFunctionReference(descriptor, "{}");
    }

    private static bool IsSameFunctionReference(NeuCharWorkflowNode left, NeuCharWorkflowNode right)
    {
        return string.Equals(GetString(left.Config, "moduleUid"), GetString(right.Config, "moduleUid"), StringComparison.OrdinalIgnoreCase) &&
               string.Equals(GetString(left.Config, "functionKey"), GetString(right.Config, "functionKey"), StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ValidateFunctionBindingsAsync(
        NeuCharWorkflowGraph graph,
        NeuCharWorkflowNode targetNode,
        NeuCharFunctionDescriptor targetFunction,
        CancellationToken cancellationToken)
    {
        var parameters = targetNode.Config?["parameters"] as JsonObject;
        if (parameters == null)
        {
            return null;
        }
        foreach (var parameter in targetFunction.Parameters)
        {
            var parameterKey = parameters.Select(z => z.Key).FirstOrDefault(z =>
                string.Equals(z, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (parameterKey == null || parameters[parameterKey] is not JsonObject value ||
                value["$source"] is not JsonObject binding)
            {
                continue;
            }

            var sourceNodeId = GetString(binding, "nodeId");
            var sourceNode = graph.Nodes.FirstOrDefault(z => string.Equals(z.Id, sourceNodeId, StringComparison.Ordinal));
            if (sourceNode == null || !IsUpstream(graph, sourceNode.Id, targetNode.Id))
            {
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”引用的节点不是有效上游节点。";
            }

            var output = await GetNodeOutputDescriptorAsync(graph, sourceNode, cancellationToken).ConfigureAwait(false);
            var path = GetString(binding, "path") ?? "$";
            var field = output?.Fields?.FirstOrDefault(z => string.Equals(z.Path, path, StringComparison.Ordinal));
            if (field == null)
            {
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”引用的输出字段“{path}”已不存在。";
            }

            var collectionIndex = GetNullableInt(binding, "collectionIndex");
            var itemIndex = GetNullableInt(binding, "itemIndex");
            if (collectionIndex < 0 || itemIndex < 0)
            {
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”的数组索引不能小于 0。";
            }
            if (field.RequiresIndex && !collectionIndex.HasValue)
            {
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”需要先选择上游列表索引。";
            }
            if (itemIndex.HasValue && !field.IsArray)
            {
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”引用的输出不是数组，不能设置数组索引。";
            }

            var expected = GetParameterValueShape(parameter);
            var actualIsArray = field.IsArray && !itemIndex.HasValue;
            if (expected.isArray != actualIsArray)
            {
                return actualIsArray
                    ? $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”需要单值，但上游输出是数组；请选择数组索引。"
                    : $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”需要数组，但上游输出是单值。";
            }
            if (expected.typeName is not ("any" or "object") &&
                field.TypeName is not ("any" or "object") &&
                !string.Equals(expected.typeName, field.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”需要 {expected.typeName}，但上游输出为 {field.TypeName}。";
            }
        }
        return null;
    }

    private async Task<string> ValidateNodeBindingsAsync(
        NeuCharWorkflowGraph graph,
        NeuCharWorkflowNode targetNode,
        CancellationToken cancellationToken)
    {
        foreach (var (configPath, binding) in EnumerateBindings(targetNode.Config, "config"))
        {
            var sourceNodeId = GetString(binding, "nodeId");
            var sourceNode = graph.Nodes.FirstOrDefault(z =>
                string.Equals(z.Id, sourceNodeId, StringComparison.Ordinal));
            if (sourceNode == null || !IsUpstream(graph, sourceNode.Id, targetNode.Id))
            {
                return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 引用了无效或非上游节点。";
            }

            var output = await GetNodeOutputDescriptorAsync(graph, sourceNode, cancellationToken)
                .ConfigureAwait(false);
            var path = GetString(binding, "path") ?? "$";
            var field = output?.Fields?.FirstOrDefault(z =>
                string.Equals(z.Path, path, StringComparison.Ordinal));
            if (field == null)
            {
                return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 引用的输出字段“{path}”已不存在。";
            }

            var collectionIndex = GetNullableInt(binding, "collectionIndex");
            var itemIndex = GetNullableInt(binding, "itemIndex");
            if (collectionIndex < 0 || itemIndex < 0)
            {
                return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 数组索引不能小于 0。";
            }
            if (field.RequiresIndex && !collectionIndex.HasValue)
            {
                return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 需要先选择上游列表索引。";
            }
            if (itemIndex.HasValue && !field.IsArray)
            {
                return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 引用的输出不是数组，不能设置数组索引。";
            }
        }
        return null;
    }

    private static IEnumerable<(string Path, JsonObject Binding)> EnumerateBindings(
        JsonNode value,
        string path)
    {
        if (value is JsonObject obj)
        {
            if (obj["$source"] is JsonObject binding)
            {
                yield return (path, binding);
                yield break;
            }
            foreach (var property in obj)
            {
                foreach (var item in EnumerateBindings(property.Value, $"{path}.{property.Key}"))
                {
                    yield return item;
                }
            }
        }
        else if (value is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                foreach (var item in EnumerateBindings(array[index], $"{path}[{index}]"))
                {
                    yield return item;
                }
            }
        }
    }

    private async Task<NeuCharFunctionOutputDescriptor> GetNodeOutputDescriptorAsync(
        NeuCharWorkflowGraph graph,
        NeuCharWorkflowNode node,
        CancellationToken cancellationToken,
        ISet<string> visited = null)
    {
        visited ??= new HashSet<string>(StringComparer.Ordinal);
        if (!visited.Add(node.Id))
        {
            return null;
        }
        if (node.Type.Equals("function", StringComparison.OrdinalIgnoreCase))
        {
            return (await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false))?.Descriptor.Output;
        }
        if (node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase))
        {
            return new NeuCharFunctionOutputDescriptor(
                "any",
                "Object[]",
                true,
                "any",
                new[] { new NeuCharFunctionOutputFieldDescriptor("$", "聚合结果", "any", true, false) });
        }
        if (node.Type.Equals("webhook-trigger", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = node.Config?["webhookParameters"] as JsonArray;
            var fields = parameters?
                .OfType<JsonObject>()
                .Select(parameter =>
                {
                    var name = GetString(parameter, "name");
                    return string.IsNullOrWhiteSpace(name)
                        ? null
                        : new NeuCharFunctionOutputFieldDescriptor(
                            $"$.{name}",
                            name,
                            "any",
                            false,
                            false);
                })
                .Where(field => field != null)
                .ToArray();
            return new NeuCharFunctionOutputDescriptor(
                "object",
                "Webhook 输入",
                false,
                null,
                fields is { Length: > 0 }
                    ? fields
                    : new[] { new NeuCharFunctionOutputFieldDescriptor("$", "Webhook 输入", "object", false, false) });
        }
        if (node.Type is "delay" or "condition" or "console" or "end")
        {
            var incoming = graph.Edges.FirstOrDefault(z =>
                string.Equals(z.Target, node.Id, StringComparison.Ordinal));
            var source = incoming == null
                ? null
                : graph.Nodes.FirstOrDefault(z =>
                    string.Equals(z.Id, incoming.Source, StringComparison.Ordinal));
            if (source != null)
            {
                return await GetNodeOutputDescriptorAsync(
                    graph,
                    source,
                    cancellationToken,
                    visited).ConfigureAwait(false);
            }
        }
        var typeName = node.Type is "manual-trigger" or "interval-trigger" or "agent" or "agent-group"
            ? "string"
            : "any";
        return new NeuCharFunctionOutputDescriptor(
            typeName,
            typeName,
            false,
            null,
            new[] { new NeuCharFunctionOutputFieldDescriptor("$", "节点输出", typeName, false, false) });
    }

    private static (string typeName, bool isArray) GetParameterValueShape(
        Senparc.Ncf.XncfBase.FunctionParameterInfo parameter)
    {
        var systemType = parameter.SystemType ?? string.Empty;
        var isArray = parameter.ParameterType == Senparc.Ncf.XncfBase.ParameterType.CheckBoxList ||
                      systemType.Contains("[]", StringComparison.Ordinal) ||
                      systemType.Contains("List", StringComparison.OrdinalIgnoreCase) ||
                      systemType.Contains("Collection", StringComparison.OrdinalIgnoreCase);
        var normalized = systemType.ToLowerInvariant();
        var typeName = normalized.Contains("bool") ? "boolean"
            : normalized.Contains("date") || normalized.Contains("time") ? "datetime"
            : normalized.Contains("int") || normalized.Contains("decimal") || normalized.Contains("double") ||
              normalized.Contains("single") || normalized.Contains("float") || normalized.Contains("number") ? "number"
            : normalized.Contains("string") || normalized.Contains("char") || normalized.Contains("guid") ? "string"
            : "any";
        return (typeName, isArray);
    }

    private static bool IsUpstream(NeuCharWorkflowGraph graph, string sourceId, string targetId)
    {
        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        queue.Enqueue(sourceId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }
            foreach (var next in graph.Edges.Where(z => z.Source == current).Select(z => z.Target))
            {
                if (string.Equals(next, targetId, StringComparison.Ordinal))
                {
                    return true;
                }
                queue.Enqueue(next);
            }
        }
        return false;
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
                var moduleService = scope.ServiceProvider.GetRequiredService<XncfModuleService>();
                var module = await moduleService.GetObjectAsync(z => z.Uid == new Register().Uid).ConfigureAwait(false);
                if (module?.State != XncfModules_State.开放)
                {
                    continue;
                }
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
