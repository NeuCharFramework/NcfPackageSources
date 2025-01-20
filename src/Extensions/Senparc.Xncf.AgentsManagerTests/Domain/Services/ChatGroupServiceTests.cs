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
                PromptCommand = "请对 https://ld.suzhou.edu.cn 进行分析，告诉我这所学校的概况，以及校训，搜索最多80个页面，找出“金波”老师的信息。",
                Name = "测试项目聊天组",
                HookPlatform = HookPlatform.None,
                HookParameter = "",
                Description = "测试项目聊天组",
                Personality = true,
            });


            ChatTask chatTask = null;
            for (int i = 0; i < 80; i++)
            {
                // 验证聊天组状态已更新为运行中
                await Task.Delay(1000);

                //说明：此处使用 CreateAsyncScope() 方法是由于还有其他线程在读写 ChatTask，此处缓存可能读取不到最新的更新
                await using (var scope = base._serviceProvider.CreateAsyncScope())
                {
                    var chatTaskService = scope.ServiceProvider.GetRequiredService<ChatTaskService>();
                    chatTask = await chatTaskService.GetObjectAsync(z => true, z => z, Ncf.Core.Enums.OrderingType.Descending);
                    if (chatTask != null && chatTask.Status == ChatTask_Status.Finished)
                    {
                        break;
                    }
                }
               
            }

            Assert.AreEqual(ChatTask_Status.Finished, chatTask.Status);
        }
    }
}