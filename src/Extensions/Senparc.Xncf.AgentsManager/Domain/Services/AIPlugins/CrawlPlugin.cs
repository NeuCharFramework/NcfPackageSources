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
            /*
            foreach (var item in senMapicResult)
            {
                var urlData = item.Value;
                var rawText = System.Text.RegularExpressions.Regex.Replace(urlData.MarkDownHtmlContent ?? "", @"[#*`_~\[\]()]+|\s+", " ").Trim();
                contentMap.Add(new KeyValuePair<ContentType, string>(ContentType.HtmlContent, rawText));

                var requestSuccess = urlData.Result == 200;

                var logStr = $"Download web content {(requestSuccess ? "Success" : "Failure")}." +
                    (requestSuccess ? $"Number of characters: {urlData.MarkDownHtmlContent?.Length}" : $"Error code: {item.Value.Result}") +
                    $"\t URL:{item.Key.ToLower()}";

                if (!requestSuccess)
                {
                    logStr += $"Source: {urlData.ParentUrl} (Link: {urlData.LinkText})";
                }

                SenparcTrace.SendCustomLog("RAG log", logStr);
                Console.WriteLine(logStr);
            }

            Console.WriteLine("Processing information...");

            #region Embedding Save information

            //Test TextEmbedding
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

#pragma warning disable SKEXP0050 // Type is for evaluation only and may be changed or removed in a future update. Cancel this diagnostic to continue.
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
#pragma warning restore SKEXP0050 // Type is for evaluation only and may be changed or removed in a future update. Cancel this diagnostic to continue.

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
                        Console.WriteLine($"Waiting for cooling {match.Value} seconds");
                    }
                    goto MemoryStore;
                }

            });

            Console.WriteLine($"Processing completed (number of files: {contentMap.Count}, number of paragraphs: {i})");
            #endregion


            #region Ask a question

            Console.WriteLine("Question: " + question);
            StringBuilder results = new StringBuilder();
            var parameter = new PromptConfigParameter()
            {
                MaxTokens = 2000,
                Temperature = 0.7,
                TopP = 0.5,
            };

            var systemMessage = @$"## SystemMessage
You are a consulting robot. You will organize the language based on the "questions" and "alternative information" I provide to generate a reply to me.
""Alternative information"" may have multiple pieces, use ////// to indicate the beginning of each piece of information, and to indicate the end of each piece of information. There will be a number after ****** indicating the relevance of this message.

## Rule
You must:
 - Strictly limit the content of the answer to the alternative information I have provided to you (the content between the opening and closing marks). The earlier the alternative information, the higher the credibility. The relevance does not belong to the answer content itself, so it must be ignored in the process of organizing the language.
 - Please strictly select information related to the "question" from the "optional information" and do not output information without relevant basis. ";

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

            SenparcTrace.SendCustomLog("RAG log", $@"Question: {question}, time consumption: {(DateTime.Now - questionDt).TotalMilliseconds}ms
result:
{results.ToString()}
");

            Console.WriteLine();

            Console.Write("Answer:");

            var input = @$"Question: {question}
Alternative answers:
{results.ToString()}";

            var useStream = iWantToRunChat.IWantToBuild.IWantToConfig.IWantTo.SenparcAiSetting.AiPlatform != AiPlatform.NeuCharAI;
            if (useStream)
            {
                //Use streaming output

                var originalColor = Console.ForegroundColor;//original color
                Action<StreamingKernelContent> streamItemProceessing = async item =>
                {
                    await Console.Out.WriteAsync(item.ToString());

                    //Change color once for each streaming output
                    if (Console.ForegroundColor == originalColor)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = originalColor;
                    }
                };

                //Output results
                SenparcAiResult result = await semanticAiHandler.ChatAsync(iWantToRunChat, input, streamItemProceessing);

                //restore color
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
            */
        }
    }
}
