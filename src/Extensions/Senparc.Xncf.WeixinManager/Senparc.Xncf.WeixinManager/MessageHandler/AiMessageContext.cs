/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AiMessageContext.cs
    File description: AiMessageContext related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.CO2NET.Extensions;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.WeixinManager
{
    public class WechatAiContext : DefaultMpMessageContext
    {
        internal static ConcurrentDictionary<string, IWantToRun> IWantoRunDic = new ConcurrentDictionary<string, IWantToRun>();

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="mpAccountDto"></param>
        /// <returns></returns>
        private static string GetKey(string openId, MpAccountDto mpAccountDto)
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            return $"{mpAccountDto.Id}:{openId}-{mpAccountDto.PromptRangeCode}";
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="mpAccountDto"></param>
        /// <param name="promptConfigParameter"></param>
        /// <param name="modelName"></param>
        /// <param name="openId"></param>
        /// <returns></returns>
        public static async Task<IWantToRun> GetIWantToRunAsync(IServiceProvider services, MpAccountDto mpAccountDto, string openId)
        {
            var key = GetKey(openId, mpAccountDto);

            if (!IWantoRunDic.ContainsKey(key))
            {
                AgentAiHandler _agentAiHandler = (AgentAiHandler)services.GetRequiredService<IAiHandler>();

                using (var scope = services.CreateScope())
                {
                    var promptItemService = scope.ServiceProvider.GetService<PromptItemService>();

                    string promptTemplate = null;
                    SenparcAiSetting senparcAiSetting = null;
                    PromptConfigParameter promptConfigParameter = null;

                    if (!mpAccountDto.PromptRangeCode.IsNullOrEmpty())
                    {
                        //Legacy comment translated to English.
                        //var isAverage = mpAccountDto.PromptRangeCode.Contains("Average", StringComparison.OrdinalIgnoreCase);

                        var promptResult = await promptItemService.GetWithVersionAsync(mpAccountDto.PromptRangeCode, isAvg: true);
                        promptTemplate = promptResult.PromptItem.Content;// Prompt

                        senparcAiSetting = promptResult.SenparcAiSetting;// AI model parameters
                        await Console.Out.WriteLineAsync(senparcAiSetting.ToJson(true));
                        var promptResultDto = promptItemService.Mapper.Map<PromptItemDto>(promptResult.PromptItem);
                        promptConfigParameter = promptItemService.GetPromptConfigParameterFromAiSetting(promptResultDto);
                    }
                    else
                    {
                        promptTemplate = Senparc.AI.DefaultSetting.GetPromptForChat(Senparc.AI.DefaultSetting.DEFAULT_SYSTEM_MESSAGE);
                        promptConfigParameter = new PromptConfigParameter()
                        {
                            MaxTokens = 2000,
                            Temperature = 0.7,
                            TopP = 0.5
                        };
                    }

                    //configurationandinitializemodel
                    var iWantToRun = _agentAiHandler.IWantTo(senparcAiSetting)
                                     .ConfigChatModel(openId, new Microsoft.Agents.AI.ChatClientAgentOptions()
                                     {
                                         ChatOptions = new Microsoft.Extensions.AI.ChatOptions()
                                         {
                                             Instructions = promptTemplate,
                                             MaxOutputTokens = 2000,
                                             Temperature = 0.7f,
                                             TopP = 0.5f
                                         }
                                     }).BuildKernel();
              
                    //var iWantToRun = chatConfig.iWantToRun;

                    //IWantoRunDic.TryAdd(openId, iWantToRun);
                    IWantoRunDic[key] = iWantToRun;
                }
            }

            return IWantoRunDic[key];
        }


        public override void OnRemoved()
        {
            var openId = base.RequestMessages.LastOrDefault()?.FromUserName ?? "I Don't Know";

            if (IWantoRunDic.ContainsKey(openId))
            {
                IWantoRunDic.TryRemove(openId, out var iWantToRun);
            }

            base.OnRemoved();
        }
    }
}
