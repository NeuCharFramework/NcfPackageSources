/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FormatorPlugin.cs
    文件功能描述：FormatorPlugin 服务逻辑
    
    
    创建标识：Senparc - 20250125
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Microsoft.SemanticKernel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Senparc.AI.Entities;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins
{
    public class FormatorPlugin
    {

        [KernelFunction, LocalizedDescription(typeof(NcfBuiltInResource), "Agents.Plugin.TextLength.Description")]
        public async Task<int> Calc(
            [LocalizedDescription(typeof(NcfBuiltInResource), "Agents.Plugin.OriginalText")]
            string text
            )
        {
            Console.WriteLine("收到原文：" + text);
            return text.Length;
        }
    }

    public class TranslatorPlugin
    {
        [KernelFunction, LocalizedDescription(typeof(NcfBuiltInResource), "Agents.Plugin.Translate.Description")]
        public async Task<string> Translate(
            [LocalizedDescription(typeof(NcfBuiltInResource), "Agents.Plugin.OriginalText")]
            string text,
            [LocalizedDescription(typeof(NcfBuiltInResource), "Agents.Plugin.TargetLanguage")]
            string language
            )
        {

            Console.WriteLine("收到翻译原文：" + text);
            Console.WriteLine("翻译语言：" + language);
            var parameter = new PromptConfigParameter()
            {
                MaxTokens = 3000,
                Temperature = 0.3,
                TopP = 0.3,
            };

            //await Console.Out.WriteLineAsync(localResponse);
            //var remoteResponse = await huggingFaceRemote.CompleteAsync(Input);
            // modelName: "gpt-4-32k"*/
            var _agentAiHandler = new AgentAiHandler(Senparc.AI.Config.SenparcAiSetting);

            // Use AgentKernel-style configuration (similar to PromptOptimizationKernelFallbackService)
            var iWantToRun = _agentAiHandler
                                .IWantTo()
                                .ConfigChatModel("TranslatorPlugin", new ChatClientAgentOptions()
                                {
                                    ChatOptions = new ChatOptions()
                                    {
                                        Instructions = NcfBuiltInResource.Format(
                                            "Agents.Plugin.Translate.Instructions",
                                            "你是一位熟悉“{0}”的专业翻译，请准确翻译用户提供的文本，只输出译文。",
                                            language),
                                        MaxOutputTokens = parameter.MaxTokens > 0 ? (int)parameter.MaxTokens : 3000,
                                        Temperature = (float)parameter.Temperature,
                                        TopP = (float)parameter.TopP
                                    }
                                }).BuildKernel();

            // Create a chat request and run it via the Kernel chat path
            var aiRequest = iWantToRun.CreateRequest(text, iWantToRun.Kernel.AgentSession);
            // ensure request uses replaced prompt (if any templating is used)
            aiRequest.RequestContent = aiRequest.ReplacePrompt();
            var runResult = await iWantToRun.RunChatAsync(aiRequest).ConfigureAwait(false);
            var resultStr = runResult.OutputString;


            Console.WriteLine("翻译结果：" + resultStr);
            return resultStr;
        }
    }
}
