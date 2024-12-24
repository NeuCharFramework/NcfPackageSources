using AutoGen.Core;
using Senparc.CO2NET.Trace;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Microsoft.SemanticKernel.ChatCompletion;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.ACL
{
    public static class AgentTemplatePrintMessageMiddleware
    {
        public static Action</*IServiceProvider,*/ IAgent, IMessage, string, AgentTemplateDto,ChatGroupDto,ChatTaskDto> SendWechatMessage => 
            async (/*serviceProvider,*/ agent, replyObject, message, agentTemplateDto, chatGroupDto, chatTaskDto) =>
        {
            string key = null;
            switch (agentTemplateDto.HookRobotType)
            {
                case HookRobotType.None:
                    break;
                case HookRobotType.WeChatMp:
                    break;
                case HookRobotType.WeChatWorkRobot:
                    key = agentTemplateDto.HookRobotParameter;
                    if (key != null)
                    {
                        try
                        {
                            await Weixin.Work.AdvancedAPIs.Webhook.WebhookApi.SendTextAsync(key, message);
                        }
                        catch (Exception ex)
                        {
                            SenparcTrace.BaseExceptionLog(ex);
                        }
                    }
                    break;
                default:
                    break;
            }

            //记录到聊天记录
            //TODO: serviceProvider 是 null
            using (var scope = Senparc.CO2NET.SenparcDI.GetServiceProvider().CreateScope())
            {
                var chatGroupHistoryService = scope.ServiceProvider.GetService<ChatGroupHistoryService>();
                var chatGroupHistoryDto = new ChatGroupHistoryDto(chatGroupDto.Id, chatTaskDto.Id, null, agentTemplateDto.Id, null, agentTemplateDto.Id, null, message, Models.DatabaseModel.Models.MessageType.Text, Models.DatabaseModel.Models.Status.Finished);
                await chatGroupHistoryService.CreateHistory(chatGroupHistoryDto);
            }

           
        };
    }
}
