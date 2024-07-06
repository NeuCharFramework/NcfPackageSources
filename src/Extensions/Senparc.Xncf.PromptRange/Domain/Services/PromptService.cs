using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Entities.Keys;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Entities;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptService /*: ServiceDataBase*/
    {
        private readonly SemanticAiHandler _aiHandler;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptItemService _promptItemService;
        private readonly ISenparcAiSetting _senparcAiSetting;

        public IWantToRun IWantToRun { get; set; }

        private string _userId = "XncfBuilder"; //区分用户

        public PromptService(PromptRangeService promptRangeService, PromptItemService promptItemService, ISenparcAiSetting senparcAiSetting = null)
        {
            this._promptRangeService = promptRangeService;
            this._promptItemService = promptItemService;
            this._senparcAiSetting = senparcAiSetting ?? Senparc.AI.Config.SenparcAiSetting;
            this._aiHandler = new SemanticAiHandler(this._senparcAiSetting);
            ReBuildKernel(this._senparcAiSetting);
        }

        public IWantToRun ReBuildKernel(ISenparcAiSetting senparcAiSetting = null, string userId = null, string modelName = null)
        {
            senparcAiSetting ??= Senparc.AI.Config.SenparcAiSetting;
            IWantToRun = this._aiHandler.IWantTo(senparcAiSetting)
                           .ConfigModel(ConfigModel.TextCompletion, userId ?? _userId)
                           .BuildKernel();
            return IWantToRun;
        }

        public async Task<T> GetPromptResultAsync<T>(ISenparcAiSetting senparcAiSetting, string input, SenparcAiArguments context = null, Dictionary<string, List<string>> pluginCollection = null, string pluginDir = null, IPromptTemplateFactory? promptTemplateFactory = null, IServiceProvider? services = null, params KernelFunction[] functionPiple)
        {
            //准备运行
            //var userId = "XncfBuilder";//区分用户
            //var modelName = "text-davinci-003";//默认使用模型

            var iWantToRun = IWantToRun ?? ReBuildKernel(senparcAiSetting);

            ////TODO:外部传入配置
            //var promptParameter = new PromptConfigParameter()
            //{
            //    MaxTokens = 6000,
            //    Temperature = 0.2,
            //    TopP = 0.2,
            //};
            //iWantToRun.SetPromptConfigParameter(promptParameter);

            List<KernelFunction> allFunctionPiple = new List<KernelFunction>();

            if (pluginCollection?.Count > 0)
            {
                //优先从数据库找
                var fromDatabasePrompt = pluginDir.IsNullOrEmpty();//不提供文件地址时，优先从数据库找

                pluginDir ??= Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");

                foreach (var pluginItem in pluginCollection)
                {
                    //第一层：靶场，对应 Plugin

                    var pluginName = pluginItem.Key;//Plugin 名字
                    var functionNames = pluginItem.Value;

                    KernelPlugin kernelPlugin = null;//导入 Plugin 后的 KernelPlugin 对象

                    var tryLocalPromptFile = false; //从数据库读取失败，需要尝试从本地文件读取

                    if (iWantToRun.Kernel.Plugins.Contains(pluginName))
                    {
                        kernelPlugin = iWantToRun.Kernel.Plugins[pluginName];
                    }
                    else
                    {
                        if (fromDatabasePrompt)
                        {
                            //从数据库读取

                            ILoggerFactory loggerFactory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
                            IPromptTemplateFactory factory = promptTemplateFactory ?? new KernelPromptTemplateFactory(loggerFactory);
                            List<KernelFunction> kernelFunctionList = new List<KernelFunction>();
                            ILogger logger = loggerFactory.CreateLogger(typeof(Kernel)) ?? NullLogger.Instance;

                            var promptRange = await this._promptRangeService.GetObjectAsync(z => z.Alias == pluginName || z.RangeName == pluginName);
                            if (promptRange == null)
                            {
                                //转到尝试用文件读取
                                tryLocalPromptFile = true;
                            }
                            else
                            {
                                //查找下属所有的靶道
                                var allPromptItems = await this._promptItemService
                                    .GetObjectListAsync(0, 0,
                                    z => z.RangeId == promptRange.Id
                                    //&& (functionNames.Any(f => f == z.FullVersion) //完整版本信息匹配
                                    //    || functionNames.Any(f => f == z.NickName)) //别称匹配
                                    ,
                                    z => z.EvalMaxScore, OrderingType.Descending);

                                /* PromptItem 的命中有几种可能：
                                 * 1、FullVersion 达到匹配 functionName 要求
                                 * 2、NickName 达到匹配 functionName 要求（常见）
                                 * 3、满足上述任意条件后（都可能多个），在相关记录内，找到评分最高的一个
                                 */

                                List<PromptItem> filteredPromptItems = null;

                                foreach (var functionName in functionNames)
                                {
                                    if (PromptItem.IsPromptVersion(functionName))
                                    {
                                        //符合版本格式
                                        filteredPromptItems = allPromptItems
                                            .Where(z => PromptItem.IsValidVersionSegment(z.FullVersion, functionName))
                                            .ToList();
                                    }
                                    else
                                    {
                                        //使用别名查找
                                        filteredPromptItems = allPromptItems
                                               .Where(z => z.NickName == functionName)
                                               .ToList();
                                    }
                                }

                                if (filteredPromptItems.Count == 0)
                                {
                                    continue;
                                }




                                /* 同一个名称的可能在同一个靶道中，也可能在不同靶道中 */
                                var nameGroupedPromptItems = allPromptItems.GroupBy(z => z.GetAvailableName());//每个 Group 就是一个函数（Function）集合

                                var groupedPromptItems = allPromptItems.GroupBy(z => z.Tactic);//每个 Group 就是一个靶道（Tactic）

                                foreach (var promptItemGroup in groupedPromptItems)
                                {
                                    //第二层：靶道，对应 Function

                                    //选择评分最高的一个
                                    var valiablePromptItem = promptItemGroup.OrderByDescending(z => z.EvalMaxScore).ThenByDescending(z => z.LastUpdateTime).FirstOrDefault();

                                    if (!pluginCollection.Keys.Any(z => z == valiablePromptItem.GetAvailableName()))
                                    {
                                        //如果名称不匹配，则过滤
                                        continue;
                                    }

                                    PromptTemplateConfig promptTemplateConfig = new PromptTemplateConfig()
                                    {
                                        Description = valiablePromptItem.Note,//TODO: 专门提供注释
                                        Name = this.GetAvaliableFunctionName(valiablePromptItem.GetAvailableName()),
                                        //设置模型参数
                                        ExecutionSettings = new Dictionary<string, PromptExecutionSettings>() {
                                        { "Default", new PromptExecutionSettings() {
                                            ExtensionData = new Dictionary<string, object>() {
                                                { "max_tokens", valiablePromptItem.MaxToken },
                                                { "temperature", valiablePromptItem.Temperature },
                                                { "top_p", valiablePromptItem.TopP },
                                                { "presence_penalty", valiablePromptItem.PresencePenalty },
                                                { "frequency_penalty", valiablePromptItem.FrequencyPenalty },
                                                { "stop_sequences", valiablePromptItem.StopSequences.IsNullOrEmpty() ? "" : valiablePromptItem.StopSequences.GetObject<List<string>>() }
                                            }
                                        }
                                        }
                                    },
                                        //设置输入参数
                                        InputVariables = valiablePromptItem.VariableDictJson.IsNullOrEmpty()
                                                            ? new List<InputVariable>()
                                                            : valiablePromptItem.GetInputValiableObject().Select(z => new InputVariable(z)).ToList(),
                                        Template = valiablePromptItem.Content
                                    };

                                    await Console.Out.WriteLineAsync("promptTemplateConfig:" + promptTemplateConfig.ToJson(true));

                                    IPromptTemplate promptTemplate = factory.Create(promptTemplateConfig);

                                    if (logger.IsEnabled(LogLevel.Trace))
                                    {
                                        logger.LogTrace("Registering function {0}.{1} loaded from {2}", pluginName, valiablePromptItem.GetAvailableName(), promptRange.GetAvailableName());
                                    }

                                    kernelFunctionList.Add(KernelFunctionFactory.CreateFromPrompt(promptTemplate, promptTemplateConfig, loggerFactory));


                                }

                                var avaliablePluginName = GetAvaliableFunctionName(pluginName);

                                kernelPlugin = KernelPluginFactory.CreateFromFunctions(avaliablePluginName, null, kernelFunctionList);

                            }

                        }// foreach plugings end


                        if (!fromDatabasePrompt || tryLocalPromptFile)
                        {

                            //从文件读取
                            var finalDir = Path.Combine(pluginDir, pluginName);
                            kernelPlugin = iWantToRun.ImportPluginFromPromptDirectory(finalDir, pluginName).kernelPlugin;
                        }
                    }

                    foreach (var functionName in pluginItem.Value)
                    {
                        if (kernelPlugin.Contains(functionName))
                        {

                            allFunctionPiple.Add(kernelPlugin[functionName]);
                        }
                        else
                        {
                            //TODO:给出警告
                        }

                    }
                }
            }

            if (functionPiple?.Length > 0)
            {
                allFunctionPiple.AddRange(functionPiple);
            }

            if (context != null)
            {
                if (!input.IsNullOrEmpty())
                {
                    context.KernelArguments["input"] = input;
                }
            }

            //构建请求对象
            var request = context == null
                ? iWantToRun.CreateRequest(input, true, allFunctionPiple.ToArray())
                : iWantToRun.CreateRequest(context.KernelArguments, true, allFunctionPiple.ToArray());
            //请求
            var result = await iWantToRun.RunAsync<T>(request);
            return result.Output;
        }

        /// <summary>
        /// 提供可供 functionName 使用的名称
        /// <para>如果出现特殊字符，可能出现错误：A function name can contain only ASCII letters, digits, and underscores: '2024.05.17.1-T1.1-A3' is not a valid name. (Parameter 'value')</para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetAvaliableFunctionName(string name)
        {
            return name.Replace(".", "_").Replace("-", "_").Replace(" ", "_");
        }
    }
}