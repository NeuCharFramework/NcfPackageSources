using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System.Reflection;

namespace Senparc.Xncf.AgentsManagerTests
{
    public class AgentsManagerSeedData : UnitTestSeedDataBuilder
    {
        public override Task<DataList> ExecuteAsync(IServiceProvider serviceProvider)
        {
            return Task.FromResult(new DataList("AgentsManagerSeedData"));
        }

        public override async Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList)
        {
            #region 初始化 PromptRange
            var aiModelService = serviceProvider.GetRequiredService<AIModelService>();
            var promptRangeService = serviceProvider.GetRequiredService<PromptRangeService>();
            var promptItemService = serviceProvider.GetRequiredService<PromptItemService>();
            var promptResultService = serviceProvider.GetRequiredService<PromptResultService>();

            var aiSetting = Senparc.AI.Config.SenparcAiSetting;
            var aiModel = new AIModel(aiSetting.DeploymentName, aiSetting.Endpoint,
              aiSetting.AiPlatform, aiSetting.OrganizationId, aiSetting.ApiKey,
              aiSetting.AzureOpenAIApiVersion, "测试", 4000, "", aiSetting.ModelName.Chat, AIKernel.Domain.Models.ConfigModelType.Chat);
            await aiModelService.SaveObjectAsync(aiModel);

            var promptRange = new PromptRange.Domain.Models.DatabaseModel.PromptRange("2025.01.17.1", "Agents靶场");
            await promptRangeService.SaveObjectAsync(promptRange);

            var promptItemDto = new PromptItemDto()
            {
                RangeId = promptRange.Id,
                RangeName = promptRange.RangeName,
                Tactic = "T1",
                ParentTac = "",
                Aiming = 1,
                NickName = "项目经理",
                Content = @"你是一名项目经理，负责管理和协调软件开发项目，请注意：
- 当需要获取外部资源时，你可以向其他人寻求帮助。
- 你不需要回答任何与协调管理工作无关的内容。
- 你不需要编写任何代码。",
                ModelId = aiModel.Id,
                TopP = 0.95f,
                Temperature = 0.7f,
                MaxToken = 2000,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                StopSequences = "[]",
                Note = "项目经理角色设定",
                IsDraft = true,
                EvalAvgScore = 0,
                EvalMaxScore = 0,
                LastRunTime = DateTime.Now,
                IsShare = false,
                ExpectedResultsJson = "[]",
                Prefix = "",
                Suffix = "",
                VariableDictJson = "{}"
            };

            var promptItem = new PromptItem(promptItemDto);
            await promptItemService.SaveObjectAsync(promptItem);
            var promptResultDto = new PromptResultDto()
            {
                LlmModelId = aiModel.Id,
                PromptItemId = promptItem.Id,
                PromptItemVersion = "1.0",
                ResultString = "测试结果",
                CostTime = 1000,
                RobotScore = 5,
                HumanScore = 0,
                FinalScore = 5,
                RobotTestExceptedResult = "",
                IsRobotTestExactlyEquat = false,
                TestType = TestType.Text,
                PromptCostToken = 100,
                ResultCostToken = 200,
                TotalCostToken = 300
            };
            var promptResult = new PromptResult(promptResultDto);
            await promptResultService.SaveObjectAsync(promptResult);

            var promptItemDto2 = new PromptItemDto()
            {
                RangeId = promptRange.Id,
                RangeName = promptRange.RangeName,
                Tactic = "T1",
                ParentTac = "",
                Aiming = 1,
                NickName = "爬虫",
                Content = @"你是一个爬虫，你负责从互联网上获取信息，并返回给用户。请注意：
- 你应该使用 function call 执行爬虫职责，不想应该编写任何代码。
- 你无需回答任何问题，如果有人向你提出和网络爬虫无关的问题，请让他们找其他更合适的人。",
                ModelId = aiModel.Id,
                TopP = 0.95f,
                Temperature = 0.7f,
                MaxToken = 2000,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                StopSequences = "[]",
                Note = "项目经理角色设定",
                IsDraft = true,
                EvalAvgScore = 0,
                EvalMaxScore = 0,
                LastRunTime = DateTime.Now,
                IsShare = false,
                ExpectedResultsJson = "[]",
                Prefix = "",
                Suffix = "",
                VariableDictJson = "{}"
            };

            var promptItem2 = new PromptItem(promptItemDto2);
            await promptItemService.SaveObjectAsync(promptItem2);

            var promptResultDto2 = new PromptResultDto()
            {
                LlmModelId = aiModel.Id,
                PromptItemId = promptItem2.Id,
                PromptItemVersion = "1.0",
                ResultString = "测试结果",
                CostTime = 1000,
                RobotScore = 5,
                HumanScore = 0,
                FinalScore = 5,
                RobotTestExceptedResult = "",
                IsRobotTestExactlyEquat = false,
                TestType = TestType.Text,
                PromptCostToken = 100,
                ResultCostToken = 200,
                TotalCostToken = 300
            };
            var promptResult2 = new PromptResult(promptResultDto2);
            await promptResultService.SaveObjectAsync(promptResult2);
            #endregion

            #region 初始化 AgentsManager
            //模板
            var agentTemplateService = serviceProvider.GetRequiredService<AgentsTemplateService>();
            var agentTemplate = new AgentTemplate("产品经理机器人", "你是一名产品经理，负责管理和协调软件开发项目，当需要获取外部资源时，你可以向其他人寻求帮助。", true, "", promptItem.FullVersion, HookRobotType.None, "", "");
            await agentTemplateService.SaveObjectAsync(agentTemplate);
            var agentTemplate2 = new AgentTemplate("爬虫机器人", "你是一个爬虫，你负责从互联网上获取信息，并返回给用户。", true, "", promptItem2.FullVersion, HookRobotType.None, "", "", typeof(CrawlPlugin).FullName);
            await agentTemplateService.SaveObjectAsync(agentTemplate2);

            //聊天组
            var chatGroupService = serviceProvider.GetRequiredService<ChatGroupService>();
            var chatGroup = new ChatGroup("测试项目", true, ChatGroupState.Unstart, "测试项目", agentTemplate.Id, agentTemplate.Id);
            await chatGroupService.SaveObjectAsync(chatGroup);

            //聊天组成员
            var chatGroupMemberService = serviceProvider.GetRequiredService<ChatGroupMemberService>();
            var chatGroupMember = new ChatGroupMember(agentTemplate.Id, agentTemplate, chatGroup.Id);
            await chatGroupMemberService.SaveObjectAsync(chatGroupMember);

            var chatGroupMember2 = new ChatGroupMember(agentTemplate2.Id, agentTemplate2, chatGroup.Id);
            await chatGroupMemberService.SaveObjectAsync(chatGroupMember2);
            #endregion

        }
    }

    [TestClass]
    public class AgentsManagerTestBase : BaseNcfUnitTest
    {
        public AgentsManagerTestBase() : base(null, new AgentsManagerSeedData())
        {
        }

        [TestMethod]
        public void PublishedA2AAgent_PublicAgentKey_IsNormalizedAndValidated()
        {
            Assert.AreEqual("product-agent-1", PublishedA2AAgent.NormalizePublicAgentKey(" Product-Agent-1 "));
            Assert.ThrowsException<ArgumentException>(() => PublishedA2AAgent.NormalizePublicAgentKey("product_agent"));
        }

        [TestMethod]
        public void RemoteA2AAgentCardUrl_UsesPathRelativeToTheAgentRoot()
        {
            var splitMethod = typeof(RemoteA2AAgentFactory).GetMethod(
                "SplitAgentCardUrl",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(splitMethod);

            if (splitMethod.Invoke(
                    null,
                    new object[] { new Uri("http://localhost:5000/a2a/agent-5013/.well-known/agent-card.json") })
                is not ValueTuple<Uri, string> result)
            {
                Assert.Fail("Remote Agent Card URL split result is invalid.");
                return;
            }

            Assert.AreEqual("http://localhost:5000/a2a/agent-5013/", result.Item1.AbsoluteUri);
            Assert.AreEqual(".well-known/agent-card.json", result.Item2);
            Assert.AreEqual(
                "http://localhost:5000/a2a/agent-5013/.well-known/agent-card.json",
                new Uri(result.Item1, result.Item2).AbsoluteUri);
        }

        [TestMethod]
        public void PublishedA2AAgent_UpstreamServiceFailure_IsNotPublishedAsAResponse()
        {
            var containsFailureMethod = typeof(PublishedA2AAgentFactory).GetMethod(
                "ContainsServiceFailureSignature",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(containsFailureMethod);

            Assert.IsTrue((bool)containsFailureMethod.Invoke(
                null,
                new object[] { "Service request failed. Status:403 (Forbidden)" })!);
            Assert.IsTrue((bool)containsFailureMethod.Invoke(
                null,
                new object[] { "ClientResultException: StatusCode: 401" })!);
            Assert.IsFalse((bool)containsFailureMethod.Invoke(
                null,
                new object[] { "结论：HTTP 403 需要由用户检查权限。" })!);
        }

        [TestMethod]
        public void PublishedA2AAgent_UsesLocalChatGroupCompatibleExecutionProfile()
        {
            var local = AgentTemplateRunRequest.ForLocalWorkflow(5013, "workflow-run", null);
            var a2aWithoutTools = AgentTemplateRunRequest.ForPublishedA2A(5013, "agent-5013", false);
            var a2aWithExplicitTools = AgentTemplateRunRequest.ForPublishedA2A(5013, "agent-5013", true);

            Assert.AreEqual(AgentTemplateRunRequest.LocalWorkflowCompatibleProfile, local.ProfileName);
            Assert.AreEqual(AgentTemplateRunRequest.LocalChatGroupCompatibleProfile, a2aWithoutTools.ProfileName);
            Assert.AreEqual(2000, a2aWithoutTools.MaxOutputTokens);
            Assert.AreEqual(0.3f, a2aWithoutTools.Temperature);
            Assert.AreEqual(0.3f, a2aWithoutTools.TopP);
            Assert.IsTrue(local.UseFreshAgentSession);
            Assert.AreEqual(local.UseFreshAgentSession, a2aWithoutTools.UseFreshAgentSession);
            Assert.IsFalse(local.AllowFunctionCalls);
            Assert.IsFalse(a2aWithoutTools.AllowFunctionCalls);
            Assert.IsTrue(a2aWithExplicitTools.AllowFunctionCalls);
            Assert.IsTrue(a2aWithoutTools.AllowDeploymentNameModelIdFallback);
            Assert.IsFalse(local.AllowDeploymentNameModelIdFallback);
            Assert.AreEqual(a2aWithoutTools.MaxOutputTokens, a2aWithExplicitTools.MaxOutputTokens);
            Assert.AreEqual(a2aWithoutTools.Temperature, a2aWithExplicitTools.Temperature);
        }

        [TestMethod]
        public void PublishedA2AAgent_DeploymentNameFallback_KeepsTheSameModelBoundary()
        {
            var fallbackMethod = typeof(AgentTemplateRunner).GetMethod(
                "TryBuildAlternateDeploymentModel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(fallbackMethod);

            var source = new AIModelDto
            {
                Id = 8,
                Alias = "published-a2a",
                AiPlatform = Senparc.AI.AiPlatform.NeuCharAI,
                ConfigModelType = AIKernel.Domain.Models.ConfigModelType.Chat,
                ModelId = "deepseek-chat",
                DeploymentName = "gateway-deployment",
                Endpoint = "https://example.invalid/",
                ApiKey = "test-key",
                ApiVersion = "2025-04-01-preview"
            };
            object?[] arguments = { source, null };

            Assert.IsTrue((bool)fallbackMethod.Invoke(null, arguments)!);
            var fallback = arguments[1] as AIModelDto;
            Assert.IsNotNull(fallback);
            Assert.AreEqual(source.ModelId, fallback.DeploymentName);
            Assert.AreEqual(source.ModelId, fallback.ModelId);
            Assert.AreEqual(source.Endpoint, fallback.Endpoint);
            Assert.AreEqual(source.ApiKey, fallback.ApiKey);
            Assert.AreEqual(source.AiPlatform, fallback.AiPlatform);
        }

        [TestMethod]
        public void AgentTemplateRunner_IsSharedByWorkflowAndPublishedA2A()
        {
            Assert.IsNotNull(_serviceProvider.GetRequiredService<AgentTemplateRunner>());
            Assert.IsNotNull(_serviceProvider.GetRequiredService<AgentsWorkflowObjectProvider>());
            Assert.IsNotNull(_serviceProvider.GetRequiredService<PublishedA2AAgentFactory>());
            Assert.IsNotNull(typeof(AgentTemplateRunner).GetMethod(
                nameof(AgentTemplateRunner.RunWithChatClientAgentAsync)));
            Assert.IsNotNull(typeof(PublishedA2AAgentFactory).GetMethod(
                "LogExecutionFailure",
                BindingFlags.NonPublic | BindingFlags.Instance));
        }

        [TestMethod]
        public async Task TestSeedDataInitialization()
        {
            #region 验证 PromptRange 初始化数据
            var aiModelService = _serviceProvider.GetRequiredService<AIModelService>();
            var promptRangeService = _serviceProvider.GetRequiredService<PromptRangeService>();
            var promptItemService = _serviceProvider.GetRequiredService<PromptItemService>();
            var promptResultService = _serviceProvider.GetRequiredService<PromptResultService>();
            
            // 验证 AIModel
            var aiModel = await aiModelService.GetObjectAsync(z => true);
            Assert.IsNotNull(aiModel);
            Assert.AreEqual("测试", aiModel.Note);

            // 验证 PromptRange
            var promptRange = await promptRangeService.GetObjectAsync(z => z.Alias == "Agents靶场");
            Assert.IsNotNull(promptRange);
            Assert.AreEqual("2025.01.17.1", promptRange.RangeName);

            // 验证 PromptItem
            var promptItems = await promptItemService.GetFullListAsync(z => z.RangeId == promptRange.Id);
            Assert.AreEqual(2, promptItems.Count);
            Assert.IsTrue(promptItems.Any(z => z.NickName == "项目经理"));
            Assert.IsTrue(promptItems.Any(z => z.NickName == "爬虫"));


            // 验证 PromptResult
            var promptResults = await promptResultService.GetFullListAsync(z => true);
            Assert.AreEqual(2, promptResults.Count);
            #endregion

            #region 验证 AgentsManager 初始化数据
            var agentTemplateService = _serviceProvider.GetRequiredService<AgentsTemplateService>();
            var chatGroupService = _serviceProvider.GetRequiredService<ChatGroupService>();
            var chatGroupMemberService = _serviceProvider.GetRequiredService<ChatGroupMemberService>();

            // 验证 AgentTemplate
            var templates = await agentTemplateService.GetFullListAsync(z => true);
            Assert.AreEqual(2, templates.Count);
            Assert.IsTrue(templates.Any(z => z.Name == "产品经理机器人"));
            Assert.IsTrue(templates.Any(z => z.Name == "爬虫机器人"));

            var robotTemplate = templates[1];
            Console.WriteLine("爬虫 FunctionCall：" + robotTemplate.FunctionCallNames);

            var functionCallNames = robotTemplate.FunctionCallNames.Split(',');
            Assert.AreEqual("Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins.CrawlPlugin", functionCallNames[0]);

            var aiPlugin = AIPluginHub.Instance;
            var functionCall = aiPlugin.GetPluginType(functionCallNames[0], true);
            Assert.IsNotNull(functionCall);

            // 验证 ChatGroup
            var chatGroup = await chatGroupService.GetObjectAsync(z => z.Name == "测试项目");
            Assert.IsNotNull(chatGroup);
            Assert.IsTrue(chatGroup.Enable);
            Assert.AreEqual(ChatGroupState.Unstart, chatGroup.State);

            // 验证 ChatGroupMember
            var members = await chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == chatGroup.Id);
            Assert.AreEqual(2, members.Count);
            #endregion
        }
    }
}
