using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Application.Dtos;
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
            RequireRealAiBackend(nameof(RunChatGroupInThreadTestAsync));
            var chatGroupService = base._serviceProvider.GetRequiredService<ChatGroupService>();
            var chatGroup = await chatGroupService.GetObjectAsync(z => z.Name == "测试项目");
            Assert.IsNotNull(chatGroup);

            var aiModelService = base._serviceProvider.GetRequiredService<AIModelService>();
            var aiModel = await aiModelService.GetObjectAsync(z => true);

            //Legacy comment translated to English.
            await chatGroupService.RunChatGroupInThread(new ChatGroupRunCommand()
            {
                ChatGroupId = chatGroup.Id,
                AiModelId = aiModel.Id,
                PromptCommand = "请对 https://www.ncf.pub 进行分析，告诉我这个站点的概要信息，以及所表述的产品的特点。",
                Name = "测试项目聊天组",
                HookPlatform = HookPlatform.None,
                HookParameter = "",
                Description = "测试项目聊天组",
                Personality = true,
            });


            ChatTask? chatTask = null;
            for (int i = 0; i < 80; i++)
            {
                //Legacy comment translated to English.
                await Task.Delay(1000);

                //Legacy comment translated to English.
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