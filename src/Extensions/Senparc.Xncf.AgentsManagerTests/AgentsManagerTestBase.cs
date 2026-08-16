using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
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
using System.Net.Http;
using System.Reflection;
using System.Text;

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
        public void RemoteA2AHttpClient_RemovesGlobalResilienceTimeoutRegardlessOfRegistrationOrder()
        {
            var services = new ServiceCollection();
            services.AddHttpClient(RemoteA2AAgentFactory.HttpClientName);
            services.AddHttpClient("unrelated-client");

            var filterType = typeof(RemoteA2AAgentFactory).Assembly.GetType(
                "Senparc.Xncf.AgentsManager.Domain.Services.RemoteA2AHttpMessageHandlerBuilderFilter",
                throwOnError: true);
            var filter = Activator.CreateInstance(filterType!) as IHttpMessageHandlerBuilderFilter;
            Assert.IsNotNull(filter);
            services.AddSingleton(filter);

            // 模拟 NCF Host 的实际顺序：XNCF 先注册，Aspire ServiceDefaults 后注册。
            services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler(
                () => new Microsoft.Extensions.Http.Resilience.ResilienceHandler()));

            using var provider = services.BuildServiceProvider();
            var handlerFactory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
            var a2aHandlerNames = GetHandlerTypeNames(
                handlerFactory.CreateHandler(RemoteA2AAgentFactory.HttpClientName));
            var unrelatedHandlerNames = GetHandlerTypeNames(
                handlerFactory.CreateHandler("unrelated-client"));

            Assert.IsFalse(a2aHandlerNames.Contains(
                "Microsoft.Extensions.Http.Resilience.ResilienceHandler"));
            Assert.IsTrue(unrelatedHandlerNames.Contains(
                "Microsoft.Extensions.Http.Resilience.ResilienceHandler"));
        }

        private static IReadOnlyList<string> GetHandlerTypeNames(HttpMessageHandler handler)
        {
            var names = new List<string>();
            var current = handler;
            while (current != null)
            {
                names.Add(current.GetType().FullName ?? current.GetType().Name);
                current = (current as DelegatingHandler)?.InnerHandler;
            }

            return names;
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
        public void PublishedA2AAgent_UsesLocalAgentExecutionProfileAndPromptParameters()
        {
            var local = AgentTemplateRunRequest.ForLocalWorkflow(5013, "workflow-run", null);
            var a2aWithoutTools = AgentTemplateRunRequest.ForPublishedA2A(5013, "agent-5013", false);
            var a2aWithExplicitTools = AgentTemplateRunRequest.ForPublishedA2A(5013, "agent-5013", true);

            Assert.AreEqual(AgentTemplateRunRequest.LocalWorkflowCompatibleProfile, local.ProfileName);
            Assert.AreEqual(local.ProfileName, a2aWithoutTools.ProfileName);
            Assert.IsTrue(local.UseTemplateModelSettings);
            Assert.IsTrue(local.UseTemplatePromptParameters);
            Assert.IsTrue(a2aWithoutTools.UseTemplateModelSettings);
            Assert.IsTrue(a2aWithoutTools.UseTemplatePromptParameters);
            Assert.IsTrue(local.UseFreshAgentSession);
            Assert.AreEqual(local.UseFreshAgentSession, a2aWithoutTools.UseFreshAgentSession);
            Assert.IsFalse(local.AllowFunctionCalls);
            var localWithApproval = AgentTemplateRunRequest.ForLocalWorkflow(
                5013,
                "workflow-run",
                null,
                true,
                HumanInTheLoopLevel.ToolApproval,
                ToolPermissionMode.RequireApproval,
                ToolPermissionMode.Deny);
            Assert.IsTrue(localWithApproval.AllowFunctionCalls);
            Assert.AreEqual(HumanInTheLoopLevel.ToolApproval, localWithApproval.HumanInTheLoopLevel);
            Assert.AreEqual(ToolPermissionMode.RequireApproval, localWithApproval.PluginToolPermission);
            Assert.AreEqual(ToolPermissionMode.Deny, localWithApproval.McpToolPermission);
            Assert.IsFalse(a2aWithoutTools.AllowFunctionCalls);
            Assert.IsTrue(a2aWithExplicitTools.AllowFunctionCalls);
            Assert.IsFalse(a2aWithoutTools.AllowDeploymentNameModelIdFallback);
            Assert.IsFalse(a2aWithoutTools.EnableModelTransportDiagnostics);
            Assert.IsFalse(local.AllowDeploymentNameModelIdFallback);
            Assert.AreEqual(a2aWithoutTools.MaxOutputTokens, a2aWithExplicitTools.MaxOutputTokens);
            Assert.AreEqual(a2aWithoutTools.Temperature, a2aWithExplicitTools.Temperature);
        }

        [TestMethod]
        public async Task PublishedA2AAgent_Build_InheritsPromptExecutionParameters()
        {
            var templateService = _serviceProvider.GetRequiredService<AgentsTemplateService>();
            var template = await templateService.GetObjectAsync(z => z.Name == "产品经理机器人");
            Assert.IsNotNull(template);

            var runner = _serviceProvider.GetRequiredService<AgentTemplateRunner>();
            var build = await runner.BuildAsync(
                template,
                "test input",
                AgentTemplateRunRequest.ForPublishedA2A(template.Id, "test-agent", false));

            Assert.IsTrue(build.Success, build.ErrorMessage);
            Assert.AreEqual(2000, build.AgentOptions.ChatOptions.MaxOutputTokens);
            Assert.AreEqual(0.7f, build.AgentOptions.ChatOptions.Temperature);
            Assert.AreEqual(0.95f, build.AgentOptions.ChatOptions.TopP);
            Assert.AreEqual(0f, build.AgentOptions.ChatOptions.FrequencyPenalty);
            Assert.AreEqual(0f, build.AgentOptions.ChatOptions.PresencePenalty);
            Assert.AreEqual(0, build.AgentOptions.ChatOptions.StopSequences.Count);
            StringAssert.Contains(build.Diagnostics.ExecutionParameters, "source=prompt:");
            StringAssert.Contains(build.Diagnostics.ExecutionParameters, "maxOutputTokens=2000");
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
        public void PublishedA2AAgent_UsesStrictNonStreamingModelExecution()
        {
            // Published A2A may stream its protocol events, but it must not force a provider-side
            // streaming request. Some Azure-compatible gateways authorise ordinary Chat requests
            // while rejecting streaming routes; this must stay aligned with local Agent execution.
            Assert.IsNotNull(typeof(AgentTemplateRunner).GetMethod(
                "ExecuteBuiltResponseRunnerAsync",
                BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNull(typeof(AgentTemplateRunner).GetMethod(
                "ExecuteBuiltStreamingRunnerAsync",
                BindingFlags.NonPublic | BindingFlags.Static));
        }

        [TestMethod]
        public void PublishedA2AAgent_ConfiguredApiVersionFallback_ReplacesOnlyApiVersion()
        {
            var transportType = typeof(AgentTemplateRunner).Assembly.GetType(
                "Senparc.Xncf.AgentsManager.Domain.Services.PublishedA2AModelTransport",
                throwOnError: true);
            var handlerType = transportType.GetNestedType(
                "PublishedA2AModelHttpMessageHandler",
                BindingFlags.NonPublic);
            Assert.IsNotNull(handlerType);
            var replaceMethod = handlerType.GetMethod(
                "ReplaceApiVersion",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(replaceMethod);

            var result = replaceMethod.Invoke(
                null,
                new object[]
                {
                    new Uri("https://example.invalid/team/openai/deployments/deepseek-chat/chat/completions?api-version=2025-04-01-preview&trace=1"),
                    "2022-12-01"
                }) as Uri;

            Assert.IsNotNull(result);
            Assert.AreEqual("/team/openai/deployments/deepseek-chat/chat/completions", result.AbsolutePath);
            Assert.AreEqual("api-version=2022-12-01&trace=1", result.Query.TrimStart('?'));
        }

        [TestMethod]
        public async Task PublishedA2AAgent_ProviderDiagnostics_DoNotIncludePromptOrCredentials()
        {
            var transportType = typeof(AgentTemplateRunner).Assembly.GetType(
                "Senparc.Xncf.AgentsManager.Domain.Services.PublishedA2AModelTransport",
                throwOnError: true);
            var handlerType = transportType.GetNestedType(
                "PublishedA2AModelHttpMessageHandler",
                BindingFlags.NonPublic);
            Assert.IsNotNull(handlerType);

            var requestSummaryMethod = handlerType.GetMethod(
                "BuildRequestSummaryAsync",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(requestSummaryMethod);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/chat")
            {
                Content = new StringContent(
                    "{\"messages\":[{\"role\":\"system\",\"content\":\"PRIVATE-PROMPT\"},{\"role\":\"user\",\"content\":\"PRIVATE-INPUT\"}],\"tools\":[{}],\"stream\":false,\"max_tokens\":2000,\"temperature\":0.3,\"top_p\":0.3}",
                    Encoding.UTF8,
                    "application/json")
            };
            var summaryTask = requestSummaryMethod.Invoke(
                null,
                new object[] { request, CancellationToken.None }) as Task<string>;
            Assert.IsNotNull(summaryTask);
            var summary = await summaryTask;

            StringAssert.Contains(summary, "messages=2");
            StringAssert.Contains(summary, "roles=system,user");
            StringAssert.Contains(summary, "tools=1");
            StringAssert.Contains(summary, "stream=False");
            Assert.IsFalse(summary.Contains("PRIVATE-PROMPT", StringComparison.Ordinal));
            Assert.IsFalse(summary.Contains("PRIVATE-INPUT", StringComparison.Ordinal));

            var extractProviderErrorMethod = handlerType.GetMethod(
                "ExtractProviderError",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(extractProviderErrorMethod);
            var providerError = extractProviderErrorMethod.Invoke(
                null,
                new object[]
                {
                    "{\"error\":{\"code\":\"Forbidden\",\"type\":\"gateway_policy\",\"message\":\"Authorization: Bearer super-secret-token at https://private.example/path\"},\"echoed_prompt\":\"PRIVATE-PROMPT\"}"
                }) as string;

            StringAssert.Contains(providerError, "code=Forbidden");
            StringAssert.Contains(providerError, "type=gateway_policy");
            Assert.IsFalse(providerError.Contains("super-secret-token", StringComparison.Ordinal));
            Assert.IsFalse(providerError.Contains("private.example", StringComparison.Ordinal));
            Assert.IsFalse(providerError.Contains("PRIVATE-PROMPT", StringComparison.Ordinal));

            var readProviderErrorMethod = handlerType.GetMethod(
                "ReadAndRestoreProviderErrorAsync",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(readProviderErrorMethod);
            const string providerPayload = "{\"error\":{\"code\":\"Forbidden\",\"message\":\"Policy denied\"}}";
            using var response = new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
            {
                Content = new StringContent(providerPayload, Encoding.UTF8, "application/json")
            };
            var providerErrorTask = readProviderErrorMethod.Invoke(
                null,
                new object[] { response, CancellationToken.None }) as Task<string>;
            Assert.IsNotNull(providerErrorTask);
            var restoredProviderError = await providerErrorTask;
            var replayedPayload = await response.Content.ReadAsStringAsync();

            StringAssert.Contains(restoredProviderError, "code=Forbidden");
            Assert.AreEqual(providerPayload, replayedPayload);

            var terminalFailureMethod = handlerType.GetMethod(
                "IsTerminalConfigurationFailure",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(terminalFailureMethod);
            Assert.IsTrue((bool)terminalFailureMethod.Invoke(
                null,
                new object[] { "message=AI 应用不可用或已暂时停用" })!);
            Assert.IsFalse((bool)terminalFailureMethod.Invoke(
                null,
                new object[] { "message=temporary gateway timeout" })!);
        }

        [TestMethod]
        public void PublishedA2AAgent_TerminalProviderFailure_ReturnsActionableMessage()
        {
            var messageMethod = typeof(PublishedA2AAgentFactory).GetMethod(
                "BuildProviderConfigurationFailureMessage",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(messageMethod);

            var message = messageMethod.Invoke(
                null,
                new object[]
                {
                    "AI 应用不可用或已暂时停用。请更新当前 AIModel。",
                    "diagnostic-123"
                }) as string;

            StringAssert.Contains(message, "AI 应用不可用或已暂时停用");
            StringAssert.Contains(message, "diagnostic-123");
        }

        [TestMethod]
        public void AIModelService_ModelRunner_UsesRequestedSettingInsteadOfSystemDefault()
        {
            var createHandlerMethod = typeof(AIModelService).GetMethod(
                "CreateModelAgentHandler",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(createHandlerMethod);

            var requestedSetting = new Senparc.AI.AgentKernel.SenparcAiSetting();
            var handler = createHandlerMethod.Invoke(null, new object[] { requestedSetting })
                as Senparc.AI.AgentKernel.AgentAiHandler;

            Assert.IsNotNull(handler);
            Assert.AreSame(requestedSetting, handler.AgentKernelHelper.AiSetting);
        }

        [TestMethod]
        public void PublishedA2AAgent_StandardTransportFallback_PreservesModelBoundary()
        {
            var source = AgentTemplateRunRequest.ForPublishedA2A(
                5013,
                "agent-5013",
                allowFunctionCalls: true,
                diagnosticId: "diagnostic-id");

            var fallback = source.WithStandardModelTransportFallback();

            Assert.IsFalse(fallback.EnableModelTransportDiagnostics);
            Assert.IsFalse(fallback.AllowDeploymentNameModelIdFallback);
            Assert.AreEqual(source.AiModelId, fallback.AiModelId);
            Assert.AreEqual(source.DefaultSetting, fallback.DefaultSetting);
            Assert.AreEqual(source.UseTemplateModelSettings, fallback.UseTemplateModelSettings);
            Assert.AreEqual(source.UseTemplatePromptParameters, fallback.UseTemplatePromptParameters);
            Assert.AreEqual(source.AllowFunctionCalls, fallback.AllowFunctionCalls);
            Assert.AreEqual(source.MaxOutputTokens, fallback.MaxOutputTokens);
            Assert.AreEqual(source.Temperature, fallback.Temperature);
            Assert.AreEqual(source.TopP, fallback.TopP);
            Assert.AreEqual(source.DiagnosticId, fallback.DiagnosticId);
            StringAssert.Contains(fallback.ProfileName, "standard-transport-fallback");
        }

        [TestMethod]
        public void PublishedA2AAgent_NeuCharLegacyApiVersionFallback_IsAvailableWhenSdkVersionIsConfigured()
        {
            var candidatesMethod = typeof(AgentTemplateRunner).GetMethod(
                "GetApiVersionCompatibilityFallbacks",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(candidatesMethod);

            var candidates = candidatesMethod.Invoke(
                null,
                new object[]
                {
                    new AIModelDto
                    {
                        AiPlatform = Senparc.AI.AiPlatform.NeuCharAI,
                        Endpoint = "https://www.neuchar.com/developer/",
                        ApiVersion = "2025-04-01-preview"
                    },
                    null!
                }) as System.Collections.IEnumerable;

            Assert.IsNotNull(candidates);
            var hasLegacyDefault = false;
            foreach (var candidate in candidates)
            {
                var apiVersion = candidate.GetType().GetProperty("ApiVersion")?.GetValue(candidate) as string;
                var source = candidate.GetType().GetProperty("Source")?.GetValue(candidate) as string;
                if (apiVersion == "2022-12-01" && source == "NeuCharLegacyDefault")
                {
                    hasLegacyDefault = true;
                    break;
                }
            }

            Assert.IsTrue(hasLegacyDefault);
        }

        [TestMethod]
        public void AIModelService_EmptyApiVersion_UsesLegacyCompatibleDefault()
        {
            var getApiVersionOrDefault = typeof(AIModelService).GetMethod(
                "GetApiVersionOrDefault",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(getApiVersionOrDefault);
            Assert.AreEqual("2022-12-01", getApiVersionOrDefault.Invoke(null, new object[] { null! }) as string);
            Assert.AreEqual("2024-10-21", getApiVersionOrDefault.Invoke(null, new object[] { " 2024-10-21 " }) as string);
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

namespace Microsoft.Extensions.Http.Resilience
{
    /// <summary>
    /// 仅用于验证 AgentsManager 按 Handler 完整类名移除宿主默认弹性管道。
    /// 测试项目不增加 Microsoft.Extensions.Http.Resilience 的直接包引用。
    /// </summary>
    internal sealed class ResilienceHandler : DelegatingHandler
    {
    }
}
