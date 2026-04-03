using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
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
        /// Get the unique Key.
        /// Use PromptRangeCode to participate in Key tagging to achieve real-time Prompt updates
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="mpAccountDto"></param>
        /// <returns></returns>
        private static string GetKey(string openId, MpAccountDto mpAccountDto)
        {
            return $"{openId}-{mpAccountDto.PromptRangeCode}";
        }

        /// <summary>
        /// Build and get the IWantToRun object
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
                SemanticAiHandler _semanticAiHandler = (SemanticAiHandler)services.GetRequiredService<IAiHandler>();

                using (var scope = services.CreateScope())
                {
                    var promptItemService = scope.ServiceProvider.GetService<PromptItemService>();

                    string promptTemplate = null;
                    SenparcAiSetting senparcAiSetting = null;
                    PromptConfigParameter promptConfigParameter = null;

                    if (!mpAccountDto.PromptRangeCode.IsNullOrEmpty())
                    {
                        //Range only, automatically selecting the best version
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

                    //Configure and initialize the model
                    var iWantToRun = _semanticAiHandler.ChatConfig(promptConfigParameter,
                                                     userId: openId,
                                                     maxHistoryStore: 20,
                                                     chatSystemMessage: promptTemplate,
                                                     promptTemplate: promptTemplate,
                                                     senparcAiSetting: senparcAiSetting
                                                                                                          /*, modelName: "gpt-4-32k"*/);

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
