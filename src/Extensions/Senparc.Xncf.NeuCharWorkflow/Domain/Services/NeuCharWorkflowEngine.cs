/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowEngine.cs
    文件功能描述：新手友好的 NeuChar Workflow 声明式存储与执行引擎


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

    修改标识：Senparc - 20260815
    修改描述：v0.2.0-preview2 增强工作流并行与运行控制

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Service;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public sealed class NeuCharWorkflowGraph
{
    public List<NeuCharWorkflowNode> Nodes { get; set; } = new();
    public List<NeuCharWorkflowEdge> Edges { get; set; } = new();
    /// <summary>工作流级变量。运行时通过 <c>vars.变量名</c> 在受限公式中读取。</summary>
    public List<NeuCharWorkflowVariable> Variables { get; set; } = new();
    /// <summary>仅用于设计器呈现，不参与工作流执行语义。</summary>
    public NeuCharWorkflowLayout Layout { get; set; } = new();
}

public sealed class NeuCharWorkflowVariable
{
    public string Name { get; set; }
    public JsonNode? Value { get; set; }
}

public sealed class NeuCharWorkflowLayout
{
    /// <summary>层级布局的阅读方向：vertical（默认）或 horizontal。</summary>
    public string Direction { get; set; } = "vertical";
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
    DateTimeOffset Timestamp,
    string? OutputSchema = null,
    string? Input = null);

