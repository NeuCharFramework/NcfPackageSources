using AutoGen.Anthropic.DTO;
using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Text;
using Senparc.AI.Kernel;
using Senparc.AI;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Trace;
using Senparc.Xncf.SenMapic.Domain.SiteMap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Senparc.Xncf.XncfBuilder.Domain.Services.Plugins.FilePlugin;
using Senparc.CO2NET.Extensions;
using System.IO;
using Senparc.AI.Entities;

namespace Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins
{
    public enum ContentType
    {
        File,
        HtmlContent
    }

    public class CrawlPlugin
    {
        //private readonly IWantToRun _iWantToRun;
        private readonly IServiceProvider _serviceProvider;

        public CrawlPlugin(/*IWantToRun iWantToRun, */IServiceProvider serviceProvider)
        {
            //this._iWantToRun = iWantToRun;
            this._serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("爬取网页信息并返回提问信息")]
        public async Task<string> Crawl(
            [Description("爬取网址")]
            string url,
            [Description("最大爬取深度")]
            int maxDeepth,
            [Description("最大爬取页数")]
            int maxPageCount,
            [Description("提问")]
            string question
         )
        {
            List<KeyValuePair<ContentType, string>> contentMap = new List<KeyValuePair<ContentType, string>>();

            Console.WriteLine($"Crawl 爬取：{url}，深度：{maxDeepth}，最大页面数：{maxPageCount}");

            var senMapicEngine = new SenMapicEngine(
                                serviceProvider: _serviceProvider,
                                urls: new[] { url },
                                maxThread: 20,
                                maxBuildMinutesForSingleSite: 5,
                                maxDeep: maxDeepth,
                                maxPageCount: maxPageCount);

            var senMapicResult = senMapicEngine.Build();

            return senMapicResult.Values.FirstOrDefault()?.MarkDownHtmlContent;

            foreach (var item in senMapicResult)
            {
                var urlData = item.Value;
                var rawText = System.Text.RegularExpressions.Regex.Replace(urlData.MarkDownHtmlContent ?? "", @"[#*`_~\[\]()]+|\s+", " ").Trim();
                contentMap.Add(new KeyValuePair<ContentType, string>(ContentType.HtmlContent, rawText));

                var requestSuccess = urlData.Result == 200;

                var logStr = $"下载网页内容{(requestSuccess ? "成功" : "失败")}。" +
                    (requestSuccess ? $"字符数：{urlData.MarkDownHtmlContent?.Length}" : $"错误代码：{item.Value.Result}") +
                    $"\t URL:{item.Key.ToLower()}";

                if (!requestSuccess)
                {
                    logStr += $" 来源：{urlData.ParentUrl} （链接：{urlData.LinkText}）";
                }

                SenparcTrace.SendCustomLog("RAG日志", logStr);
                Console.WriteLine(logStr);
            }

            Console.WriteLine("正在处理信息...");

            #region Embedding 储存信息

            //测试 TextEmbedding
            var embeddingAiSetting = Senparc.AI.Config.SenparcAiSetting;
            var semanticAiHandler = new SemanticAiHandler(embeddingAiSetting);
            var iWantToRunEmbedding = semanticAiHandler
                 .IWantTo()
                 .ConfigModel(ConfigModel.TextEmbedding, "Jeffrey")
                 .BuildKernel();

            var i = 0;
            var dt = SystemTime.Now;
            var mapTasks = new List<Task>();

            contentMap.ForEach(file =>
            {
                if (file.Value.IsNullOrEmpty())
                {
                    return;
                }

                var text = file.Key == ContentType.File ? File.ReadAllTextAsync(file.Value).Result : file.Value;
                List<string> paragraphs = new List<string>();

#pragma warning disable SKEXP0050 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                if (file.Value.EndsWith(".md"))
                {
                    paragraphs = TextChunker.SplitMarkdownParagraphs(
                      TextChunker.SplitMarkDownLines(System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Replace("\r\n", " "), 128),
                      64);
                }
                else
                {
                    paragraphs = TextChunker.SplitPlainTextParagraphs(
                      TextChunker.SplitPlainTextLines(System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Replace("\r\n", " "), 128),
                      256);
                }
#pragma warning restore SKEXP0050 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。

            MemoryStore:
                try
                {
                    paragraphs.ForEach(paragraph =>
                    {
                        var currentI = i++;

                        iWantToRunEmbedding
                          .MemorySaveInformation(
                              modelName: embeddingAiSetting.ModelName.Embedding,
                              azureDeployName: embeddingAiSetting.DeploymentName,
                              collection: "EmbeddingAgent",
                              id: $"paragraph-{Guid.NewGuid().ToString("n")}",
                              text: paragraph);
                    });
                }
                catch (Exception ex)
                {
                    string pattern = @"retry after (\d+) seconds";
                    Match match = Regex.Match(ex.Message, pattern);
                    if (match.Success)
                    {
                        Console.WriteLine($"等待冷却 {match.Value} 秒");
                    }
                    goto MemoryStore;
                }

            });

            Console.WriteLine($"处理完成(文件数：{contentMap.Count}，段落数：{i})");
            #endregion


            #region 提问

            Console.WriteLine("提问：" + question);
            StringBuilder results = new StringBuilder();
            var parameter = new PromptConfigParameter()
            {
                MaxTokens = 2000,
                Temperature = 0.7,
                TopP = 0.5,
            };

            var systemMessage = @$"## SystemMessage
你是一位咨询机器人，你将根据我所提供的“提问”以及“备选信息”组织语言，生成一段给我的回复。
""备选信息""可能有多条，使用 ////// 表示每一条信息的开头， 表示每一条信息的结尾。在 ****** 后会有一个数字，表示这条信息的相关性。

## Rule
你必须：
 - 将回答内容严格限制在我所提供给你的备选信息中（开头和结尾标记中间的内容），其中越靠前的备选信息可信度越高，相关性不属于答案内容本身，因此在组织语言的过程中必须将其忽略。
 - 请严格从“备选信息”中挑选和“提问”有关的信息，不要输出没有相关依据的信息。";

            var iWantToRunChat = semanticAiHandler.ChatConfig(parameter,
                                 userId: "Jeffrey",
                                 maxHistoryStore: 10,
                                 chatSystemMessage: systemMessage,
                                 senparcAiSetting: null);

            var questionDt = DateTime.Now;
            var limit = 3;
            var embeddingResult = await iWantToRunEmbedding.MemorySearchAsync(
                    modelName: embeddingAiSetting.ModelName.Embedding,
                    azureDeployName: embeddingAiSetting.DeploymentName,
                    memoryCollectionName: "EmbeddingAgent",
                    query: question,
                    limit: limit);

            await foreach (var item in embeddingResult.MemoryQueryResult)
            {
                results.AppendLine($@"//////
{item.Metadata.Text}
******{item.Relevance}");
            }

            SenparcTrace.SendCustomLog("RAG日志", $@"提问：{question}，耗时：{(DateTime.Now - questionDt).TotalMilliseconds}ms
结果：
{results.ToString()}
");

            Console.WriteLine();

            Console.Write("回答：");

            var input = @$"提问：{question}
备选答案：
{results.ToString()}";

            var useStream = iWantToRunChat.IWantToBuild.IWantToConfig.IWantTo.SenparcAiSetting.AiPlatform != AiPlatform.NeuCharAI;
            if (useStream)
            {
                //使用流式输出

                var originalColor = Console.ForegroundColor;//原始颜色
                Action<StreamingKernelContent> streamItemProceessing = async item =>
                {
                    await Console.Out.WriteAsync(item.ToString());

                    //每个流式输出改变一次颜色
                    if (Console.ForegroundColor == originalColor)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = originalColor;
                    }
                };

                //输出结果
                SenparcAiResult result = await semanticAiHandler.ChatAsync(iWantToRunChat, input, streamItemProceessing);

                //复原颜色
                Console.ForegroundColor = originalColor;

                return result.OutputString;
            }
            else
            {
                //iWantToRunChat
                var result = await semanticAiHandler.ChatAsync(iWantToRunChat, input);
                await Console.Out.WriteLineAsync(result.OutputString);

                return result.OutputString;
            }

            #endregion
        }
    }
}
