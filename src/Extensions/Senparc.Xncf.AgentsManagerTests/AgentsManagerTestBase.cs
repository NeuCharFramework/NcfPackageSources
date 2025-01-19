using log4net.Util;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;

namespace Senparc.Xncf.AgentsManagerTests
{
    public class AgentsManagerSeedData : UnitTestSeedDataBuilder
    {
        public override async Task<DataList> ExecuteAsync(IServiceProvider serviceProvider)
        {
            return new DataList("AgentsManagerSeedData");
        }

        public override Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList)
        {
            var aiModelService = serviceProvider.GetService<PromptRangeService>();
            var promptRangeService = serviceProvider.GetService<PromptRangeService>();

            var aiSetting = Senparc.AI.Config.SenparcAiSetting;
            var aiModel = new AIModel(aiSetting.DeploymentName, aiSetting.Endpoint,
              aiSetting.AiPlatform, aiSetting.OrganizationId, aiSetting.ApiKey,
              aiSetting.AzureOpenAIApiVersion, "测试", 4000, "", aiSetting.ModelName.Chat, AIKernel.Domain.Models.ConfigModelType.Chat);

            var promptRange = new PromptRange.Domain.Models.DatabaseModel.PromptRange("2025.01.17.1", "Agents靶场");



            //var promptItem = new PromptItem()


            return Task.CompletedTask;
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
