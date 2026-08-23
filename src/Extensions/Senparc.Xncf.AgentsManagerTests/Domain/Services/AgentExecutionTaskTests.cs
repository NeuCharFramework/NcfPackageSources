using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class AgentExecutionTaskTests
{
    [TestMethod]
    public void UsageAndTerminalStateAreAggregated()
    {
        var task = new AgentExecutionTask(new AgentExecutionTaskDto
        {
            AgentTemplateId = 7,
            AgentTemplateName = "测试 Agent",
            Name = "独立执行",
            Source = "Workflow",
            PromptCommand = "hello",
            Status = AgentExecutionTask_Status.Waiting,
            StartTime = DateTime.Now
        });

        task.AddUsage(100, 50, 150, 900);
        task.AddUsage(20, 10, 30, 300);
        task.AddToolCall();
        task.ChangeStatus(AgentExecutionTask_Status.Finished);

        var dto = new AgentExecutionTaskDto(task);
        Assert.AreEqual(120, dto.TotalPromptTokens);
        Assert.AreEqual(60, dto.TotalCompletionTokens);
        Assert.AreEqual(180, dto.TotalTokens);
        Assert.AreEqual(1, dto.ToolCallCount);
        Assert.AreEqual(2, dto.ResponseCount);
        Assert.AreEqual(600, dto.AverageResponseMilliseconds);
        Assert.IsNotNull(dto.EndTime);
    }
}
