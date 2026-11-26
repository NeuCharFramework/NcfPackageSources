/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentTemplatePrintMessageMiddleware.cs
    File description: Related implementation.

    Created by: Senparc - 20241017

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

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
        /// <summary>
        /// Sends an Agent result to the configured WeChat Work robot hook when enabled.
        /// Unsupported hook types and missing configuration are ignored; provider failures are logged.
        /// </summary>
        /// <param name="message">The message content to send.</param>
        /// <param name="agentTemplateDto">The Agent template containing hook settings.</param>
        /// <param name="chatGroupDto">The ChatGroup context.</param>
        /// <param name="chatTaskDto">The ChatTask context.</param>
        /// <returns>A task completing after the optional send attempt.</returns>
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
