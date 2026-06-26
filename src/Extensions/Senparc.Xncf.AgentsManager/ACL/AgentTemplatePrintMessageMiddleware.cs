using Senparc.CO2NET.Trace;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.ACL
{
    public static class AgentTemplatePrintMessageMiddleware
    {
        public static async Task SendWechatMessageAsync(
            string message,
            AgentTemplateDto agentTemplateDto,
            ChatGroupDto chatGroupDto,
            ChatTaskDto chatTaskDto)
        {
            if (string.IsNullOrWhiteSpace(message) || agentTemplateDto == null)
            {
                return;
            }

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
        }
    }
}
