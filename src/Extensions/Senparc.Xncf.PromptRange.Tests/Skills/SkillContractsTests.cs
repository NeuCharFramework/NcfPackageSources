using System.Text.Json;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Tests.Skills;

[TestClass]
public class SkillContractsTests
{
    [TestMethod]
    public void PromptCodeRemainsAStableSkillVersion()
    {
        Assert.IsTrue(PromptItem.IsPromptVersion("2026.10.16.1-T1.2-A3"));
        Assert.IsFalse(PromptItem.IsPromptVersion("skill-name"));
    }

    [TestMethod]
    public void SkillPackageManifestRoundTrips()
    {
        var manifest = new SkillPackageManifest(
            SkillId: "customer.service",
            SkillCode: "customer.service",
            Name: "Customer service",
            Description: "Answers customer questions",
            Version: "2026.10.16.1-T1-A2",
            PromptCode: "2026.10.16.1-T1-A2");

        var json = JsonSerializer.Serialize(manifest);
        var restored = JsonSerializer.Deserialize<SkillPackageManifest>(json);

        Assert.IsNotNull(restored);
        Assert.AreEqual(manifest.SkillId, restored.SkillId);
        Assert.AreEqual(manifest.PromptCode, restored.PromptCode);
    }

    [TestMethod]
    public void EvaluationContextCanUseDraftSkillVersions()
    {
        var context = new SkillExecutionContext(
            "PromptRange",
            Mode: SkillExecutionMode.Evaluation);

        Assert.AreEqual("PromptRange", context.CallerModule);
        Assert.AreEqual(SkillExecutionMode.Evaluation, context.Mode);
    }
}
