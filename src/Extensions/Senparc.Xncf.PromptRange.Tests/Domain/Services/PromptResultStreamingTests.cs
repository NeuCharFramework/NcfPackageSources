using System.Reflection;
using Senparc.Xncf.PromptRange.Domain.Services;

namespace Senparc.Xncf.PromptRange.Domain.Services.Tests;

[TestClass]
public class PromptResultStreamingTests
{
    [TestMethod]
    public void ResolveChatOutput_UsesStreamedText_WhenAgentKernelOutputIsEmpty()
    {
        var output = ResolveChatOutput(string.Empty, "streamed response");

        Assert.AreEqual("streamed response", output);
    }

    [TestMethod]
    public void ResolveChatOutput_PreservesAgentKernelOutput_WhenItIsPresent()
    {
        var output = ResolveChatOutput("direct response", "streamed response");

        Assert.AreEqual("direct response", output);
    }

    [TestMethod]
    public void ApplyOllamaLowTokenCompatibility_DisablesThinkingBelowMinimumBudget()
    {
        var options = new Microsoft.Extensions.AI.ChatOptions
        {
            MaxOutputTokens = 100
        };
        var model = new Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto.AIModelDto
        {
            AiPlatform = Senparc.AI.AiPlatform.Ollama,
            ModelId = "qwen3.8:27b"
        };

        ApplyOllamaLowTokenCompatibility(options, model);

        Assert.IsTrue(options.AdditionalProperties.TryGetValue("think", out var think));
        Assert.AreEqual(false, think);
    }

    [TestMethod]
    public void ApplyOllamaLowTokenCompatibility_PreservesThinkingAtOrAboveMinimumBudget()
    {
        var options = new Microsoft.Extensions.AI.ChatOptions
        {
            MaxOutputTokens = 512
        };
        var model = new Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto.AIModelDto
        {
            AiPlatform = Senparc.AI.AiPlatform.Ollama,
            ModelId = "qwen3.8:27b"
        };

        ApplyOllamaLowTokenCompatibility(options, model);

        Assert.IsFalse(options.AdditionalProperties?.ContainsKey("think") ?? false);
    }

    private static string ResolveChatOutput(string output, string streamedText)
    {
        var method = typeof(PromptResultService).GetMethod(
            "ResolveChatOutput",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        var result = method.Invoke(null, new object[] { output, streamedText });
        return result as string
               ?? throw new AssertFailedException("ResolveChatOutput should return a string.");
    }

    private static void ApplyOllamaLowTokenCompatibility(
        Microsoft.Extensions.AI.ChatOptions options,
        Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto.AIModelDto model)
    {
        var method = typeof(PromptResultService).GetMethod(
            "ApplyOllamaLowTokenCompatibility",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        method.Invoke(null, new object[] { options, model, "test" });
    }
}
