using log4net.Util;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.AgentsManagerTests
{
    public class AgentsManagerSeedData : UnitTestSeedDataBuilder
    {
        public override async Task<DataList> ExecuteAsync(IServiceProvider serviceProvider)
        {
            return new DataList("AgentsManagerSeedData");
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
            var promptResultDto = new PromptResultDto(){
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

            var promptResultDto2 = new PromptResultDto(){
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
            var agentService = serviceProvider.GetRequiredService<AgentService>();
            var agent = new Agent("2025.01.17.1", "Agents靶场");
            await agentService.SaveObjectAsync(agent);
            #endregion
        }
    }

    [TestClass]
    public sealed class AgentsManagerTestBase : BaseNcfUnitTest
    {

        public AgentsManagerTestBase(Action<IServiceCollection> servicesRegister = null, UnitTestSeedDataBuilder seedDataBuilder = null) : base(servicesRegister, seedDataBuilder)
        {
        }
    }
}
