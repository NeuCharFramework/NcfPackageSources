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

            // Test launch chat group
            await chatGroupService.RunChatGroupInThread(new ChatGroup_RunGroupRequest()
            {
                ChatGroupId = chatGroup.Id,
                AiModelId = aiModel.Id,
                PromptCommand = "Please use https://www.ncf.pub Do an analysis and tell me an overview of the site and the features of the product being represented.",
                Name = "Test project chat group",
                HookPlatform = HookPlatform.None,
                HookParameter = "",
                Description = "Test project chat group",
                Personality = true,
            });


            ChatTask? chatTask = null;
            for (int i = 0; i < 80; i++)
            {
                // Verify that the chat group status has been updated to Running
                await Task.Delay(1000);

                //Note: The CreateAsyncScope() method is used here because there are other threads reading and writing ChatTask, and the cache here may not be able to read the latest updates.
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

            Assert.AreEqual(ChatTask_Status.Finished, chatTask?.Status);
        }
    }
}