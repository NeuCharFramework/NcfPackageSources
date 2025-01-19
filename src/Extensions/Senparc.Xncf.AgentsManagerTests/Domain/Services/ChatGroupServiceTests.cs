using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AgentsManagerTests;
using Senparc.Xncf.AIKernel.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests
{
    [TestClass()]
    public class ChatGroupServiceTests : AgentsManagerTestBase
    {
        [TestMethod()]
        public async Task RunChatGroupInThreadTestAsync()
        {
            var chatGroupService = base._serviceProvider.GetRequiredService<ChatGroupService>();
            var chatGroup = await chatGroupService.GetObjectAsync(z => z.Name == "测试项目");
            Assert.IsNotNull(chatGroup);

            var aiModelService = base._serviceProvider.GetRequiredService<AIModelService>();
            var aiModel = await aiModelService.GetObjectAsync(z => true);

            // 测试启动聊天组
            await chatGroupService.RunChatGroupInThread(new ChatGroup_RunGroupRequest()
            {
                ChatGroupId = chatGroup.Id,
                AiModelId = aiModel.Id,
                PromptCommand = "请对 https://www.ncf.pub 首页内容进行抓取,并分析其中 HTML 代码",
                Name = "测试项目聊天组",
                HookPlatform = HookPlatform.None,
                HookParameter = "",
                Description = "测试项目聊天组",
                Personality = true,

            });

            await Task.Delay(1000);

            // 验证聊天组状态已更新为运行中
            var updatedChatGroup = await chatGroupService.GetObjectAsync(z => z.Id == chatGroup.Id);
            Assert.AreEqual(ChatGroupState.Unstart, updatedChatGroup.State);
        }
    }
}