public sealed class NeuCharWorkflowEngine
{
    private const int MaxStreamActivations = 500;
    private const int MaxLoopIterations = 100;
    private const int MaxWorkflowVariables = 30;
    private const string WorkflowVariablesOutputKey = "__workflow_variables__";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "manual-trigger", "interval-trigger", "webhook-trigger", "function", "delay", "condition", "agent", "agent-group", "a2a",
        "aggregate", "merge", "parallel", "loop", "loop-end", "sub-workflow", "code", "console", "neubell", "end"
    };

    private sealed record ResolvedFunctionReference(
        NeuCharFunctionDescriptor Descriptor,
        string DefaultParametersJson);

    /// <summary>
    /// Runtime state for one explicit loop body. The graph remains acyclic: each iteration
    /// travels from the loop node to its loop-end marker, and only the final marker completion
    /// releases the continuation after the loop.
    /// </summary>
    private sealed class LoopExecutionState
    {
        public LoopExecutionState(string loopNodeId, string boundaryNodeId, int iterationCount)
        {
            LoopNodeId = loopNodeId;
            BoundaryNodeId = boundaryNodeId;
            IterationCount = iterationCount;
        }

        public string LoopNodeId { get; }
        public string BoundaryNodeId { get; }
        public int IterationCount { get; }
        public int CompletedIterations { get; set; }
        public JsonNode LastOutput { get; set; }
    }

    private readonly NeuCharWorkflowService _workflowService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NeuCharWorkflowExecutionLogService _logService;
    private readonly NeuCharWorkflowParameterProtector _parameterProtector;
    private readonly IReadOnlyDictionary<string, IWorkflowObjectProvider> _objectProviders;
    private readonly NeuCharWorkflowNeuBellProvider? _neuBellProvider;
    private readonly INeuBellPublisher? _neuBellPublisher;

    public NeuCharWorkflowEngine(
        NeuCharWorkflowService workflowService,
        IServiceScopeFactory scopeFactory,
        NeuCharWorkflowExecutionLogService logService,
        NeuCharWorkflowParameterProtector parameterProtector,
        IEnumerable<IWorkflowObjectProvider> objectProviders,
        NeuCharWorkflowNeuBellProvider? neuBellProvider = null,
        INeuBellPublisher? neuBellPublisher = null)
    {
        _workflowService = workflowService;
        _scopeFactory = scopeFactory;
        _logService = logService;
        _parameterProtector = parameterProtector;
        _objectProviders = objectProviders
            .GroupBy(z => z.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(z => z.Key, z => z.First(), StringComparer.OrdinalIgnoreCase);
        _neuBellProvider = neuBellProvider;
        _neuBellPublisher = neuBellPublisher;
    }

    /// <summary>
    /// 解析并校验工作流图。保存草稿时可保留未连接节点，但执行前必须要求所有节点均可从触发器到达。
    /// </summary>
    public NeuCharWorkflowGraph ParseAndValidateGraph(string graphJson, bool requireAllNodesReachable = true)
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
        graph.Variables ??= new List<NeuCharWorkflowVariable>();
        graph.Layout ??= new NeuCharWorkflowLayout();
        graph.Layout.Direction = string.Equals(graph.Layout.Direction, "horizontal", StringComparison.OrdinalIgnoreCase)
            ? "horizontal"
            : "vertical";
        foreach (var node in graph.Nodes)
        {
            node.Config ??= new JsonObject();
            // 已保存的旧聚合节点直接输出数组。迁移到可配置输出时保留这一明确模板，
            // 以免升级后让既有工作流失效；新建节点会以空模板提示用户主动配置。
            if (node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase) &&
                !node.Config.ContainsKey("outputTemplate"))
            {
                node.Config["outputTemplate"] = "{{input}}";
            }
            // Console 节点早期固定打印其输入。补上显式模板后，既有工作流仍保留原有显示结果。
            if (node.Type.Equals("console", StringComparison.OrdinalIgnoreCase) &&
                !node.Config.ContainsKey("printTemplate"))
            {
                node.Config["printTemplate"] = "{{input}}";
            }
            // Loop 是有限次数的 For，不支持 while 或图上的回连。显式的 loop-end 节点
            // 用于标记循环体边界；没有该节点的旧图继续使用兼容的“重复全部下游”语义。
            if (node.Type.Equals("loop", StringComparison.OrdinalIgnoreCase) &&
                !node.Config.ContainsKey("count"))
            {
                node.Config["count"] = 3;
            }
            // Code is deliberately an assignment list rather than arbitrary JavaScript. This
            // keeps a workflow's state changes inspectable, bounded and safe to replay.
            if (node.Type.Equals("code", StringComparison.OrdinalIgnoreCase) &&
                !node.Config.ContainsKey("assignments"))
            {
                node.Config["assignments"] = new JsonArray();
            }
        }
        if (graph.Nodes.Count > 100 || graph.Edges.Count > 200)
        {
            throw new InvalidOperationException("单个工作流最多允许 100 个节点和 200 条连接。");
        }
        ValidateWorkflowVariables(graph.Variables);

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
        foreach (var loop in graph.Nodes.Where(node =>
                     node.Type.Equals("loop", StringComparison.OrdinalIgnoreCase)))
        {
            var loopBoundaryError = ValidateLoopBoundary(graph, loop);
            if (loopBoundaryError != null)
            {
                throw new InvalidOperationException(loopBoundaryError);
            }
        }

        foreach (var node in graph.Nodes)
        {
            var outgoing = graph.Edges.Where(z => z.Source == node.Id).ToList();
            var incoming = graph.Edges.Where(z => z.Target == node.Id).ToList();
            if (!node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase) &&
                !node.Type.Equals("merge", StringComparison.OrdinalIgnoreCase) &&
                !node.Type.Equals("function", StringComparison.OrdinalIgnoreCase) &&
                incoming.Count > 1)
            {
                throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”只允许一个上游连接；多对一目标请使用 Function、聚合或逐项合流节点。");
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
            else if (!node.Type.Equals("parallel", StringComparison.OrdinalIgnoreCase) &&
                     outgoing.Count > 1)
            {
                throw new InvalidOperationException($"节点“{node.Name ?? node.Id}”只能连接一个后续节点。");
            }
        }

        foreach (var aggregate in graph.Nodes.Where(node =>
                     node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase)))
        {
            if (graph.Nodes.Any(node =>
                    node.Type.Equals("merge", StringComparison.OrdinalIgnoreCase) &&
                    IsUpstream(graph, node.Id, aggregate.Id)))
            {
                throw new InvalidOperationException(
                    $"聚合节点“{aggregate.Name ?? aggregate.Id}”不能位于逐项合流节点之后；请在合流前完成聚合，或让逐项链路直接结束。");
            }
            if (graph.Nodes.Any(node =>
                    node.Type.Equals("loop", StringComparison.OrdinalIgnoreCase) &&
                    IsUpstream(graph, node.Id, aggregate.Id)))
            {
                throw new InvalidOperationException(
                    $"聚合节点“{aggregate.Name ?? aggregate.Id}”不能位于循环节点之后；请在循环前完成聚合，或改用逐项合流处理每一轮输入。");
            }
        }

        if (requireAllNodesReachable)
        {
            EnsureAllNodesReachable(graph, triggerNodes[0].Id);
        }
        return graph;
    }

    /// <summary>
    /// 返回无法从唯一触发器到达的草稿节点。调用方应先确保图结构已经通过校验。
    /// </summary>
    public IReadOnlyList<NeuCharWorkflowNode> GetDisconnectedNodes(NeuCharWorkflowGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var trigger = graph.Nodes.SingleOrDefault(z =>
            z.Type.EndsWith("trigger", StringComparison.OrdinalIgnoreCase));
        return trigger == null ? graph.Nodes : FindDisconnectedNodes(graph, trigger.Id);
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
            if (node.Type.Equals("loop", StringComparison.OrdinalIgnoreCase))
            {
                var loopCountError = ValidateLoopCountConfiguration(node.Config);
                if (loopCountError != null)
                {
                    return $"节点“{node.Name ?? node.Id}”：{loopCountError}";
                }
            }
            if (node.Type.Equals("code", StringComparison.OrdinalIgnoreCase))
            {
                var codeError = ValidateCodeAssignments(node.Config, graph.Variables);
                if (codeError != null)
                {
                    return $"节点“{node.Name ?? node.Id}”：{codeError}";
                }
            }
            var textTemplateError = ValidateNodeTextTemplates(node);
            if (textTemplateError != null)
            {
                return $"节点“{node.Name ?? node.Id}”：{textTemplateError}";
            }
            if (node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase))
            {
                var aggregateOutputError = ValidateAggregateOutputTemplate(node.Config);
                if (aggregateOutputError != null)
                {
                    return $"节点“{node.Name ?? node.Id}”：{aggregateOutputError}";
                }
            }
            else if (node.Type.Equals("console", StringComparison.OrdinalIgnoreCase) &&
                     GetRuntimeText(node.Config, "printTemplate")?.Length > 8_000)
            {
                return $"节点“{node.Name ?? node.Id}”的 Console 打印内容不能超过 8000 个字符。";
            }
            else if (node.Type.Equals("function", StringComparison.OrdinalIgnoreCase))
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
            else if (node.Type.Equals("neubell", StringComparison.OrdinalIgnoreCase))
            {
                var consumeMode = GetString(node.Config, "consumeMode");
                if (!string.IsNullOrWhiteSpace(consumeMode) &&
                    !string.Equals(consumeMode, NeuCharWorkflowNeuBellConsumption.None, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(consumeMode, NeuCharWorkflowNeuBellConsumption.Item, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(consumeMode, NeuCharWorkflowNeuBellConsumption.Provider, StringComparison.OrdinalIgnoreCase))
                {
                    return $"节点“{node.Name ?? node.Id}”的纽铃消费方式无效。";
                }
                if (GetRuntimeText(node.Config, "title")?.Length > 200 || GetRuntimeText(node.Config, "summary")?.Length > 4_000)
                {
                    return $"节点“{node.Name ?? node.Id}”的纽铃标题或内容超过允许长度。";
                }
            }
            else if (node.Type.Equals("agent", StringComparison.OrdinalIgnoreCase) ||
                     node.Type.Equals("agent-group", StringComparison.OrdinalIgnoreCase) ||
                     node.Type.Equals("a2a", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>校验子工作流归属、可用性及整个引用链，防止保存后出现直接或间接递归。</summary>
    public async Task<string?> ValidateSubWorkflowReferencesAsync(
        NeuCharWorkflowGraph graph,
        int currentWorkflowId,
        int adminUserId,
        bool requireEnabled,
        CancellationToken cancellationToken = default)
    {
        async Task<string?> VisitAsync(int workflowId, HashSet<int> path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (workflowId <= 0)
            {
                return "子工作流必须选择一个已保存的工作流。";
            }
            if (workflowId == currentWorkflowId || !path.Add(workflowId))
            {
                return "子工作流不能循环引用自身或上游工作流。";
            }
            if (path.Count > 8)
            {
                return "子工作流嵌套最多允许 8 层。";
            }

            var workflow = await _workflowService.GetObjectAsync(z =>
                z.Id == workflowId && z.AdminUserId == adminUserId).ConfigureAwait(false);
            if (workflow == null)
            {
                return $"子工作流 #{workflowId} 不存在，或不属于当前用户。";
            }
            if (requireEnabled && !workflow.Enabled)
            {
                return $"子工作流“{workflow.Name}”未启用，不能在运行中调用。";
            }

            NeuCharWorkflowGraph targetGraph;
            try
            {
                targetGraph = ParseAndValidateGraph(workflow.GraphJson, requireAllNodesReachable: false);
            }
            catch (InvalidOperationException ex)
            {
                return $"子工作流“{workflow.Name}”配置无效：{ex.Message}";
            }
            foreach (var childId in GetSubWorkflowIds(targetGraph))
            {
                var childError = await VisitAsync(childId, new HashSet<int>(path)).ConfigureAwait(false);
                if (childError != null)
                {
                    return childError;
                }
            }
            return null;
        }

        foreach (var workflowId in GetSubWorkflowIds(graph))
        {
            var error = await VisitAsync(workflowId, new HashSet<int>()).ConfigureAwait(false);
            if (error != null)
            {
                return error;
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
                existingGraph = ParseAndValidateGraph(existingGraphJson, requireAllNodesReachable: false);
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
        var graph = ParseAndValidateGraph(storedGraphJson, requireAllNodesReachable: false);
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
        Action<NeuCharWorkflowProgress> progress = null,
        string? runId = null,
        Func<string?>? cancellationResult = null,
        IReadOnlyCollection<int>? ancestorWorkflowIds = null)
    {
        var graph = ParseAndValidateGraph(workflow.GraphJson);
        var trace = new List<string>();
        var workflowPath = new HashSet<int>(ancestorWorkflowIds ?? Array.Empty<int>());
        if (!workflowPath.Add(workflow.Id))
        {
            const string message = "子工作流检测到循环引用，已拒绝执行。";
            return new NeuCharWorkflowRunResult(false, string.Empty, trace, message);
        }
        var replayEvents = new List<NeuCharWorkflowProgress>();
        var callerProgress = progress;
        var progressLock = new object();
        progress = item =>
        {
            lock (progressLock)
            {
                if (replayEvents.Count < 500)
                {
                    replayEvents.Add(item with
                    {
                        Message = LimitReplayText(item.Message, 2_000),
                        Output = LimitReplayText(item.Output, 2_000),
                        OutputSchema = LimitReplayText(item.OutputSchema, 20_000),
                        Input = LimitReplayText(item.Input, 20_000)
                    });
                }
                callerProgress?.Invoke(item);
            }
        };
        var correlationId = Guid.TryParse(runId, out var parsedRunId)
            ? $"workflow-{workflow.Id}-run-{parsedRunId:N}"
            : $"workflow-{workflow.Id}-legacy-{Guid.NewGuid():N}";
        var replayDefinitionJson = JsonSerializer.Serialize(new NeuCharWorkflowReplayDefinition(
            workflow.Name,
            workflow.Description,
            workflow.GraphJson,
            workflow.Enabled,
            workflow.TriggerType,
            workflow.TriggerConfigJson,
            workflow.AutoSaveMinutes,
            workflow.Revision), JsonOptions);
        var replaySnapshotHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(replayDefinitionJson)));
        var previousSnapshot = await _logService.GetLatestReplaySnapshotAsync(workflow.Id).ConfigureAwait(false);
        var workflowLog = new NeuCharWorkflowExecutionLog(
            workflow.Id,
            workflow.Name,
            correlationId);
        workflowLog.SetReplaySnapshot(
            replaySnapshotHash,
            string.Equals(previousSnapshot?.ReplaySnapshotHash, replaySnapshotHash, StringComparison.Ordinal)
                ? null
                : replayDefinitionJson);
        await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);

        try
        {
            var nodes = graph.Nodes.ToDictionary(z => z.Id, StringComparer.Ordinal);
            var trigger = graph.Nodes.Single(z =>
                z.Type.EndsWith("trigger", StringComparison.OrdinalIgnoreCase));
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

            // Schedule activations by completion rather than dependency waves. A parallel node
            // therefore lets each branch enqueue its own successor as soon as that branch
            // completes; a slow sibling no longer blocks the next node on a fast branch.
            // Aggregate nodes remain joins and are held until all currently active work has
            // settled. A merge node starts a stream: every input is carried through its
            // downstream chain as an independent activation. Stream activations are kept
            // serial for deterministic side effects and replay ordering.
            var ready = new List<(NeuCharWorkflowNode node, JsonNode value, bool isStream, LoopExecutionState loopState)>
            {
                (trigger, triggerInput, false, null)
            };
            var scheduled = new HashSet<string>(StringComparer.Ordinal) { trigger.Id };
            var waitingAggregateEdges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var outputs = new ConcurrentDictionary<string, JsonNode>(StringComparer.Ordinal);
            var streamActivationCount = 0;
            // Only Selection values are retained for bindings; do not retain unrelated input
            // parameters such as passwords in the runtime source cache.
            var functionSelectionInputs = new ConcurrentDictionary<string, JsonNode>(StringComparer.Ordinal);
            outputs[WorkflowVariablesOutputKey] = BuildWorkflowVariables(
                graph.Variables,
                triggerInput,
                outputs,
                functionSelectionInputs);
            JsonNode finalOutput = JsonValue.Create(input ?? string.Empty);
            var activeExecutions = new List<Task<(
                NeuCharWorkflowNode node,
                bool isStream,
                LoopExecutionState loopState,
                string replayInputText,
                (bool success, JsonNode output, bool? condition, string error) execution)>>();
            var streamActivationRunning = false;

            async Task<(
                NeuCharWorkflowNode node,
                bool isStream,
                LoopExecutionState loopState,
                string replayInputText,
                (bool success, JsonNode output, bool? condition, string error) execution)> ExecuteActivationAsync(
                (NeuCharWorkflowNode node, JsonNode value, bool isStream, LoopExecutionState loopState) item)
            {
                var replayInputText = await BuildReplayInputTextAsync(
                        item.node,
                        item.value,
                        outputs,
                        functionSelectionInputs,
                        cancellationToken)
                    .ConfigureAwait(false);
                Report(progress, item.node, "running", "开始执行节点。", null, input: replayInputText);
                var execution = await ExecuteNodeAsync(
                        workflow,
                        item.node,
                        item.value,
                        outputs,
                        functionSelectionInputs,
                        correlationId,
                        cancellationToken,
                        workflowPath)
                    .ConfigureAwait(false);
                return (item.node, item.isStream, item.loopState, replayInputText, execution);
            }

            void ProcessCompletedExecution((
                NeuCharWorkflowNode node,
                bool isStream,
                LoopExecutionState loopState,
                string replayInputText,
                (bool success, JsonNode output, bool? condition, string error) execution) completed)
            {
                var (node, isStream, loopState, replayInputText, execution) = completed;
                trace.Add($"{node.Name ?? node.Type}: {(execution.success ? "OK" : "FAILED")}");
                if (!execution.success)
                {
                    Report(progress, node, "failed", execution.error, null, input: replayInputText);
                    throw new InvalidOperationException(execution.error);
                }

                finalOutput = execution.output ?? JsonNode.Parse("null");
                outputs[node.Id] = finalOutput.DeepClone();
                var outputText = NodeToText(finalOutput);
                var outputSchema = NeuCharWorkflowObservedOutputSchemaBuilder.Build(node, finalOutput);
                Report(progress, node, "success", "节点执行完成。", outputText,
                    JsonSerializer.Serialize(outputSchema, JsonOptions),
                    replayInputText);
                if (node.Type.Equals("console", StringComparison.OrdinalIgnoreCase))
                {
                    var printOutput = ResolveConsolePrintOutput(
                        node.Config,
                        finalOutput,
                        outputs,
                        functionSelectionInputs);
                    Report(progress, node, "console", "Console 输出", NodeToText(printOutput));
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
                if (node.Type.Equals("loop", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryResolveLoopCount(
                            node.Config,
                            finalOutput,
                            outputs,
                            functionSelectionInputs,
                            out var loopCount,
                            out var loopError))
                    {
                        throw new InvalidOperationException(loopError);
                    }

                    trace.Add($"{node.Name ?? node.Type}: loop={loopCount}");
                    Report(progress, node, "loop", $"For 循环将按顺序执行下游 {loopCount} 次。", loopCount.ToString(CultureInfo.InvariantCulture));
                    var boundaryNodeId = FindLoopBoundaryNodeId(graph, node.Id);
                    var newLoopState = boundaryNodeId == null
                        ? null
                        : new LoopExecutionState(node.Id, boundaryNodeId, loopCount);
                    foreach (var edge in outgoing)
                    {
                        var target = nodes[edge.Target];
                        for (var iteration = 0; iteration < loopCount; iteration++)
                        {
                            // Loop/merge share one global guard, including nested loops and
                            // long stream chains. This makes a dynamic upstream count safe.
                            if (++streamActivationCount > MaxStreamActivations)
                            {
                                throw new InvalidOperationException($"循环或逐项合流产生的执行次数超过 {MaxStreamActivations} 次；请缩小次数、分支或拆分工作流。");
                            }
                            ready.Add((target, finalOutput.DeepClone(), true, newLoopState));
                        }
                    }
                    return;
                }

                if (loopState != null &&
                    node.Id.Equals(loopState.BoundaryNodeId, StringComparison.Ordinal))
                {
                    loopState.CompletedIterations++;
                    loopState.LastOutput = finalOutput.DeepClone();
                    trace.Add($"{node.Name ?? node.Type}: loop={loopState.CompletedIterations}/{loopState.IterationCount}");
                    Report(progress, node, "loop-end",
                        $"循环体第 {loopState.CompletedIterations} / {loopState.IterationCount} 轮完成。",
                        NodeToText(finalOutput));
                    if (loopState.CompletedIterations < loopState.IterationCount)
                    {
                        // Do not release the continuation after every iteration. The next
                        // iteration remains in the serial stream and the node after loop-end
                        // starts only once the complete body has run the requested number of times.
                        return;
                    }

                    finalOutput = loopState.LastOutput ?? JsonValue.Create(string.Empty);
                    isStream = false;
                    loopState = null;
                }
                foreach (var edge in outgoing)
                {
                    var target = nodes[edge.Target];
                    if (target.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!waitingAggregateEdges.TryGetValue(target.Id, out var activeEdgeIds))
                        {
                            activeEdgeIds = new HashSet<string>(StringComparer.Ordinal);
                            waitingAggregateEdges[target.Id] = activeEdgeIds;
                        }
                        activeEdgeIds.Add(edge.Id);
                    }
                    else if (target.Type.Equals("merge", StringComparison.OrdinalIgnoreCase))
                    {
                        // Each incoming edge activates a merge independently; no join or
                        // de-duplication is applied here.
                        if (++streamActivationCount > MaxStreamActivations)
                        {
                            throw new InvalidOperationException($"逐项合流产生的执行次数超过 {MaxStreamActivations} 次；请缩小分支或拆分工作流。");
                        }
                        ready.Add((target, finalOutput.DeepClone(), true, null));
                    }
                    else if (isStream || node.Type.Equals("merge", StringComparison.OrdinalIgnoreCase))
                    {
                        // Keep every item in a merge stream alive through all downstream
                        // nodes. The scheduler executes these stream activations serially.
                        if (++streamActivationCount > MaxStreamActivations)
                        {
                            throw new InvalidOperationException($"逐项合流产生的执行次数超过 {MaxStreamActivations} 次；请缩小分支或拆分工作流。");
                        }
                        ready.Add((target, finalOutput.DeepClone(), true, loopState));
                    }
                    else if (scheduled.Add(target.Id))
                    {
                        ready.Add((target, finalOutput.DeepClone(), false, null));
                    }
                }
            }

            try
            {
                while (ready.Count > 0 || activeExecutions.Count > 0 || waitingAggregateEdges.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ready.Count == 0 && activeExecutions.Count == 0 && waitingAggregateEdges.Count > 0)
                    {
                        // All non-aggregate work that can feed these joins has completed. The
                        // graph is acyclic, so only now is every selected upstream input known.
                        ready.AddRange(waitingAggregateEdges
                            .Select(pair => (node: nodes[pair.Key], activeEdgeIds: pair.Value))
                            .OrderBy(item => graph.Nodes.FindIndex(node => node.Id == item.node.Id))
                            .Select(item => (item.node, value: (JsonNode)BuildAggregateInput(
                                graph, item.node, item.activeEdgeIds, outputs), isStream: false, loopState: (LoopExecutionState)null)));
                        waitingAggregateEdges.Clear();
                    }

                    if (ready.Count > 0)
                    {
                        var codeIndex = ready.FindIndex(item =>
                            item.node.Type.Equals("code", StringComparison.OrdinalIgnoreCase));
                        if (codeIndex >= 0 && activeExecutions.Count == 0)
                        {
                            // Code mutates run-local variables. Keep it as an exclusive state
                            // barrier, while ordinary branches continue independently otherwise.
                            var codeActivation = ready[codeIndex];
                            ready.RemoveAt(codeIndex);
                            if (codeActivation.isStream)
                            {
                                streamActivationRunning = true;
                            }
                            var completedCode = await ExecuteActivationAsync(codeActivation).ConfigureAwait(false);
                            if (completedCode.isStream)
                            {
                                streamActivationRunning = false;
                            }
                            ProcessCompletedExecution(completedCode);
                            continue;
                        }

                        // Start every ready non-code activation. If a code barrier is waiting while
                        // other branches are still running, those branches may continue and finish;
                        // the code node starts only after they have drained.
                        for (var index = ready.Count - 1; index >= 0; index--)
                        {
                            var activation = ready[index];
                            if (activation.node.Type.Equals("code", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            if (activation.isStream && streamActivationRunning)
                            {
                                continue;
                            }
                            ready.RemoveAt(index);
                            if (activation.isStream)
                            {
                                streamActivationRunning = true;
                            }
                            activeExecutions.Add(ExecuteActivationAsync(activation));
                        }
                    }

                    if (activeExecutions.Count == 0)
                    {
                        continue;
                    }

                    var completedTask = await Task.WhenAny(activeExecutions).ConfigureAwait(false);
                    activeExecutions.Remove(completedTask);
                    var completed = await completedTask.ConfigureAwait(false);
                    if (completed.isStream)
                    {
                        streamActivationRunning = false;
                    }
                    ProcessCompletedExecution(completed);
                }
            }
            finally
            {
                // Do not dispose the run/child scope while a branch still owns a provider,
                // function, Agent or A2A scope. All of them receive the same cancellation
                // token, so this is cooperative cleanup for stop/failure paths.
                if (activeExecutions.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(activeExecutions).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // The outer RunAsync cancellation handler records the final result.
                    }
                    catch (Exception)
                    {
                        // Preserve the original execution failure.
                    }
                    activeExecutions.Clear();
                }
            }

            var finalOutputText = NodeToText(finalOutput);
            workflowLog.Complete(true, finalOutputText, null, JsonSerializer.Serialize(replayEvents, JsonOptions));
            await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);
            return new NeuCharWorkflowRunResult(true, finalOutputText, trace);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = cancellationResult?.Invoke();
            if (string.IsNullOrWhiteSpace(message))
            {
                message = ex.Message;
            }
            workflowLog.Complete(false, message, message, JsonSerializer.Serialize(replayEvents, JsonOptions));
            await _logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);
            return new NeuCharWorkflowRunResult(false, message, trace, message);
        }
        catch (Exception ex)
        {
            workflowLog.Complete(false, null, ex.ToString(), JsonSerializer.Serialize(replayEvents, JsonOptions));
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

    private async Task<string> BuildReplayInputTextAsync(
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs,
        CancellationToken cancellationToken)
    {
        var fallback = NodeToText(input);
        if (!node.Type.Equals("function", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }
        try
        {
            var reference = await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false);
            if (reference == null)
            {
                return fallback;
            }
            var parameterNode = node.Config?["parameters"]?.DeepClone();
            if (parameterNode == null)
            {
                parameterNode = JsonNode.Parse(reference.DefaultParametersJson ?? "{}");
            }
            parameterNode = JsonNode.Parse(_parameterProtector.Unprotect(parameterNode.ToJsonString()))
                ?? new JsonObject();
            var resolvedParameters = ResolveRuntimeValue(
                parameterNode,
                input,
                outputs,
                functionSelectionInputs) ?? new JsonObject();
            var maskedJson = _parameterProtector.MaskForClient(
                resolvedParameters.ToJsonString(),
                reference.Descriptor.Parameters
                    .Where(parameter => parameter.ParameterType == Senparc.Ncf.XncfBase.ParameterType.Password)
                    .Select(parameter => parameter.Name));
            return NodeToText(JsonNode.Parse(maskedJson));
        }
        catch
        {
            // Capturing replay detail must not turn a successful run into a failed one.
            return fallback;
        }
    }

    private static JsonArray BuildAggregateInput(
        NeuCharWorkflowGraph graph,
        NeuCharWorkflowNode aggregateNode,
        ISet<string> activeEdgeIds,
        IReadOnlyDictionary<string, JsonNode> outputs)
    {
        var aggregate = new JsonArray();
        foreach (var edge in graph.Edges.Where(edge =>
                     edge.Target == aggregateNode.Id && activeEdgeIds.Contains(edge.Id)))
        {
            if (outputs.TryGetValue(edge.Source, out var sourceOutput))
            {
                aggregate.Add(sourceOutput?.DeepClone());
            }
        }
        return aggregate;
    }

    private async Task<(bool success, JsonNode output, bool? condition, string error)> ExecuteNodeAsync(
        WorkflowEntity workflow,
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        ConcurrentDictionary<string, JsonNode> functionSelectionInputs,
        string correlationId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<int> workflowPath)
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
                var condition = EvaluateCondition(node.Config, input, outputs, functionSelectionInputs);
                return (true, input, condition, null);
            case "loop":
                return TryResolveLoopCount(
                    node.Config,
                    input,
                    outputs,
                    functionSelectionInputs,
                    out _,
                    out var loopError)
                    ? (true, input, null, null)
                    : (false, null, null, loopError);
            case "function":
                return await ExecuteFunctionNodeAsync(node, input, outputs, functionSelectionInputs, cancellationToken).ConfigureAwait(false);
            case "agent":
            case "agent-group":
            case "a2a":
                return await ExecuteWorkflowObjectNodeAsync(node, input, outputs, functionSelectionInputs, correlationId, cancellationToken)
                    .ConfigureAwait(false);
            case "sub-workflow":
                return await ExecuteSubWorkflowNodeAsync(
                        workflow,
                        node,
                        input,
                        outputs,
                        functionSelectionInputs,
                        cancellationToken,
                        workflowPath)
                    .ConfigureAwait(false);
            case "code":
                return ExecuteCodeNode(node, input, outputs, functionSelectionInputs);
            case "neubell":
                return await ExecuteNeuBellNodeAsync(
                        workflow,
                        node,
                        input,
                        outputs,
                        functionSelectionInputs,
                        correlationId,
                        cancellationToken)
                    .ConfigureAwait(false);
            case "aggregate":
                return (true, ResolveAggregateOutput(node.Config, input, outputs, functionSelectionInputs), null, null);
            case "merge":
            case "parallel":
            case "console":
            case "loop-end":
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
        ConcurrentDictionary<string, JsonNode> functionSelectionInputs,
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
        var resolvedParameters = ResolveRuntimeValue(parameterNode, input, outputs, functionSelectionInputs) ?? new JsonObject();
        functionSelectionInputs[node.Id] = ExtractFunctionSelectionValues(
            resolvedParameters,
            reference.Descriptor.Parameters);
        var parameterJson = resolvedParameters.ToJsonString();
        // Function services and their repositories are scoped. Independent workflow branches may execute
        // independent Function nodes concurrently, so never reuse the engine's request scope
        // for the actual invocation; each node gets its own DbContext graph.
        var result = await ExecuteInFunctionScopeAsync(functionService => functionService.ExecuteAsync(
                reference.Descriptor.ModuleUid,
                reference.Descriptor.FunctionKey,
                parameterJson,
                cancellationToken))
            .ConfigureAwait(false);
        return result.Success
            ? (true, ToJsonNode(result.Data), null, null)
            : (false, null, null, result.ErrorMessage);
    }

    private async Task<(bool success, JsonNode output, bool? condition, string error)> ExecuteNeuBellNodeAsync(
        WorkflowEntity workflow,
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (_neuBellProvider == null)
        {
            return (false, null, null, "NeuBell 提醒服务当前不可用，请确认 NeuChar Workflow 模块已正确加载。");
        }

        var title = NodeToText(ResolveRuntimeValue(
            node.Config?["title"]?.DeepClone() ?? JsonValue.Create("Workflow 提醒"),
            input,
            outputs,
            functionSelectionInputs));
        var summary = NodeToText(ResolveRuntimeValue(
            node.Config?["summary"]?.DeepClone() ?? JsonValue.Create("{{input}}"),
            input,
            outputs,
            functionSelectionInputs));
        var consumeMode = NeuCharWorkflowNeuBellConsumption.Normalize(GetString(node.Config, "consumeMode"));
        var runId = TryGetWorkflowRunId(correlationId);
        var notificationId = _neuBellProvider.Send(
            workflow.AdminUserId,
            workflow.Id,
            workflow.Name,
            runId,
            node.Id,
            title,
            summary,
            consumeMode);
        if (_neuBellPublisher != null)
        {
            await _neuBellPublisher.NotifyChangedAsync(NeuCharWorkflowNeuBellProvider.ProviderIdValue, cancellationToken)
                .ConfigureAwait(false);
        }

        return (true, new JsonObject
        {
            ["notificationId"] = notificationId,
            ["consumeMode"] = consumeMode,
            ["workflowId"] = workflow.Id
        }, null, null);
    }

    private async Task<(bool success, JsonNode output, bool? condition, string error)> ExecuteWorkflowObjectNodeAsync(
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs,
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

        // Providers are scoped because their services/repositories hold scoped EF Core
        // DbContexts. A parallel node must not reuse the provider instance captured by the
        // engine's request scope; resolve and execute the provider inside a private scope for
        // this one node instead.
        using var scope = _scopeFactory.CreateScope();
        var executionProvider = scope.ServiceProvider
            .GetServices<IWorkflowObjectProvider>()
            .FirstOrDefault(z => string.Equals(z.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
        if (executionProvider == null)
        {
            return (false, null, null, "工作流对象 Provider 不可用，请确认对应模块已安装并开启。");
        }

        var objects = await executionProvider.GetObjectsAsync(cancellationToken).ConfigureAwait(false);
        var descriptor = objects.FirstOrDefault(z => z.ObjectId == objectId);
        if (descriptor == null || !descriptor.Enabled)
        {
            return (false, null, null, "Agent 或组不存在、未启用，或所属模块已关闭。");
        }

        var promptValue = ResolveRuntimeValue(
            node.Config?["prompt"]?.DeepClone() ?? JsonValue.Create("{{input}}"),
            input,
            outputs,
            functionSelectionInputs);
        var prompt = NodeToText(promptValue);
        var result = await executionProvider.ExecuteAsync(
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

    private async Task<(bool success, JsonNode output, bool? condition, string error)> ExecuteSubWorkflowNodeAsync(
        WorkflowEntity parentWorkflow,
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        ConcurrentDictionary<string, JsonNode> functionSelectionInputs,
        CancellationToken cancellationToken,
        IReadOnlyCollection<int> workflowPath)
    {
        var workflowId = GetInt(node.Config, "workflowId", 0);
        if (workflowId <= 0)
        {
            return (false, null, null, "调用工作流节点尚未选择目标工作流。");
        }
        if (workflowPath.Contains(workflowId))
        {
            return (false, null, null, "调用工作流会形成循环引用，已拒绝执行。");
        }

        using var scope = _scopeFactory.CreateScope();
        var workflowService = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowService>();
        var childEngine = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowEngine>();
        var childWorkflow = await workflowService.GetObjectAsync(item =>
                item.Id == workflowId && item.AdminUserId == parentWorkflow.AdminUserId)
            .ConfigureAwait(false);
        if (childWorkflow == null)
        {
            return (false, null, null, "目标工作流不存在，或不属于当前用户。");
        }
        if (!childWorkflow.Enabled)
        {
            return (false, null, null, $"目标工作流“{childWorkflow.Name}”未启用。");
        }

        var promptValue = ResolveRuntimeValue(
            node.Config?["prompt"]?.DeepClone() ?? JsonValue.Create("{{input}}"),
            input,
            outputs,
            functionSelectionInputs);
        var result = await childEngine.RunAsync(
                childWorkflow,
                NodeToText(promptValue),
                cancellationToken,
                progress: null,
                runId: null,
                cancellationResult: null,
                ancestorWorkflowIds: workflowPath)
            .ConfigureAwait(false);
        // A child RunAsync records its own cancellation result for replay. The parent still
        // needs the cancellation exception so sibling branches and external resources receive
        // the same stop signal instead of treating cancellation as an ordinary node failure.
        cancellationToken.ThrowIfCancellationRequested();
        return result.Success
            ? (true, JsonValue.Create(result.Output ?? string.Empty), null, null)
            : (false, null, null, result.ErrorMessage ?? "子工作流执行失败。");
    }

    private static (bool success, JsonNode output, bool? condition, string error) ExecuteCodeNode(
        NeuCharWorkflowNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        if (outputs is not IDictionary<string, JsonNode> mutableOutputs ||
            !mutableOutputs.TryGetValue(WorkflowVariablesOutputKey, out var storedVariables) ||
            storedVariables is not JsonObject variables)
        {
            return (false, null, null, "工作流变量运行时上下文不可用。");
        }
        if (node.Config?["assignments"] is not JsonArray assignments)
        {
            return (false, null, null, "安全代码节点的赋值配置无效。");
        }

        foreach (var assignment in assignments.OfType<JsonObject>())
        {
            var name = GetString(assignment, "name")?.Trim();
            if (string.IsNullOrWhiteSpace(name) || !variables.ContainsKey(name))
            {
                return (false, null, null, $"安全代码节点不能为未定义的工作流变量“{name ?? "(空)"}”赋值。");
            }
            variables[name] = ResolveRuntimeValue(
                assignment["value"]?.DeepClone() ?? JsonValue.Create(string.Empty),
                input,
                outputs,
                functionSelectionInputs);
        }
        return (true, input, null, null);
    }

    private static bool TryResolveLoopCount(
        JsonObject config,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs,
        out int count,
        out string error)
    {
        count = 0;
        error = null;
        try
        {
            var value = ResolveRuntimeValue(
                config?["count"]?.DeepClone() ?? JsonValue.Create(3),
                input,
                outputs,
                functionSelectionInputs);
            if (TryReadLoopCount(value, out count))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            error = $"循环次数引用无法解析：{ex.Message}";
            return false;
        }

        error = $"循环次数必须为 1 到 {MaxLoopIterations} 的整数；上游引用在运行时也必须提供此范围内的单个数字。";
        return false;
    }

    private static bool TryReadLoopCount(JsonNode value, out int count)
    {
        count = 0;
        var text = NodeToText(value).Trim();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) &&
               count is >= 1 and <= MaxLoopIterations;
    }

    private static bool EvaluateCondition(
        JsonObject config,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        var left = NodeToText(ResolveRuntimeValue(
            config?["left"]?.DeepClone() ?? JsonValue.Create("{{input}}"),
            input,
            outputs,
            functionSelectionInputs));
        var right = NodeToText(ResolveRuntimeValue(
            config?["right"]?.DeepClone() ?? JsonValue.Create(string.Empty),
            input,
            outputs,
            functionSelectionInputs));
        return (GetString(config, "operator") ?? "equals").ToLowerInvariant() switch
        {
            "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "not-equals" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "starts-with" => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
            "ends-with" => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static JsonNode ResolveAggregateOutput(
        JsonObject config,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        var templateValue = config?["outputTemplate"]?.DeepClone();
        var template = GetRuntimeText(config, "outputTemplate")?.Trim();
        // {{input}} is the explicit compatibility/template form for preserving the raw
        // aggregate array instead of serializing it to text.
        if (string.Equals(template, "{{input}}", StringComparison.OrdinalIgnoreCase))
        {
            return input?.DeepClone();
        }

        // Reuse the restricted template renderer so aggregate output has the
        // same safe {{= expression }} semantics as text parameters. A plain
        // {{input}} was handled above to keep the aggregate value typed as an
        // array; mixed text always has a textual result.
        var resolved = templateValue is JsonObject { } templateObject && templateObject["$template"] is JsonObject
            ? ResolveRuntimeValue(templateObject, input, outputs, functionSelectionInputs)
            : ResolveTemplate(
                new JsonObject { ["text"] = template ?? string.Empty },
                input,
                outputs,
                functionSelectionInputs);
        if (NodeToText(resolved).Length > 8_000)
        {
            throw new InvalidOperationException("聚合节点输出内容超过 8000 个字符。");
        }
        return resolved;
    }

    /// <summary>
    /// 解析 Console 的展示内容。该值只供执行 Console 使用，Console 节点本身仍透传原始输入，
    /// 因而不会改变下游节点看到的数据类型或内容。
    /// </summary>
    private static JsonNode ResolveConsolePrintOutput(
        JsonObject config,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        var templateValue = config?["printTemplate"]?.DeepClone() ?? JsonValue.Create("{{input}}");
        var template = GetRuntimeText(config, "printTemplate")?.Trim();
        if (string.IsNullOrWhiteSpace(template) ||
            string.Equals(template, "{{input}}", StringComparison.OrdinalIgnoreCase))
        {
            return input?.DeepClone();
        }

        return ResolveRuntimeValue(templateValue, input, outputs, functionSelectionInputs);
    }

    private static JsonNode ResolveRuntimeValue(
        JsonNode node,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        if (node is JsonObject jsonObject)
        {
            if (jsonObject["$source"] is JsonObject binding)
            {
                return ResolveBinding(binding, outputs, functionSelectionInputs)?.DeepClone();
            }
            if (jsonObject["$template"] is JsonObject template)
            {
                return ResolveTemplate(template, input, outputs, functionSelectionInputs);
            }
            var result = new JsonObject();
            foreach (var property in jsonObject)
            {
                result[property.Key] = ResolveRuntimeValue(property.Value, input, outputs, functionSelectionInputs);
            }
            return result;
        }
        if (node is JsonArray jsonArray)
        {
            var result = new JsonArray();
            foreach (var item in jsonArray)
            {
                result.Add(ResolveRuntimeValue(item, input, outputs, functionSelectionInputs));
            }
            return result;
        }
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            if (text.Contains("{{=", StringComparison.Ordinal))
            {
                return ResolveTemplate(
                    new JsonObject { ["text"] = text },
                    input,
                    outputs,
                    functionSelectionInputs);
            }
            return JsonValue.Create((text ?? string.Empty).Replace(
                    "{{input}}",
                    NodeToText(input),
                    StringComparison.Ordinal));
        }
        return node?.DeepClone();
    }

    private static JsonObject BuildWorkflowVariables(
        IEnumerable<NeuCharWorkflowVariable> definitions,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        var values = new JsonObject();
        // Place the object into the runtime map before evaluating values so a later declaration
        // may deliberately refer to an earlier one through vars.name, without exposing host data.
        if (outputs is IDictionary<string, JsonNode> mutableOutputs)
        {
            mutableOutputs[WorkflowVariablesOutputKey] = values;
        }
        foreach (var definition in definitions ?? Enumerable.Empty<NeuCharWorkflowVariable>())
        {
            var name = definition.Name.Trim();
            values[name] = ResolveRuntimeValue(
                definition.Value?.DeepClone() ?? JsonValue.Create(string.Empty),
                input,
                outputs,
                functionSelectionInputs);
        }
        return values;
    }

    /// <summary>
    /// Renders a textual Function parameter containing one or more explicit binding tokens.
    /// The persisted contract deliberately separates the editable text from its source records:
    /// { "$template": { "text": "prefix {{value_1}}", "bindings": [{ "token": "value_1", "source": { ... } }] } }.
    /// It keeps user text literal and makes Function/module upgrades validate every referenced source.
    /// </summary>
    private static JsonNode ResolveTemplate(
        JsonObject template,
        JsonNode input,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        var text = GetString(template, "text") ?? string.Empty;
        var variables = new Dictionary<string, JsonNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["input"] = input
        };
        if (outputs.TryGetValue(WorkflowVariablesOutputKey, out var workflowVariables) &&
            workflowVariables is JsonObject)
        {
            variables["vars"] = workflowVariables;
        }
        if (template["bindings"] is JsonArray bindings)
        {
            foreach (var (index, item) in bindings.Select((item, index) => (index, item)))
            {
                if (item is not JsonObject entry)
                {
                    throw new InvalidOperationException($"文本模板第 {index + 1} 个变量格式无效。");
                }
                var token = GetString(entry, "token");
                if (!IsTemplateToken(token) || entry["source"] is not JsonObject source)
                {
                    throw new InvalidOperationException($"文本模板第 {index + 1} 个变量缺少有效名称或绑定来源。");
                }
                variables[token] = ResolveBinding(source, outputs, functionSelectionInputs);
            }
        }

        // A template consisting of one formula is a value expression, not text interpolation.
        // Preserve its JSON type so a Function request can receive an Int32, Boolean, Decimal,
        // etc. rather than the textual rendering of that value. Any surrounding text keeps the
        // existing text-template behavior for backwards compatibility.
        if (TryGetWholeTemplateExpression(text, out var wholeExpression))
        {
            if (!NeuCharWorkflowExpressionEngine.TryEvaluate(wholeExpression, variables, out var value, out var error))
            {
                throw new InvalidOperationException($"文本表达式无效：{error}");
            }
            if (NodeToText(value).Length > 8_000)
            {
                throw new InvalidOperationException("文本表达式的结果超过 8000 个字符。");
            }
            return value?.DeepClone();
        }

        text = RenderTemplateExpressions(text, variables);
        foreach (var (token, value) in variables)
        {
            text = text.Replace($"{{{{{token}}}}}", NodeToText(value), StringComparison.Ordinal);
        }
        return JsonValue.Create(text);
    }

    private static bool TryGetWholeTemplateExpression(string text, out string expression)
    {
        expression = null;
        var trimmed = (text ?? string.Empty).Trim();
        if (!trimmed.StartsWith("{{=", StringComparison.Ordinal))
        {
            return false;
        }

        var end = FindTemplateExpressionEnd(trimmed, 3);
        if (end < 0 || end + 2 != trimmed.Length)
        {
            return false;
        }

        expression = trimmed[3..end].Trim();
        return expression.Length > 0;
    }

    private static string RenderTemplateExpressions(string text, IReadOnlyDictionary<string, JsonNode> variables)
    {
        var position = 0;
        var count = 0;
        var rendered = new StringBuilder();
        while (true)
        {
            var start = text.IndexOf("{{=", position, StringComparison.Ordinal);
            if (start < 0)
            {
                return rendered.Append(text, position, text.Length - position).ToString();
            }
            rendered.Append(text, position, start - position);
            var end = FindTemplateExpressionEnd(text, start + 3);
            if (end < 0)
            {
                throw new InvalidOperationException("文本表达式缺少“}}”。");
            }
            var expression = text[(start + 3)..end].Trim();
            if (++count > 32)
            {
                throw new InvalidOperationException("每个参数最多包含 32 个文本表达式。");
            }
            if (!NeuCharWorkflowExpressionEngine.TryEvaluate(expression, variables, out var value, out var error))
            {
                throw new InvalidOperationException($"文本表达式无效：{error}");
            }
            var renderedValue = NodeToText(value);
            if (renderedValue.Length > 8_000)
            {
                throw new InvalidOperationException("文本表达式的结果超过 8000 个字符。");
            }
            rendered.Append(renderedValue);
            position = end + 2;
        }
    }

    private static int FindTemplateExpressionEnd(string text, int start)
    {
        var quote = '\0';
        var escaped = false;
        for (var index = start; index < text.Length - 1; index++)
        {
            var character = text[index];
            if (quote != '\0')
            {
                if (!escaped && character == quote)
                {
                    quote = '\0';
                }
                escaped = !escaped && character == '\\';
                continue;
            }
            if (character is '\'' or '"')
            {
                quote = character;
                continue;
            }
            if (character == '}' && text[index + 1] == '}')
            {
                return index;
            }
        }
        return -1;
    }

    private static bool TemplateReferencesToken(string text, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        if (text.Contains($"{{{{{token}}}}}", StringComparison.Ordinal)) return true;
        var position = 0;
        while (true)
        {
            var start = text.IndexOf("{{=", position, StringComparison.Ordinal);
            if (start < 0) return false;
            var end = FindTemplateExpressionEnd(text, start + 3);
            if (end < 0) return false;
            if (ExpressionReferencesToken(text[(start + 3)..end], token)) return true;
            position = end + 2;
        }
    }

    private static bool ExpressionReferencesToken(string expression, string token)
    {
        var quote = '\0';
        var escaped = false;
        for (var index = 0; index < expression.Length;)
        {
            var character = expression[index];
            if (quote != '\0')
            {
                if (!escaped && character == quote) quote = '\0';
                escaped = !escaped && character == '\\';
                if (character != '\\') escaped = false;
                index++;
                continue;
            }
            if (character is '\'' or '"')
            {
                quote = character;
                index++;
                continue;
            }
            if (character != '_' && !char.IsLetter(character))
            {
                index++;
                continue;
            }

            var start = index++;
            while (index < expression.Length &&
                   (expression[index] == '_' || char.IsLetterOrDigit(expression[index]))) index++;
            var length = index - start;
            if (length == token.Length &&
                string.Compare(expression, start, token, 0, length, StringComparison.OrdinalIgnoreCase) == 0) return true;
        }
        return false;
    }

    private static bool IsTemplateToken(string token) =>
        !string.IsNullOrWhiteSpace(token) &&
        token.Length <= 64 &&
        char.IsLetter(token[0]) &&
        token.All(character => char.IsLetterOrDigit(character) || character is '_' or '-');

    private static bool IsObservedOutputBinding(JsonObject binding, string path) =>
        string.Equals(GetString(binding, "sourceKind"), "observed-output", StringComparison.OrdinalIgnoreCase) &&
        path.StartsWith("$", StringComparison.Ordinal) &&
        path.Length <= 256;

    private static JsonNode ResolveBinding(
        JsonObject binding,
        IReadOnlyDictionary<string, JsonNode> outputs,
        IReadOnlyDictionary<string, JsonNode> functionSelectionInputs)
    {
        var nodeId = GetString(binding, "nodeId");
        var path = GetString(binding, "path") ?? "$";
        var sourceKind = GetString(binding, "sourceKind") ?? "output";
        JsonNode value;
        if (string.Equals(sourceKind, "function-selection", StringComparison.OrdinalIgnoreCase))
        {
            var parameterName = GetString(binding, "sourceParameterName");
            const string inputPathPrefix = "$.__functionInput.";
            if (string.IsNullOrWhiteSpace(parameterName) && path.StartsWith(inputPathPrefix, StringComparison.Ordinal))
            {
                parameterName = path[inputPathPrefix.Length..];
            }
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(parameterName) ||
                !functionSelectionInputs.TryGetValue(nodeId, out var inputValues) || inputValues is not JsonObject inputs)
            {
                throw new InvalidOperationException($"上游 Function 节点“{nodeId}”尚未产生可绑定的 Selection 参数。");
            }
            var parameterKey = inputs.Select(pair => pair.Key).FirstOrDefault(key =>
                string.Equals(key, parameterName, StringComparison.OrdinalIgnoreCase));
            if (parameterKey == null)
            {
                throw new InvalidOperationException($"上游 Function 的 Selection 参数“{parameterName}”已不存在或当前未提供值。");
            }
            value = inputs[parameterKey]?.DeepClone();
            path = "$";
        }
        else if (string.IsNullOrWhiteSpace(nodeId) || !outputs.TryGetValue(nodeId, out value))
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

    private static JsonObject ExtractFunctionSelectionValues(
        JsonNode resolvedParameters,
        IReadOnlyList<Senparc.Ncf.XncfBase.FunctionParameterInfo> parameterInfos)
    {
        var selections = new JsonObject();
        if (resolvedParameters is not JsonObject parameters)
        {
            return selections;
        }
        foreach (var parameter in (parameterInfos ?? Array.Empty<Senparc.Ncf.XncfBase.FunctionParameterInfo>())
                     .Where(parameter =>
                         (parameter.ParameterType is Senparc.Ncf.XncfBase.ParameterType.DropDownList or
                             Senparc.Ncf.XncfBase.ParameterType.CheckBoxList) &&
                         !string.IsNullOrWhiteSpace(parameter.Name)))
        {
            var key = parameters.Select(pair => pair.Key).FirstOrDefault(candidate =>
                string.Equals(candidate, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (key != null)
            {
                selections[parameter.Name] = parameters[key]?.DeepClone();
            }
        }
        return selections;
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
        string output,
        string? outputSchema = null,
        string? input = null)
    {
        progress?.Invoke(new NeuCharWorkflowProgress(
            node.Id,
            node.Name ?? node.Type,
            status,
            message,
            output?.Length > 8_000 ? output[..8_000] : output,
            DateTimeOffset.UtcNow,
            outputSchema,
            LimitReplayText(input, 20_000)));
    }

    private static string? LimitReplayText(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength] + "\n…（回看内容已截断）";

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

    private static string GetRuntimeText(JsonObject config, string name)
    {
        var value = config?[name];
        if (value is JsonObject obj && obj["$template"] is JsonObject template)
        {
            return GetString(template, "text");
        }
        return GetString(config, name);
    }

    private static string ValidateNodeTextTemplates(NeuCharWorkflowNode node)
    {
        var fields = node.Type.ToLowerInvariant() switch
        {
            "condition" => new[] { "left", "right" },
            "agent" or "agent-group" or "a2a" or "sub-workflow" => new[] { "prompt" },
            "neubell" => new[] { "title", "summary" },
            "aggregate" => new[] { "outputTemplate" },
            "console" => new[] { "printTemplate" },
            _ => Array.Empty<string>()
        };

        foreach (var field in fields)
        {
            var error = ValidateRuntimeTextValue(node.Config?[field]);
            if (error != null)
            {
                return $"字段“{field}”：{error}";
            }
        }

        return null;
    }

    private static string ValidateRuntimeTextValue(JsonNode value)
    {
        if (value is JsonObject obj && obj["$template"] is JsonObject template)
        {
            return ValidateTemplateText(template);
        }
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text) &&
            text.Contains("{{=", StringComparison.Ordinal))
        {
            return ValidateTemplateText(new JsonObject { ["text"] = text });
        }
        return null;
    }

    private static string? TryGetWorkflowRunId(string correlationId)
    {
        const string marker = "-run-";
        var index = correlationId?.LastIndexOf(marker, StringComparison.Ordinal) ?? -1;
        if (index < 0)
        {
            return null;
        }
        var candidate = correlationId[(index + marker.Length)..];
        return Guid.TryParse(candidate, out var parsed) ? parsed.ToString("N") : null;
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
        var catalog = await ExecuteInFunctionScopeAsync(functionService =>
                functionService.GetCatalogAsync(moduleUid, true, cancellationToken))
            .ConfigureAwait(false);
        var descriptor = catalog.FirstOrDefault(z =>
            string.Equals(z.FunctionKey, functionKey, StringComparison.OrdinalIgnoreCase));
        return descriptor == null
            ? null
            : new ResolvedFunctionReference(descriptor, "{}");
    }

    private async Task<T> ExecuteInFunctionScopeAsync<T>(
        Func<NeuCharWorkflowFunctionService, Task<T>> operation)
    {
        using var scope = _scopeFactory.CreateScope();
        var functionService = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowFunctionService>();
        return await operation(functionService).ConfigureAwait(false);
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
            if (parameterKey == null)
            {
                continue;
            }

            if (parameter.ParameterType == Senparc.Ncf.XncfBase.ParameterType.Text &&
                parameters[parameterKey] is JsonValue)
            {
                var formulaError = ValidateRuntimeTextValue(parameters[parameterKey]);
                if (formulaError != null)
                {
                    return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”：{formulaError}";
                }
            }

            if (parameters[parameterKey] is not JsonObject value)
            {
                continue;
            }

            if (value["$template"] is JsonObject template)
            {
                var templateError = ValidateTemplate(parameter, template);
                if (templateError != null)
                {
                    return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”：{templateError}";
                }
                // Individual template sources are checked by ValidateNodeBindingsAsync. Unlike a
                // whole-value binding, an interpolated value is intentionally converted to text.
                continue;
            }
            if (value["$source"] is not JsonObject binding)
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
                if (string.Equals(GetString(binding, "sourceKind"), "function-selection", StringComparison.OrdinalIgnoreCase))
                {
                    return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”关联的 Function Selection 参数已在模块更新后删除或不可用。";
                }
                if (IsObservedOutputBinding(binding, path))
                {
                    continue;
                }
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”引用的输出字段“{path}”已不存在。";
            }
            if (!string.Equals(GetString(binding, "sourceKind") ?? "output", field.SourceKind ?? "output", StringComparison.OrdinalIgnoreCase))
            {
                return $"节点“{targetNode.Name}”参数“{parameter.Title ?? parameter.Name}”关联字段的类型已在模块更新后发生变化，请重新选择来源。";
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

    private static void ValidateWorkflowVariables(IReadOnlyCollection<NeuCharWorkflowVariable> variables)
    {
        if (variables.Count > MaxWorkflowVariables)
        {
            throw new InvalidOperationException($"单个工作流最多允许 {MaxWorkflowVariables} 个变量。");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables)
        {
            var name = variable?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) ||
                !char.IsLetter(name[0]) && name[0] != '_' ||
                name.Length > 64 ||
                name.Any(character => !char.IsLetterOrDigit(character) && character != '_') ||
                !names.Add(name) ||
                string.Equals(name, "input", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "vars", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("工作流变量名必须唯一，以字母或下划线开头，且仅包含字母、数字、下划线；不能使用 input 或 vars。");
            }
            if (variable?.Value?.ToJsonString().Length > 8_000)
            {
                throw new InvalidOperationException($"工作流变量“{name}”的值不能超过 8000 个字符。");
            }
            var expressionError = ValidateRuntimeTextValue(variable?.Value);
            if (expressionError != null)
            {
                throw new InvalidOperationException($"工作流变量“{name}”：{expressionError}");
            }
        }
    }

    private static string ValidateCodeAssignments(
        JsonObject config,
        IReadOnlyCollection<NeuCharWorkflowVariable> workflowVariables)
    {
        if (config?["assignments"] is not JsonArray assignments)
        {
            return "安全代码节点必须提供变量赋值列表。";
        }
        if (assignments.Count == 0 || assignments.Count > MaxWorkflowVariables)
        {
            return $"安全代码节点一次必须设置 1 到 {MaxWorkflowVariables} 条变量赋值。";
        }

        var declaredNames = (workflowVariables ?? Array.Empty<NeuCharWorkflowVariable>())
            .Select(variable => variable?.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (item, index) in assignments.Select((item, index) => (item, index)))
        {
            if (item is not JsonObject assignment)
            {
                return $"第 {index + 1} 条变量赋值格式无效。";
            }
            var name = GetString(assignment, "name")?.Trim();
            if (string.IsNullOrWhiteSpace(name) || !declaredNames.Contains(name))
            {
                return $"第 {index + 1} 条赋值必须选择已定义的工作流变量。";
            }
            if (assignment["value"]?.ToJsonString().Length > 8_000)
            {
                return $"变量“{name}”的赋值不能超过 8000 个字符。";
            }
            var expressionError = ValidateRuntimeTextValue(assignment["value"]);
            if (expressionError != null)
            {
                return $"变量“{name}”：{expressionError}";
            }
        }
        return null;
    }

    private static IEnumerable<int> GetSubWorkflowIds(NeuCharWorkflowGraph graph) =>
        (graph?.Nodes ?? Enumerable.Empty<NeuCharWorkflowNode>())
            .Where(node => node.Type.Equals("sub-workflow", StringComparison.OrdinalIgnoreCase))
            .Select(node => GetInt(node.Config, "workflowId", 0));

    /// <summary>
    /// Validates the optional explicit loop body. Legacy loops without a loop-end marker are
    /// accepted and retain their historical semantics for compatibility. Once a marker exists,
    /// the body is deliberately a single linear chain so an iteration has exactly one boundary
    /// completion and cannot accidentally release the continuation multiple times.
    /// </summary>
    private static string ValidateLoopBoundary(NeuCharWorkflowGraph graph, NeuCharWorkflowNode loop)
    {
        var nodeMap = graph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var outgoing = graph.Edges.Where(edge => edge.Source == loop.Id).ToList();
        if (outgoing.Count == 0)
        {
            return null;
        }

        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(outgoing.Select(edge => edge.Target));
        var boundaryIds = new HashSet<string>(StringComparer.Ordinal);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!reachable.Add(current))
            {
                continue;
            }

            var currentNode = nodeMap[current];
            if (currentNode.Type.Equals("loop-end", StringComparison.OrdinalIgnoreCase))
            {
                boundaryIds.Add(current);
                continue;
            }

            foreach (var edge in graph.Edges.Where(edge => edge.Source == current))
            {
                queue.Enqueue(edge.Target);
            }
        }

        // No explicit boundary means this is an old workflow. Preserve it until the author
        // inserts a loop-end node to opt into bounded body semantics.
        if (boundaryIds.Count == 0)
        {
            return null;
        }
        if (boundaryIds.Count > 1)
        {
            return $"循环节点“{loop.Name ?? loop.Id}”的循环体只能有一个“循环结束”节点。";
        }

        var boundaryId = boundaryIds.Single();
        var currentId = outgoing[0].Target;
        var previousId = loop.Id;
        var bodyVisited = new HashSet<string>(StringComparer.Ordinal);
        while (!string.Equals(currentId, boundaryId, StringComparison.Ordinal))
        {
            if (!bodyVisited.Add(currentId))
            {
                return $"循环节点“{loop.Name ?? loop.Id}”的循环体存在重复路径，无法确定循环结束位置。";
            }

            var currentNode = nodeMap[currentId];
            if (currentNode.Type is "condition" or "parallel" or "aggregate" or "merge" or "loop" or "end")
            {
                return $"循环节点“{loop.Name ?? loop.Id}”当前只支持由普通节点组成的单一路径；节点“{currentNode.Name ?? currentNode.Id}”不能放在循环体中。";
            }

            var incoming = graph.Edges.Where(edge => edge.Target == currentId).ToList();
            if (incoming.Count != 1 || !string.Equals(incoming[0].Source, previousId, StringComparison.Ordinal))
            {
                return $"循环节点“{loop.Name ?? loop.Id}”的循环体节点“{currentNode.Name ?? currentNode.Id}”不能被循环外路径共享。";
            }

            var next = graph.Edges.Where(edge => edge.Source == currentId).ToList();
            if (next.Count != 1)
            {
                return $"循环节点“{loop.Name ?? loop.Id}”的循环体必须是一条连接到“循环结束”的单一路径。";
            }

            previousId = currentId;
            currentId = next[0].Target;
        }

        var boundaryIncoming = graph.Edges.Where(edge => edge.Target == boundaryId).ToList();
        if (boundaryIncoming.Count != 1 || !string.Equals(boundaryIncoming[0].Source, previousId, StringComparison.Ordinal))
        {
            return $"循环结束节点必须是循环体的唯一最后节点，且不能被循环外路径共享。";
        }

        return null;
    }

    private static string FindLoopBoundaryNodeId(NeuCharWorkflowGraph graph, string loopNodeId)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var boundaryIds = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(graph.Edges
            .Where(edge => edge.Source == loopNodeId)
            .Select(edge => edge.Target));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!reachable.Add(current))
            {
                continue;
            }

            var node = graph.Nodes.FirstOrDefault(item => item.Id == current);
            if (node?.Type.Equals("loop-end", StringComparison.OrdinalIgnoreCase) == true)
            {
                boundaryIds.Add(current);
                continue;
            }

            foreach (var edge in graph.Edges.Where(edge => edge.Source == current))
            {
                queue.Enqueue(edge.Target);
            }
        }

        return boundaryIds.Count switch
        {
            0 => null,
            1 => boundaryIds.Single(),
            _ => throw new InvalidOperationException($"循环节点“{loopNodeId}”的循环体只能有一个“循环结束”节点。")
        };
    }

    private static string ValidateLoopCountConfiguration(JsonObject config)
    {
        var count = config?["count"];
        if (count is JsonObject { } countObject && countObject["$source"] is JsonObject)
        {
            return null;
        }

        return TryReadLoopCount(count, out _)
            ? null
            : $"循环次数必须为 1 到 {MaxLoopIterations} 的整数，或引用上游单值。";
    }

    private static string ValidateAggregateOutputTemplate(JsonObject config)
    {
        var template = GetRuntimeText(config, "outputTemplate");
        if (string.IsNullOrWhiteSpace(template))
        {
            return "聚合节点必须设置输出内容。可使用 {{input}}、length(input)、join(input, '，') 等受限表达式。";
        }
        if (template.Length > 8_000)
        {
            return "聚合输出内容不能超过 8000 个字符。";
        }

        var position = 0;
        var tokenCount = 0;
        while (true)
        {
            var start = template.IndexOf("{{", position, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }
            var end = template.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0 || ++tokenCount > 32)
            {
                return "聚合输出内容中的占位符缺少结束标记，或数量超过 32 个。";
            }

            var token = template[(start + 2)..end].Trim();
            if (string.Equals(token, "input", StringComparison.OrdinalIgnoreCase))
            {
                position = end + 2;
                continue;
            }
            if (token.StartsWith("=", StringComparison.Ordinal))
            {
                var expression = token[1..].Trim();
                if (!NeuCharWorkflowExpressionEngine.TryValidate(expression, new[] { "input", "vars" }, out var error))
                {
                    return $"聚合输出表达式无效：{error}";
                }
                position = end + 2;
                continue;
            }
            return "聚合输出内容仅支持 {{input}} 或 {{= 表达式 }}。";
        }
    }

    private static string ValidateTemplate(
        Senparc.Ncf.XncfBase.FunctionParameterInfo parameter,
        JsonObject template)
    {
        if (parameter.ParameterType != Senparc.Ncf.XncfBase.ParameterType.Text ||
            GetParameterValueShape(parameter).isArray)
        {
            return "文本中嵌入变量仅支持单值文本参数；请使用“关联上游输出”传入完整值。";
        }
        if (template["text"] is not JsonValue textValue || !textValue.TryGetValue<string>(out var text))
        {
            return "文本模板缺少可编辑的文本内容。";
        }
        var bindings = template["bindings"] as JsonArray;
        if (template["bindings"] != null && bindings == null)
        {
            return "文本模板的变量列表格式无效。";
        }
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "input", "vars" };
        foreach (var (index, item) in (bindings ?? new JsonArray()).Select((item, index) => (index, item)))
        {
            if (item is not JsonObject entry)
            {
                return $"第 {index + 1} 个变量格式无效。";
            }
            var token = GetString(entry, "token");
            if (!IsTemplateToken(token) || !tokens.Add(token))
            {
                return $"第 {index + 1} 个变量名称无效或重复。";
            }
            if (entry["source"] is not JsonObject)
            {
                return $"第 {index + 1} 个变量缺少绑定来源。";
            }
            if (!TemplateReferencesToken(text, token))
            {
                return $"变量“{token}”没有出现在文本中。";
            }
        }
        var expressionPosition = 0;
        var expressionCount = 0;
        while (true)
        {
            var start = text.IndexOf("{{=", expressionPosition, StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }
            var end = FindTemplateExpressionEnd(text, start + 3);
            if (end < 0 || ++expressionCount > 32)
            {
                return "文本表达式缺少结束标记，或数量超过 32 个。";
            }
            var expression = text[(start + 3)..end].Trim();
            if (!NeuCharWorkflowExpressionEngine.TryValidate(expression, tokens, out var error))
            {
                return $"文本表达式无效：{error}";
            }
            expressionPosition = end + 2;
        }
        return null;
    }

    private static string ValidateTemplateText(JsonObject template)
    {
        if (template["text"] is not JsonValue textValue || !textValue.TryGetValue<string>(out var text))
        {
            return "文本模板缺少可编辑的文本内容。";
        }
        var bindings = template["bindings"] as JsonArray;
        if (template["bindings"] != null && bindings == null)
        {
            return "文本模板的变量列表格式无效。";
        }
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "input", "vars" };
        foreach (var (index, item) in (bindings ?? new JsonArray()).Select((item, index) => (index, item)))
        {
            if (item is not JsonObject entry)
            {
                return $"第 {index + 1} 个变量格式无效。";
            }
            var token = GetString(entry, "token");
            if (!IsTemplateToken(token) || !tokens.Add(token))
            {
                return $"第 {index + 1} 个变量名称无效或重复。";
            }
            if (entry["source"] is not JsonObject)
            {
                return $"第 {index + 1} 个变量缺少绑定来源。";
            }
            if (!TemplateReferencesToken(text, token))
            {
                return $"变量“{token}”没有出现在文本中。";
            }
        }

        var expressionPosition = 0;
        var expressionCount = 0;
        while (true)
        {
            var start = text.IndexOf("{{=", expressionPosition, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }
            var end = FindTemplateExpressionEnd(text, start + 3);
            if (end < 0 || ++expressionCount > 32)
            {
                return "文本表达式缺少结束标记，或数量超过 32 个。";
            }
            var expression = text[(start + 3)..end].Trim();
            if (!NeuCharWorkflowExpressionEngine.TryValidate(expression, tokens, out var error))
            {
                return $"文本表达式无效：{error}";
            }
            expressionPosition = end + 2;
        }
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
                if (string.Equals(GetString(binding, "sourceKind"), "function-selection", StringComparison.OrdinalIgnoreCase))
                {
                    return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 关联的 Function Selection 参数已在模块更新后删除或不可用。";
                }
                if (IsObservedOutputBinding(binding, path))
                {
                    continue;
                }
                return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 引用的输出字段“{path}”已不存在。";
            }
            if (!BindingSourceKindMatches(configPath, binding, field))
            {
                return $"节点“{targetNode.Name ?? targetNode.Id}”的 {configPath} 关联字段的类型已在模块更新后发生变化，请重新选择来源。";
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
            if (obj["$template"] is JsonObject template)
            {
                if (template["bindings"] is JsonArray bindings)
                {
                    for (var index = 0; index < bindings.Count; index++)
                    {
                        if (bindings[index] is JsonObject entry && entry["source"] is JsonObject source)
                        {
                            yield return ($"{path}.$template.bindings[{index}]", source);
                        }
                    }
                }
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

    private static bool BindingSourceKindMatches(
        string configPath,
        JsonObject binding,
        NeuCharFunctionOutputFieldDescriptor field)
    {
        var bindingKind = GetString(binding, "sourceKind") ?? "output";
        var fieldKind = field.SourceKind ?? "output";
        if (string.Equals(bindingKind, fieldKind, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Runtime-observed fields are not part of a module's declared output contract. Text
        // templates and the loop count both accept a single runtime value, so an observed field
        // that now overlaps the generic "$" output descriptor remains usable after re-indexing.
        var path = GetString(binding, "path") ?? "$";
        var acceptsObservedOutput = configPath.Contains(".$template.bindings[", StringComparison.Ordinal) ||
                                    string.Equals(configPath, "config.count", StringComparison.Ordinal);
        return acceptsObservedOutput &&
               IsObservedOutputBinding(binding, path) &&
               string.Equals(fieldKind, "output", StringComparison.OrdinalIgnoreCase);
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
            var reference = await ResolveFunctionReferenceAsync(node, cancellationToken).ConfigureAwait(false);
            return reference == null
                ? null
                : AddFunctionSelectionInputFields(reference.Descriptor.Output, reference.Descriptor.Parameters);
        }
        if (node.Type.Equals("aggregate", StringComparison.OrdinalIgnoreCase))
        {
            var preservesArray = string.Equals(GetRuntimeText(node.Config, "outputTemplate")?.Trim(), "{{input}}",
                StringComparison.OrdinalIgnoreCase);
            return new NeuCharFunctionOutputDescriptor(
                preservesArray ? "any" : "string",
                preservesArray ? "Object[]" : "聚合输出文本",
                preservesArray,
                preservesArray ? "any" : null,
                new[] { new NeuCharFunctionOutputFieldDescriptor("$", preservesArray ? "聚合数组" : "聚合输出", "any", preservesArray, false) });
        }
        if (node.Type.Equals("merge", StringComparison.OrdinalIgnoreCase))
        {
            return new NeuCharFunctionOutputDescriptor(
                "any",
                "逐项合流输出",
                false,
                null,
                new[] { new NeuCharFunctionOutputFieldDescriptor("$", "当前输入项", "any", false, false) });
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
        if (node.Type is "delay" or "condition" or "loop" or "loop-end" or "code" or "console" or "end")
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
        var typeName = node.Type is "manual-trigger" or "interval-trigger" or "agent" or "agent-group" or "a2a" or "sub-workflow"
            ? "string"
            : "any";
        return new NeuCharFunctionOutputDescriptor(
            typeName,
            typeName,
            false,
            null,
            new[] { new NeuCharFunctionOutputFieldDescriptor("$", "节点输出", typeName, false, false) });
    }

    private static NeuCharFunctionOutputDescriptor AddFunctionSelectionInputFields(
        NeuCharFunctionOutputDescriptor output,
        IReadOnlyList<Senparc.Ncf.XncfBase.FunctionParameterInfo> parameterInfos)
    {
        var selectionFields = (parameterInfos ?? Array.Empty<Senparc.Ncf.XncfBase.FunctionParameterInfo>())
            .Where(parameter =>
                (parameter.ParameterType is Senparc.Ncf.XncfBase.ParameterType.DropDownList or
                    Senparc.Ncf.XncfBase.ParameterType.CheckBoxList) &&
                !string.IsNullOrWhiteSpace(parameter.Name))
            .Select(parameter =>
            {
                var shape = GetParameterValueShape(parameter);
                return new NeuCharFunctionOutputFieldDescriptor(
                    $"$.__functionInput.{parameter.Name}",
                    $"预载输入选择 · {parameter.Title ?? parameter.Name}",
                    shape.typeName,
                    shape.isArray,
                    false,
                    "function-selection",
                    parameter.Name);
            })
            .ToArray();
        if (selectionFields.Length == 0)
        {
            return output;
        }

        var outputFields = output?.Fields ?? Array.Empty<NeuCharFunctionOutputFieldDescriptor>();
        return new NeuCharFunctionOutputDescriptor(
            output?.TypeName ?? "any",
            output?.DisplayName ?? "Function 输出",
            output?.IsArray ?? false,
            output?.ElementTypeName,
            outputFields.Concat(selectionFields).ToArray());
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
        var unreachable = FindDisconnectedNodes(graph, triggerId);
        if (unreachable.Count == 0)
        {
            return;
        }

        var names = unreachable
            .Select(z => z.Name ?? z.Id)
            .Take(5);
        throw new InvalidOperationException($"工作流包含未连接到触发器的节点：{string.Join("、", names)}。");
    }

    private static IReadOnlyList<NeuCharWorkflowNode> FindDisconnectedNodes(NeuCharWorkflowGraph graph, string triggerId)
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

        return graph.Nodes.Where(z => !visited.Contains(z.Id)).ToList();
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
                var runCoordinator = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowRunCoordinator>();
                var now = DateTime.UtcNow;
                var workflows = await workflowService.GetFullListAsync(
                    z => z.Enabled && z.TriggerType == "interval" && z.NextRunAt != null && z.NextRunAt <= now,
                    z => z.NextRunAt,
                    OrderingType.Ascending).ConfigureAwait(false);
                foreach (var workflow in workflows.Take(10))
                {
                    if (!runCoordinator.TryStart(workflow.Id, workflow.AdminUserId, string.Empty, out _, out var error, "interval"))
                    {
                        _logger.LogDebug("跳过已在运行的定时 Workflow：WorkflowId={WorkflowId}，原因：{Error}", workflow.Id, error);
                    }
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
