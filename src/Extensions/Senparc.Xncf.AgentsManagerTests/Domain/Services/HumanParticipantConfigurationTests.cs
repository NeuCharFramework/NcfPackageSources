using Microsoft.Extensions.DependencyInjection;
using Senparc.AI;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManagerTests;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Services;
using System.Reflection;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class HumanParticipantConfigurationTests : AgentsManagerTestBase
{
    [TestMethod]
    public void OllamaSettingCarriesTheConfiguredChatModelName()
    {
        var service = _serviceProvider.GetRequiredService<AIModelService>();
        var setting = service.BuildSenparcAiSetting(
            new AIModelDto
            {
                Alias = "Ollama 测试",
                AiPlatform = AiPlatform.Ollama,
                ConfigModelType = ConfigModelType.Chat,
                ModelId = "qwen3:8b",
                Endpoint = "http://localhost:11434"
            },
            null);

        Assert.AreEqual("qwen3:8b", setting.ModelName.Chat);
        Assert.AreEqual("qwen3:8b", setting.OllamaKeys.ModelName.Chat);
    }

    [TestMethod]
    public async Task RuntimeCanEnsureTheSystemHumanParticipant()
    {
        var agentService = _serviceProvider.GetRequiredService<AgentsTemplateService>();
        var method = typeof(ChatGroupService).GetMethod(
            "EnsureHumanParticipantAsync",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var task = (Task<AgentTemplate>)method.Invoke(
            null,
            new object[] { agentService })!;
        var human = await task;

        Assert.IsTrue(human.IsHuman);
        Assert.IsTrue(human.Enable);
        Assert.AreEqual(HumanParticipantConstants.PromptCode, human.PromptCode);
    }
}
