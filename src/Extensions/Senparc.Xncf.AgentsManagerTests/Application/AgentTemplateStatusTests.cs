/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentTemplateStatusTests.cs
    文件功能描述：Agent 手动 Prompt 状态查询回归测试

    创建标识：Senparc - 20260813

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.OHS.Local.AppService;
using Senparc.Xncf.PromptRange.Domain.Services;

namespace Senparc.Xncf.AgentsManagerTests.Application;

[TestClass]
public class AgentTemplateStatusTests : AgentsManagerTestBase
{
    [TestMethod]
    public async Task GetItemStatus_ShouldReturnAgentOnlyForManualPrompt()
    {
        const string manualPrompt = "你是一位知识库专家，负责从知识库中查找内容，回复问题";
        var agentTemplateService = _serviceProvider.GetRequiredService<AgentsTemplateService>();
        var agentTemplate = new AgentTemplate(
            "知识库专家-手动 Prompt 回归测试",
            manualPrompt,
            true,
            "",
            manualPrompt,
            HookRobotType.None,
            "");

        await agentTemplateService.SaveObjectAsync(agentTemplate);

        try
        {
            var appService = new AgentTemplateAppService(
                _serviceProvider,
                agentTemplateService,
                _serviceProvider.GetRequiredService<PromptItemService>(),
                _serviceProvider.GetRequiredService<PromptRangeService>());

            var response = await appService.GetItemStatus(agentTemplate.Id);

            Assert.IsTrue(response.Success == true, response.ErrorMessage);
            Assert.IsNotNull(response.Data?.AgentTemplateStatus);
            Assert.AreEqual(agentTemplate.Id, response.Data.AgentTemplateStatus.AgentTemplateDto.Id);
            Assert.IsNull(response.Data.AgentTemplateStatus.PromptItemDto);
            Assert.IsNull(response.Data.AgentTemplateStatus.PromptRangeDto);
            Assert.IsNull(response.Data.AgentTemplateStatus.AIModelDto);
        }
        finally
        {
            await agentTemplateService.DeleteObjectAsync(agentTemplate);
        }
    }
}
