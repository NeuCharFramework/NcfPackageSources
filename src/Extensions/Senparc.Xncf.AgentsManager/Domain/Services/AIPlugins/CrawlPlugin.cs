/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CrawlPlugin.cs
    File description: Related implementation.

    Created by: Senparc - 20250117

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260717
    Change description: v0.12.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Text;
using Senparc.AI.AgentKernel;
using Senparc.AI;
using Senparc.AI.AgentKernel.Handlers;
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

        [KernelFunction, LocalizedDescription(typeof(AgentsManagerResource), "Agents.Plugin.Crawl.Description")]
        public Task<string> Crawl(
            [LocalizedDescription(typeof(AgentsManagerResource), "Agents.Plugin.Crawl.Url")]
            string url,
            [LocalizedDescription(typeof(AgentsManagerResource), "Agents.Plugin.Crawl.Depth")]
            int maxDepth = 1,
            [LocalizedDescription(typeof(AgentsManagerResource), "Agents.Plugin.Crawl.PageCount")]
            int maxPageCount = 5,
            [LocalizedDescription(typeof(AgentsManagerResource), "Agents.Plugin.Crawl.Question")]
            string question = ""
         )
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri)
                || (targetUri.Scheme != Uri.UriSchemeHttp && targetUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Crawl 需要有效的 HTTP 或 HTTPS URL。", nameof(url));
            }

            maxDepth = Math.Clamp(maxDepth, 0, 3);
            maxPageCount = Math.Clamp(maxPageCount, 1, 20);
            _ = question;
            List<KeyValuePair<ContentType, string>> contentMap = new List<KeyValuePair<ContentType, string>>();

            Console.WriteLine($"Crawl 爬取：{targetUri}，深度：{maxDepth}，最大页面数：{maxPageCount}");
            SenparcTrace.SendCustomLog(
                "AgentsManager.Crawl.Start",
                $"Url={targetUri}; MaxDepth={maxDepth}; MaxPageCount={maxPageCount}");

            var senMapicEngine = new SenMapicEngine(
                                serviceProvider: _serviceProvider,
                                urls: new[] { targetUri.ToString() },
                                maxThread: 4,
                                maxBuildMinutesForSingleSite: 5,
                                maxDeep: maxDepth,
                                maxPageCount: maxPageCount);

            var senMapicResult = senMapicEngine.Build();

            var result = senMapicResult.Values.FirstOrDefault()?.MarkDownHtmlContent ?? string.Empty;
            SenparcTrace.SendCustomLog(
                "AgentsManager.Crawl.Completed",
                $"Url={targetUri}; Pages={senMapicResult.Count}; ContentLength={result.Length}");
            return Task.FromResult(result);
            /*
            foreach (var item in senMapicResult)

            {

                var urlData = item.Value;

                var rawText = System.Text.RegularExpressions.Regex.Replace(urlData.MarkDownHtmlContent ?? "", @"[#*`_~\[\]()]+|\s+", " ").Trim();

                contentMap.Add(new KeyValuePair<ContentType, string>(ContentType.HtmlContent, rawText));


                var requestSuccess = urlData.Result == 200;


                Legacy comment translated to English.

                    Legacy comment translated to English.

                    $"\t URL:{item.Key.ToLower()}";


                if (!requestSuccess)

                {

                    Legacy comment translated to English.

                }


                Legacy comment translated to English.

                Console.WriteLine(logStr);

            }


            Legacy comment translated to English.


            Legacy section


            //test TextEmbedding

            var embeddingAiSetting = Senparc.AI.Config.SenparcAiSetting;

            var agentAiHandler = new AgentAiHandler(embeddingAiSetting);

            var iWantToRunEmbedding = agentAiHandler

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


Legacy comment translated to English.

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

Legacy comment translated to English.


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

                        Legacy comment translated to English.

                    }

                    goto MemoryStore;

                }


            });


            Legacy comment translated to English.

            #endregion



            Legacy section


            Legacy comment translated to English.

            StringBuilder results = new StringBuilder();

            var parameter = new PromptConfigParameter()

            {

                MaxTokens = 2000,

                Temperature = 0.7,

                TopP = 0.5,

            };


            var systemMessage = @$"## SystemMessage

Legacy comment translated to English.

Legacy comment translated to English.


## Rule

Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.


            var iWantToRunChat = agentAiHandler.ChatConfig(parameter,

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


            Legacy comment translated to English.

result：

{results.ToString()}

");


            Console.WriteLine();


            Legacy comment translated to English.


            Legacy comment translated to English.

Legacy comment translated to English.

{results.ToString()}";


            var useStream = iWantToRunChat.IWantToBuild.IWantToConfig.IWantTo.SenparcAiSetting.AiPlatform != AiPlatform.NeuCharAI;

            if (useStream)

            {

                Legacy comment translated to English.


                var originalColor = Console.ForegroundColor;//originalcolor

                Action<StreamingKernelContent> streamItemProceessing = async item =>

                {

                    await Console.Out.WriteAsync(item.ToString());


                    Legacy comment translated to English.

                    if (Console.ForegroundColor == originalColor)

                    {

                        Console.ForegroundColor = ConsoleColor.Green;

                    }

                    else

                    {

                        Console.ForegroundColor = originalColor;

                    }

                };


                //outputresult

                SenparcAiResult result = await agentAiHandler.ChatAsync(iWantToRunChat, input, streamItemProceessing);


                Legacy comment translated to English.

                Console.ForegroundColor = originalColor;


                return result.OutputString;

            }

            else

            {

                //iWantToRunChat

                var result = await agentAiHandler.ChatAsync(iWantToRunChat, input);

                await Console.Out.WriteLineAsync(result.OutputString);


                return result.OutputString;

            }


            #endregion

            */
        }
    }
}
