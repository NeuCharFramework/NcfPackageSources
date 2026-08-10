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

    private static Task<AppResponseBase<List<SampleOutput>>> ListOutputFunction() => null!;

    private sealed class SampleOutput
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    private static NeuCharWorkflowEngine CreateEngine() =>
        new(null!, null!, null!, null!, Array.Empty<IWorkflowObjectProvider>());
}
