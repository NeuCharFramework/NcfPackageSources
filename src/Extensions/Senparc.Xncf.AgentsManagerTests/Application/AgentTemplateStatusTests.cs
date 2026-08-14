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
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.PromptRange.Domain.Services;

namespace Senparc.Xncf.AgentsManagerTests.Application;

[TestClass]
public class AgentTemplateStatusTests : AgentsManagerTestBase
{
    private const string ManualPrompt = "你是一位知识库专家，负责从知识库中查找内容，回复问题";

    [TestMethod]
    public async Task GetItemStatus_ShouldReturnAgentOnlyForManualPrompt()
    {
        var agentTemplateService = _serviceProvider.GetRequiredService<AgentsTemplateService>();
        var agentTemplate = new AgentTemplate(
            "知识库专家-手动 Prompt 回归测试",
            ManualPrompt,
            true,
            "",
            ManualPrompt,
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

    [TestMethod]
    public async Task AgentTemplateManage_ShouldStoreManualPromptAsSystemMessage()
    {
        var agentTemplateService = _serviceProvider.GetRequiredService<AgentsTemplateService>();
        var agentTemplate = new AgentTemplate(
            "知识库专家-编辑前",
            "编辑前的系统消息",
            true,
            "",
            "编辑前的系统消息",
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

            var response = await appService.AgentTemplateManage(new AgentTemplate_ManageRequest
            {
                Id = agentTemplate.Id,
                Name = "知识库专家-编辑后",
                SystemMessagePromptCode = ManualPrompt,
                Description = "手动 Prompt 回归测试",
                HookRobotType = HookRobotType.None.ToString(),
                HookRobotParameter = ""
            });

            Assert.IsTrue(response.Success == true, response.ErrorMessage ?? response.Data);
            var updated = await agentTemplateService.GetObjectAsync(z => z.Id == agentTemplate.Id);
            Assert.IsNotNull(updated);
            Assert.AreEqual(ManualPrompt, updated.SystemMessage);
            Assert.AreEqual(ManualPrompt, updated.PromptCode);
        }
        finally
        {
            var saved = await agentTemplateService.GetObjectAsync(z => z.Id == agentTemplate.Id);
            if (saved != null)
            {
                await agentTemplateService.DeleteObjectAsync(saved);
            }
        }
    }

    [TestMethod]
    public async Task AgentTemplateRunner_ShouldUseManualPromptAsInstructions()
    {
        Assert.IsFalse(AgentTemplateRunner.IsPromptRangeReference(ManualPrompt));

        var template = new AgentTemplate(
            "知识库专家-运行回归测试",
            ManualPrompt,
            true,
            "",
            ManualPrompt,
            HookRobotType.None,
            "");
        var runner = _serviceProvider.GetRequiredService<AgentTemplateRunner>();

        var build = await runner.BuildAsync(
            template,
            "测试问题",
            AgentTemplateRunRequest.ForPublishedA2A(template.Id, "manual-prompt-regression", false));

        Assert.IsTrue(build.Success, build.ErrorMessage);
        Assert.AreEqual(ManualPrompt, build.AgentOptions.ChatOptions.Instructions);
    }
}
