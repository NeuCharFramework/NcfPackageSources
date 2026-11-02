using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.Entities.Keys;
using Senparc.AI.Interfaces;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class AgentModelFallbackResolverTests
{
    [TestMethod]
    public void ContainsForbiddenStatus_ShouldDetect403VariantsAndInnerChain()
    {
        Assert.IsFalse(AgentModelFallbackResolver.ContainsForbiddenStatus(null));
        Assert.IsFalse(AgentModelFallbackResolver.ContainsForbiddenStatus(new Exception("普通错误")));

        Assert.IsTrue(AgentModelFallbackResolver.ContainsForbiddenStatus(new Exception("Status: 403 (Forbidden)")));
        Assert.IsTrue(AgentModelFallbackResolver.ContainsForbiddenStatus(new Exception("StatusCode: 403")));
        Assert.IsTrue(AgentModelFallbackResolver.ContainsForbiddenStatus(new Exception("HTTP 403 Forbidden")));
        Assert.IsTrue(AgentModelFallbackResolver.ContainsForbiddenStatus(new Exception("请求被禁止，状态码 403")));
        Assert.IsTrue(AgentModelFallbackResolver.ContainsForbiddenStatus(
            new Exception("外层包装", new Exception("StatusCode: 403"))));
    }

    [TestMethod]
    public void ContainsServiceFailureSignature_ShouldMatch403And401Signatures()
    {
        Assert.IsFalse(AgentModelFallbackResolver.ContainsServiceFailureSignature(null));
        Assert.IsFalse(AgentModelFallbackResolver.ContainsServiceFailureSignature("正常输出"));
        Assert.IsTrue(AgentModelFallbackResolver.ContainsServiceFailureSignature("Service request failed. Status: 403 (Forbidden)"));
        Assert.IsTrue(AgentModelFallbackResolver.ContainsServiceFailureSignature("ClientResultException: StatusCode: 401"));
        Assert.IsTrue(AgentModelFallbackResolver.ContainsServiceFailureSignature("Status: 403 (Forbidden)"));
    }

    [TestMethod]
    public void FlattenExceptionMessages_ShouldJoinExceptionChain()
    {
        var message = AgentModelFallbackResolver.FlattenExceptionMessages(
            new Exception("外层", new Exception("内层")));

        Assert.IsTrue(message.Contains("外层"));
        Assert.IsTrue(message.Contains("内层"));
        Assert.IsTrue(message.Contains("->"));
    }

    [TestMethod]
    public void TryBuildAlternateDeploymentModel_ShouldSwapDeploymentForModelId()
    {
        var model = new AIModelDto
        {
            Id = 7,
            AiPlatform = AiPlatform.AzureOpenAI,
            ModelId = "gpt-4o",
            DeploymentName = "deployment-1",
            ApiKey = "key"
        };

        Assert.IsTrue(AgentModelFallbackResolver.TryBuildAlternateDeploymentModel(model, out var fallback));
        Assert.IsNotNull(fallback);
        Assert.AreEqual("gpt-4o", fallback.DeploymentName);
        Assert.AreEqual("gpt-4o", fallback.ModelId);
        Assert.AreEqual(model.Id, fallback.Id);
        Assert.AreEqual(model.ApiKey, fallback.ApiKey);
    }

    [TestMethod]
    public void TryBuildAlternateDeploymentModel_ShouldRejectSameDeploymentOrOtherPlatform()
    {
        Assert.IsFalse(AgentModelFallbackResolver.TryBuildAlternateDeploymentModel(
            new AIModelDto
            {
                AiPlatform = AiPlatform.AzureOpenAI,
                ModelId = "gpt-4o",
                DeploymentName = "gpt-4o"
            }, out _));

        Assert.IsFalse(AgentModelFallbackResolver.TryBuildAlternateDeploymentModel(
            new AIModelDto
            {
                AiPlatform = AiPlatform.OpenAI,
                ModelId = "gpt-4o",
                DeploymentName = "deployment-1"
            }, out _));

        Assert.IsFalse(AgentModelFallbackResolver.TryBuildAlternateDeploymentModel(null, out _));
    }

    [TestMethod]
    public void GetApiVersionCompatibilityFallbacks_ShouldExcludeMafDefaultAndAddNeuCharLegacy()
    {
        var model = new AIModelDto
        {
            AiPlatform = AiPlatform.NeuCharAI,
            Endpoint = "https://www.neuchar.com/",
            ApiVersion = "2025-04-01-preview"
        };
        ISenparcAiSetting setting = new SenparcAiSetting
        {
            AiPlatform = AiPlatform.NeuCharAI,
            NeuCharAIKeys = new NeuCharAIKeys
            {
                NeuCharAIApiVersion = "2024-06-01"
            }
        };

        var candidates = AgentModelFallbackResolver.GetApiVersionCompatibilityFallbacks(model, setting);

        Assert.IsFalse(candidates.Any(z => z.ApiVersion == "2025-04-01-preview"));
        Assert.IsTrue(candidates.Any(z => z.ApiVersion == "2024-06-01" && z.Source == "EffectiveSetting"));
        Assert.IsTrue(candidates.Any(z => z.ApiVersion == "2022-12-01" && z.Source == "NeuCharLegacyDefault"));
    }

    [TestMethod]
    public void GetApiVersionCompatibilityFallbacks_ShouldReturnEmptyForUnsupportedPlatform()
    {
        var model = new AIModelDto
        {
            AiPlatform = AiPlatform.OpenAI,
            ApiVersion = "2024-06-01"
        };
        ISenparcAiSetting setting = new SenparcAiSetting
        {
            AiPlatform = AiPlatform.OpenAI
        };

        Assert.AreEqual(0, AgentModelFallbackResolver.GetApiVersionCompatibilityFallbacks(model, setting).Count);
    }

    [TestMethod]
    public void GetConfiguredApiVersion_ShouldPreferModelThenSetting()
    {
        ISenparcAiSetting setting = new SenparcAiSetting
        {
            AiPlatform = AiPlatform.AzureOpenAI,
            AzureOpenAIKeys = new AzureOpenAIKeys
            {
                AzureOpenAIApiVersion = "2024-06-01"
            }
        };

        Assert.AreEqual(
            "2024-02-01",
            AgentModelFallbackResolver.GetConfiguredApiVersion(new AIModelDto { AiPlatform = AiPlatform.AzureOpenAI, ApiVersion = " 2024-02-01 " }, setting));
        Assert.AreEqual(
            "2024-06-01",
            AgentModelFallbackResolver.GetConfiguredApiVersion(new AIModelDto { AiPlatform = AiPlatform.AzureOpenAI }, setting));
        Assert.AreEqual(
            "unset",
            AgentModelFallbackResolver.GetConfiguredApiVersion(new AIModelDto { AiPlatform = AiPlatform.AzureOpenAI }, null));
    }

    [TestMethod]
    public void CanUseDefaultChatFallback_ShouldValidateByPlatform()
    {
        Assert.IsFalse(AgentModelFallbackResolver.CanUseDefaultChatFallback(null));
        Assert.IsTrue(AgentModelFallbackResolver.CanUseDefaultChatFallback(new SenparcAiSetting
        {
            AiPlatform = AiPlatform.OpenAI,
            OpenAIKeys = new OpenAIKeys
            {
                ApiKey = "key",
                ModelName = new ModelName { Chat = "gpt-4o" }
            }
        }));
        Assert.IsFalse(AgentModelFallbackResolver.CanUseDefaultChatFallback(new SenparcAiSetting
        {
            AiPlatform = AiPlatform.OpenAI,
            OpenAIKeys = new OpenAIKeys
            {
                ModelName = new ModelName { Chat = "gpt-4o" }
            }
        }));
        Assert.IsTrue(AgentModelFallbackResolver.CanUseDefaultChatFallback(new SenparcAiSetting
        {
            AiPlatform = AiPlatform.Ollama,
            OllamaKeys = new OllamaKeys
            {
                Endpoint = "http://localhost:11434",
                ModelName = new ModelName { Chat = "llama3" }
            }
        }));
        Assert.IsFalse(AgentModelFallbackResolver.CanUseDefaultChatFallback(new SenparcAiSetting
        {
            AiPlatform = AiPlatform.Ollama,
            OllamaKeys = new OllamaKeys
            {
                ModelName = new ModelName { Chat = "llama3" }
            }
        }));
    }

    [TestMethod]
    public void BuildModelDtoFromSetting_ShouldMapNeuCharFields()
    {
        var setting = new SenparcAiSetting
        {
            AiPlatform = AiPlatform.NeuCharAI,
            NeuCharAIKeys = new NeuCharAIKeys
            {
                ApiKey = "key",
                NeuCharAIApiVersion = "2024-06-01",
                NeuCharEndpoint = "https://www.neuchar.com",
                ModelName = new ModelName { Chat = "gpt-4o" }
            }
        };

        var model = AgentModelFallbackResolver.BuildModelDtoFromSetting(setting, "TestAlias");

        Assert.IsNotNull(model);
        Assert.AreEqual("TestAlias", model.Alias);
        Assert.AreEqual(AiPlatform.NeuCharAI, model.AiPlatform);
        Assert.AreEqual(ConfigModelType.Chat, model.ConfigModelType);
        Assert.AreEqual("gpt-4o", model.ModelId);
        Assert.AreEqual("gpt-4o", model.DeploymentName);
        Assert.AreEqual("https://www.neuchar.com", model.Endpoint);
        Assert.AreEqual("key", model.ApiKey);
        Assert.AreEqual("2024-06-01", model.ApiVersion);
    }

    [TestMethod]
    public void IsSameChatConfig_ShouldTreatTrailingSlashAsSameEndpoint()
    {
        var left = new AIModelDto
        {
            AiPlatform = AiPlatform.NeuCharAI,
            Endpoint = "https://www.neuchar.com/",
            ModelId = "gpt-4o",
            DeploymentName = "gpt-4o",
            ApiKey = "key"
        };
        var right = new AIModelDto
        {
            AiPlatform = AiPlatform.NeuCharAI,
            Endpoint = "https://www.neuchar.com",
            ModelId = "gpt-4o",
            DeploymentName = "gpt-4o",
            ApiKey = "key"
        };
        var differentModel = new AIModelDto
        {
            AiPlatform = AiPlatform.NeuCharAI,
            Endpoint = "https://www.neuchar.com",
            ModelId = "gpt-4o-mini",
            DeploymentName = "gpt-4o",
            ApiKey = "key"
        };

        Assert.IsTrue(AgentModelFallbackResolver.IsSameChatConfig(left, right));
        Assert.IsFalse(AgentModelFallbackResolver.IsSameChatConfig(left, differentModel));
        Assert.IsFalse(AgentModelFallbackResolver.IsSameChatConfig(left, null));
    }

    [TestMethod]
    public void BuildModelDiagnosticInfo_ShouldNotLeakApiKey()
    {
        var info = AgentModelFallbackResolver.BuildModelDiagnosticInfo(new AIModelDto
        {
            Id = 7,
            ModelId = "gpt-4o",
            AiPlatform = AiPlatform.AzureOpenAI,
            ApiKey = "super-secret"
        });

        Assert.IsTrue(info.Contains("AIModelDbId=7"));
        Assert.IsFalse(info.Contains("super-secret"));
        Assert.IsTrue(info.Contains("set(len:12)"));
        Assert.AreEqual("模型配置为空（AIModelDto == null）", AgentModelFallbackResolver.BuildModelDiagnosticInfo(null));
    }

    [TestMethod]
    public void GetEndpointHost_ShouldHandleEmptyAndInvalidEndpoints()
    {
        Assert.AreEqual("unset", AgentModelFallbackResolver.GetEndpointHost(null));
        Assert.AreEqual("www.neuchar.com", AgentModelFallbackResolver.GetEndpointHost("https://www.neuchar.com/v1"));
        Assert.AreEqual("custom", AgentModelFallbackResolver.GetEndpointHost("not-an-uri"));
    }
}
