/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowEngineTests.cs
    文件功能描述：NeuChar Workflow 声明式图安全校验测试
----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuCharWorkflowEngineTests
{
    [TestMethod]
    public void ParseAndValidateGraph_ValidLinearGraph_ShouldNormalizeConfig()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger", "name": "手动触发" },
                { "id": "delay", "type": "delay", "name": "等待" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "delay" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Nodes.Count);
        Assert.IsNotNull(graph.Nodes[0].Config);
        Assert.AreEqual("delay", graph.Edges[0].Target);
    }

    [TestMethod]
    public async Task ParseAndValidateGraph_LayoutDirection_ShouldPersistAndNormalize()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "layout": { "direction": "horizontal" },
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);
        var editableJson = await engine.BuildEditableGraphJsonAsync(graphJson);

        Assert.AreEqual("horizontal", graph.Layout.Direction);
        StringAssert.Contains(editableJson, "\"layout\"");
        StringAssert.Contains(editableJson, "\"horizontal\"");

        var legacyGraph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" }], "edges":[] }""");
        Assert.AreEqual("vertical", legacyGraph.Layout.Direction);
    }

    [TestMethod]
    public void ParseAndValidateGraph_Cycle_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "delay", "type": "delay" },
                { "id": "condition", "type": "condition" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "delay" },
                { "id": "edge-2", "source": "delay", "target": "condition" },
                { "id": "edge-3", "source": "condition", "target": "delay", "sourceHandle": "true" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "不允许工作流形成循环");
    }

    [TestMethod]
    public void ParseAndValidateGraph_MultipleTriggers_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "manual", "type": "manual-trigger" },
                { "id": "interval", "type": "interval-trigger" }
              ],
              "edges": []
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "只能包含一个触发器");
    }

    [TestMethod]
    public void CalculateNextRun_IntervalBelowMinimum_ShouldClampToOneMinute()
    {
        var now = new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc);

        var result = NeuCharWorkflowEngine.CalculateNextRun(
            "interval",
            "{\"intervalSeconds\":1}",
            now);

        Assert.AreEqual(now.AddMinutes(1), result);
    }

    [TestMethod]
    public void ParseAndValidateGraph_DisconnectedNode_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "orphan", "type": "delay", "name": "孤立节点" }
              ],
              "edges": []
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "未连接到触发器");
    }

    [TestMethod]
    public async Task ParseAndValidateGraph_DraftWithDisconnectedNode_ShouldRemainEditable()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "orphan", "type": "delay", "name": "草稿等待" }
              ],
              "edges": []
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson, requireAllNodesReachable: false);

        Assert.AreEqual(1, engine.GetDisconnectedNodes(graph).Count);
        var editableJson = await engine.BuildEditableGraphJsonAsync(graphJson);
        StringAssert.Contains(editableJson, "orphan");
    }

    [TestMethod]
    public void ParseAndValidateGraph_LegacyConditionEdge_ShouldNormalizeToTrueBranch()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual("default", graph.Edges[0].SourceHandle);
        Assert.AreEqual("true", graph.Edges[1].SourceHandle);
    }

    [TestMethod]
    public void ParseAndValidateGraph_OrdinaryNodeWithTwoOutputs_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "end-a", "type": "end" },
                { "id": "end-b", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "end-a" },
                { "id": "edge-2", "source": "trigger", "target": "end-b" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "只能连接一个后续节点");
    }

    [TestMethod]
    public void ParseAndValidateGraph_ParallelWithMultipleOutputs_ShouldBeAllowed()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "parallel", "type": "parallel", "name": "并行" },
                { "id": "end-a", "type": "end", "name": "分支 A" },
                { "id": "end-b", "type": "end", "name": "分支 B" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "parallel" },
                { "id": "edge-2", "source": "parallel", "target": "end-a" },
                { "id": "edge-3", "source": "parallel", "target": "end-b" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Edges.Count(edge => edge.Source == "parallel"));
        Assert.IsTrue(graph.Nodes.Any(node => node.Type == "parallel"));
    }

    [TestMethod]
    public void ParseAndValidateGraph_OrdinaryNodeWithTwoInputs_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "delay", "type": "delay", "name": "普通节点" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "delay", "sourceHandle": "true" },
                { "id": "edge-3", "source": "condition", "target": "delay", "sourceHandle": "false" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "只允许一个上游连接");
    }

    [TestMethod]
    public void ParseAndValidateGraph_AggregateWithTwoInputs_ShouldBeAllowed()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "aggregate", "type": "aggregate", "name": "聚合" },
                { "id": "console", "type": "console", "name": "Console 打印" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "aggregate", "sourceHandle": "true" },
                { "id": "edge-3", "source": "condition", "target": "aggregate", "sourceHandle": "false" },
                { "id": "edge-4", "source": "aggregate", "target": "console" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Edges.Count(z => z.Target == "aggregate"));
        Assert.IsTrue(graph.Nodes.Any(z => z.Type == "console"));
    }

    [TestMethod]
    public void AggregateInput_ShouldContainOnlyActiveIncomingEdgesInGraphOrder()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "BuildAggregateInput",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var aggregate = new NeuCharWorkflowNode { Id = "aggregate", Type = "aggregate" };
        var graph = new NeuCharWorkflowGraph
        {
            Nodes = { aggregate },
            Edges =
            {
                new NeuCharWorkflowEdge { Id = "true-edge", Source = "condition", Target = "aggregate", SourceHandle = "true" },
                new NeuCharWorkflowEdge { Id = "false-edge", Source = "condition", Target = "aggregate", SourceHandle = "false" }
            }
        };
        var outputs = new Dictionary<string, JsonNode>
        {
            ["condition"] = JsonValue.Create("selected")!
        };

        var value = (JsonArray)method.Invoke(null, new object[]
        {
            graph,
            aggregate,
            new HashSet<string> { "true-edge" },
            outputs
        })!;

        Assert.AreEqual(1, value.Count);
        Assert.AreEqual("selected", value[0]!.GetValue<string>());
    }

    [TestMethod]
    public void ParseAndValidateGraph_FunctionWithTwoInputs_ShouldBeAllowed()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "function", "type": "function", "name": "共享 Function" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "function", "sourceHandle": "true" },
                { "id": "edge-3", "source": "condition", "target": "function", "sourceHandle": "false" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Edges.Count(z => z.Target == "function"));
    }

    [TestMethod]
    public void ExecutionLog_ReplaySnapshotAndEvents_ShouldBeStoredSeparately()
    {
        var executionLog = new Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflowExecutionLog(
            12,
            "回看测试",
            "workflow-12-run-0123456789abcdef0123456789abcdef");

        executionLog.SetReplaySnapshot("a".PadLeft(64, 'a'), "{\"graphJson\":\"{}\"}");
        executionLog.Complete(true, "完成", null, "[{\"nodeId\":\"trigger\"}]");

        Assert.AreEqual(64, executionLog.ReplaySnapshotHash!.Length);
        Assert.AreEqual("{\"graphJson\":\"{}\"}", executionLog.ReplaySnapshotJson);
        Assert.AreEqual("[{\"nodeId\":\"trigger\"}]", executionLog.ReplayEventsJson);
        Assert.IsTrue(executionLog.Succeeded == true && executionLog.FinishedAt != null);
    }

    [TestMethod]
    public void ReplayProgress_ShouldRetainNodeInputAlongsideOutput()
    {
        var progress = new NeuCharWorkflowProgress(
            "function-1",
            "查询",
            "success",
            "节点执行完成。",
            "{\"result\":\"ok\"}",
            DateTimeOffset.UtcNow,
            null,
            "{\"keyword\":\"workflow\"}");

        Assert.AreEqual("{\"keyword\":\"workflow\"}", progress.Input);
        Assert.AreEqual("{\"result\":\"ok\"}", progress.Output);
    }

    [TestMethod]
    public async Task ValidateReferencesAsync_NeuBellNode_ShouldAcceptSupportedConsumptionModes()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "notify", "type": "neubell", "name": "发送纽铃", "config": { "title": "任务完成", "summary": "请查看 {{input}}", "consumeMode": "item" } }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "notify" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);
        Assert.IsNull(await engine.ValidateReferencesAsync(graph));

        graph.Nodes.Single(node => node.Id == "notify").Config["consumeMode"] = "unsupported";
        var error = await engine.ValidateReferencesAsync(graph);
        StringAssert.Contains(error, "消费方式无效");
    }

    [TestMethod]
    public void BuildOutputDescriptor_AppResponseList_ShouldExposeElementFieldsAndArrayShape()
    {
        var method = typeof(NeuCharWorkflowEngineTests).GetMethod(
            nameof(ListOutputFunction),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var output = NeuCharWorkflowFunctionService.BuildOutputDescriptor(method);

        Assert.IsTrue(output.IsArray);
        Assert.AreEqual("object", output.TypeName);
        var name = output.Fields.Single(z => z.Path == "$.Name");
        Assert.AreEqual("string", name.TypeName);
        Assert.IsTrue(name.RequiresIndex);
        var tags = output.Fields.Single(z => z.Path == "$.Tags");
        Assert.IsTrue(tags.IsArray);
        Assert.IsTrue(tags.RequiresIndex);
    }

    [TestMethod]
    public void WorkflowFunctionSchemaBuilder_UnnamedParameter_ShouldUseStableDraftKey()
    {
        var descriptor = new NeuCharFunctionDescriptor(
            "module",
            "测试模块",
            "1.0.0",
            true,
            "test-function",
            "测试 Function",
            null,
            new[] { new Senparc.Ncf.XncfBase.FunctionParameterInfo { Name = null, Title = null } });

        var parameter = WorkflowFunctionSchemaBuilder.Build(descriptor).Single();

        Assert.AreEqual("parameter_1", parameter.Name);
        Assert.AreEqual("Function 参数元数据缺少字段名；当前仅可保存草稿，修复或更新模块后才能运行。", parameter.MetadataError);
        Assert.IsTrue(parameter.HasSyntheticName);
    }

    [TestMethod]
    public void WorkflowFunctionSchemaBuilder_SelectionParameter_ShouldRetainSandboxStyleMetadata()
    {
        var descriptor = new NeuCharFunctionDescriptor(
            "sandbox",
            "Sandbox",
            "1.0.0",
            true,
            "create",
            "创建沙箱",
            null,
            new[]
            {
                new Senparc.Ncf.XncfBase.FunctionParameterInfo
                {
                    Name = "TemplateKey",
                    Title = "模板",
                    Description = "选择沙箱模板",
                    ParameterType = Senparc.Ncf.XncfBase.ParameterType.DropDownList,
                    SystemType = "String",
                    SelectionList = new Senparc.Ncf.XncfBase.Functions.SelectionList(
                        Senparc.Ncf.XncfBase.Functions.SelectionType.DropDownList,
                        new[] { new Senparc.Ncf.XncfBase.Functions.SelectionItem("python", "Python Exec", "Python 模板", true) })
                }
            });

        var parameter = WorkflowFunctionSchemaBuilder.Build(descriptor).Single();

        Assert.AreEqual("TemplateKey", parameter.Name);
        Assert.AreEqual("模板", parameter.Title);
        Assert.AreEqual("选择沙箱模板", parameter.Description);
        Assert.AreEqual(1, parameter.ParameterType);
        Assert.AreEqual("Python Exec", parameter.Options.Single().Text);
    }

    [TestMethod]
    public void ValidateRequiredParameters_UnnamedMetadata_ShouldRejectExecution()
    {
        var error = NeuCharWorkflowFunctionService.ValidateRequiredParameters(
            new[] { new Senparc.Ncf.XncfBase.FunctionParameterInfo { Name = null, Title = "未知参数" } },
            "{}");

        StringAssert.Contains(error, "缺少字段名");
    }

    [TestMethod]
    public void ResolveBinding_FunctionSelection_ShouldUseResolvedSelectionValue()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveBinding",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var binding = JsonNode.Parse(
            """
            {
              "nodeId": "source",
              "path": "$.__functionInput.crawlMode",
              "sourceKind": "function-selection",
              "sourceParameterName": "crawlMode"
            }
            """)!.AsObject();
        var selectionInputs = new Dictionary<string, JsonNode>
        {
            ["source"] = JsonNode.Parse("""{ "crawlMode": "full" }""")!
        };

        var value = (JsonNode?)method.Invoke(null, new object[]
        {
            binding,
            new Dictionary<string, JsonNode>(),
            selectionInputs
        });

        Assert.AreEqual("full", value!.GetValue<string>());
    }

    [TestMethod]
    public void ResolveRuntimeValue_Template_ShouldInterpolateMultipleBindings()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveRuntimeValue",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var template = JsonNode.Parse(
            """
            {
              "$template": {
                "text": "模板={{template}}；运行时={{runtime}}；表达式={{=upper(template)}}；输入={{input}}",
                "bindings": [
                  { "token": "template", "source": { "nodeId": "source", "path": "$.template" } },
                  { "token": "runtime", "source": { "nodeId": "source", "path": "$.runtime" } }
                ]
              }
            }
            """)!;
        var outputs = new Dictionary<string, JsonNode>
        {
            ["source"] = JsonNode.Parse("""{ "template": "Python Exec", "runtime": "Docker" }""")!
        };

        var value = (JsonNode?)method.Invoke(null, new object[]
        {
            template,
            JsonValue.Create("来自触发器"),
            outputs,
            new Dictionary<string, JsonNode>()
        });

        Assert.AreEqual("模板=Python Exec；运行时=Docker；表达式=PYTHON EXEC；输入=来自触发器", value!.GetValue<string>());
    }

    [TestMethod]
    public void TemplateExpression_ShouldUseBuiltInFunctions()
    {
        var variables = new Dictionary<string, JsonNode>
        {
            ["value_1"] = JsonValue.Create("VIP-user")
        };
        var valid = NeuCharWorkflowExpressionEngine.TryEvaluate(
            "if(contains(value_1, 'VIP'), substring(value_1, 0, 3), 'normal')",
            variables, out var result, out var error);
        Assert.IsTrue(valid, error);
        Assert.AreEqual("VIP", result!.GetValue<string>());
        Assert.IsFalse(NeuCharWorkflowExpressionEngine.TryValidate(
            "system.exit()", new[] { "value_1" }, out _));
    }

    [TestMethod]
    public void ObservedOutputSchema_ShouldExcludeSensitiveValuesAndMarkArrays()
    {
        var node = new NeuCharWorkflowNode { Id = "function-1", Name = "查询", Type = "function" };
        var schema = NeuCharWorkflowObservedOutputSchemaBuilder.Build(
            node,
            JsonNode.Parse("""{ "items": [{ "name": "first", "token": "hidden" }] }""")!);

        Assert.IsTrue(schema.Fields.Any(field => field.Path == "$.items.name" && field.RequiresIndex));
        Assert.IsFalse(schema.Fields.Any(field => field.Path.Contains("token")));
    }

    private static Task<AppResponseBase<List<SampleOutput>>> ListOutputFunction() => null!;

    private sealed class SampleOutput
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    private static NeuCharWorkflowEngine CreateEngine() =>
        new(null!, null!, null!, null!, Array.Empty<IWorkflowObjectProvider>());
}
