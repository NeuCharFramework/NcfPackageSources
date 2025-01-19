using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

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
                Content = "你是一名项目经理，负责管理和协调软件开发项目，当需要获取外部资源时，你可以向其他人寻求帮助。",
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
                Content = "你是一个爬虫，你负责从互联网上获取信息，并返回给用户。",
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
            var agentTemplate = new AgentTemplate("产品经理机器人", "你是一名产品经理，负责管理和协调软件开发项目，当需要获取外部资源时，你可以向其他人寻求帮助。", false, "", promptItem.FullVersion, HookRobotType.None, "", "");
            await agentTemplateService.SaveObjectAsync(agentTemplate);
            var agentTemplate2 = new AgentTemplate("爬虫机器人", "你是一个爬虫，你负责从互联网上获取信息，并返回给用户。", false, "", promptItem2.FullVersion, HookRobotType.None, "", "");
            await agentTemplateService.SaveObjectAsync(agentTemplate2);

            //聊天组
            var chatGroupService = serviceProvider.GetRequiredService<ChatGroupService>();
            var chatGroup = new ChatGroup("测试项目", true, ChatGroupState.Unstart, "测试项目", agentTemplate.Id, agentTemplate2.Id);
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
            Assert.AreEqual("测试", aiModel.Note );

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
