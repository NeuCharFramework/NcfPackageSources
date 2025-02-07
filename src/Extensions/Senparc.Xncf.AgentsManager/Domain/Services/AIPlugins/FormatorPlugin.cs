using AutoGen.Anthropic.DTO;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
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
                Temperature = 0.7,
                TopP = 0.5,
            };

            //await Console.Out.WriteLineAsync(localResponse);
            //var remoteResponse = await huggingFaceRemote.CompleteAsync(Input);
            // modelName: "gpt-4-32k"*/
            var _semanticAiHandler = new SemanticAiHandler(Senparc.AI.Config.SenparcAiSetting);
            var setting = (SenparcAiSetting)Senparc.AI.Config.SenparcAiSetting;//也可以留空，将自动获取

            var iWantToRun = _semanticAiHandler.ChatConfig(parameter,
                                userId: "Jeffrey",
                                maxHistoryStore: 1,
                                chatSystemMessage: @$"你是一个翻译官，你熟悉“{language}”语言，你将帮我完成文本翻译。
原文：
这是一个数据库实体类，用于管理和存储AI模型配置信息。

翻译：
This is a database entity class used to manage and store AI model configuration information.
",
                                senparcAiSetting: setting);

            SenparcAiRequest aiRequest = iWantToRun.CreateRequest(text);
            var result = await iWantToRun.RunAsync(aiRequest);
            var resultStr = result.OutputString;


            Console.WriteLine("翻译结果：" + resultStr);
            return resultStr;
        }
    }
}
