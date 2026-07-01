/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FormatorPlugin.cs
    文件功能描述：FormatorPlugin 服务逻辑
    
    
    创建标识：Senparc - 20250125
    
    修改标识：Senparc - 20260701
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

        [KernelFunction, Description("获取文本的字符数量")]
        public async Task<int> Calc(
            [Description("原文")]
            string text
            )
        {
            Console.WriteLine("收到原文：" + text);
            return text.Length;
        }
    }

    public class TranslatorPlugin
    {
        [KernelFunction, Description("翻译文本")]
        public async Task<string> Translate(
            [Description("原文")]
            string text,
            [Description("目标语言")]
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
                                        Instructions = @$"你是一个翻译官，你熟悉“{language}”语言，你将帮我完成文本翻译。
原文：
这是一个数据库实体类，用于管理和存储AI模型配置信息。

翻译：
This is a database entity class used to manage and store AI model configuration information.",
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
