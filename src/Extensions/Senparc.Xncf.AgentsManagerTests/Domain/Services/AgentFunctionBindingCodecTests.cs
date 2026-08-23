using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System.Linq;

namespace Senparc.Xncf.AgentsManagerTests.Domain.Services;

[TestClass]
public class AgentFunctionBindingCodecTests
{
    [TestMethod]
    public void Parse_LegacyPluginNames_ProducesPluginBindings()
    {
        var bindings = AgentFunctionBindingCodec.Parse("PluginA, PluginB,PluginA");

        Assert.AreEqual(2, bindings.Count);
        CollectionAssert.AreEquivalent(
            new[] { "PluginA", "PluginB" },
            bindings.Select(binding => binding.Key).ToArray());
        Assert.IsTrue(bindings.All(binding => binding.Kind == "plugin"));
    }

    [TestMethod]
    public void SerializeAndParse_PreservesFunctionAndWorkflowBindings()
    {
        var input = new[]
        {
            new AgentFunctionBindingDto
            {
                Kind = "function",
                Key = "module::function",
                ModuleUid = "module",
                FunctionKey = "function",
                Name = "Function"
            },
            new AgentFunctionBindingDto
            {
                Kind = "workflow",
                Key = "42",
                WorkflowId = 42,
                Name = "Workflow"
            },
            new AgentFunctionBindingDto
            {
                Kind = "plugin",
                Key = "PluginA"
            }
        };

        var stored = AgentFunctionBindingCodec.Serialize(input);
        var parsed = AgentFunctionBindingCodec.Parse(stored);

        Assert.AreEqual(3, parsed.Count);
        Assert.IsTrue(parsed.Any(binding => binding.Kind == "function" && binding.Key == "module::function"));
        Assert.IsTrue(parsed.Any(binding => binding.Kind == "workflow" && binding.Key == "42"));
        Assert.AreEqual("PluginA", AgentFunctionBindingCodec.GetLegacyPluginNames(stored));
    }

    [TestMethod]
    public void Serialize_WithLegacyNames_MergesWithoutDuplicates()
    {
        var stored = AgentFunctionBindingCodec.Serialize(
            new[]
            {
                new AgentFunctionBindingDto { Kind = "plugin", Key = "PluginA" }
            },
            "PluginA,PluginB");

        var parsed = AgentFunctionBindingCodec.Parse(stored);

        Assert.AreEqual(2, parsed.Count);
        CollectionAssert.AreEquivalent(
            new[] { "PluginA", "PluginB" },
            parsed.Select(binding => binding.Key).ToArray());
    }
}
