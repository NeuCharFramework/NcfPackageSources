using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Entities;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptService /*: ServiceDataBase*/
    {
        private readonly SemanticAiHandler _aiHandler;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptItemService _promptItemService;
        private readonly AIModelService _aiModelService;
        private readonly ISenparcAiSetting _senparcAiSetting;

        public IWantToRun IWantToRun { get; set; }

        private string _userId = "XncfBuilder"; //Distinguish between users

        public PromptService(PromptRangeService promptRangeService, PromptItemService promptItemService, ISenparcAiSetting senparcAiSetting = null, AIModelService aiModelService = null)
        {
            this._promptRangeService = promptRangeService;
            this._promptItemService = promptItemService;
            this._senparcAiSetting = senparcAiSetting ?? Senparc.AI.Config.SenparcAiSetting;
            this._aiHandler = new SemanticAiHandler(this._senparcAiSetting);
            ReBuildKernel(this._senparcAiSetting);
            _aiModelService = aiModelService;
        }

        public IWantToRun ReBuildKernel(ISenparcAiSetting senparcAiSetting = null, string userId = null, string modelName = null)
        {
            senparcAiSetting ??= Senparc.AI.Config.SenparcAiSetting;
            IWantToRun = this._aiHandler.IWantTo(senparcAiSetting)
                           .ConfigModel(ConfigModel.TextCompletion, userId ?? _userId)
                           .BuildKernel();
            return IWantToRun;
        }

        /// <summary>
        /// Based on the input Version, NickName and other rules, find the best quality among the associated PromptItems
        /// <para>When explicitly specified, an exact hit will occur; </para>
        /// <para>When Version is a fuzzy query, find the best one among the lower-level PromptItems</para>
        /// <para>When there are multiple NickNames, find the best one with the same name</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="senparcAiSetting"></param>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <param name="pluginCollection"></param>
        /// <param name="pluginDir"></param>
        /// <param name="promptTemplateFactory"></param>
        /// <param name="services"></param>
        /// <param name="functionPiple"></param>
        /// <returns></returns>
        public async Task<T> GetPromptResultAsync<T>(ISenparcAiSetting senparcAiSetting, string input, SenparcAiArguments context = null, Dictionary<string, List<string>> pluginCollection = null, string pluginDir = null, IPromptTemplateFactory? promptTemplateFactory = null, IServiceProvider? services = null, params KernelFunction[] functionPiple)
        {
            //ready to run
            //var userId = "XncfBuilder";//Differentiate users
            //var modelName = "text-davinci-003"; //Use model by default

            var iWantToRun = IWantToRun ?? ReBuildKernel(senparcAiSetting);

            ////TODO: External incoming configuration
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
                //Find it first from the database
                var fromDatabasePrompt = pluginDir.IsNullOrEmpty();//When the file address is not provided, priority is given to finding it from the database.

                pluginDir ??= Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");

                foreach (var pluginItem in pluginCollection)
                {
                    //The first level: shooting range, corresponding to Plugin

                    var pluginName = pluginItem.Key;//Plugin name
                    var functionNames = pluginItem.Value;

                    KernelPlugin kernelPlugin = null;//KernelPlugin object after importing Plugin

                    var tryLocalPromptFile = false; //Failed to read from database, need to try reading from local file

                    if (iWantToRun.Kernel.Plugins.Contains(pluginName))
                    {
                        kernelPlugin = iWantToRun.Kernel.Plugins[pluginName];
                    }
                    else
                    {
                        if (fromDatabasePrompt)
                        {
                            //Read from database

                            ILoggerFactory loggerFactory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
                            IPromptTemplateFactory factory = promptTemplateFactory ?? new KernelPromptTemplateFactory(loggerFactory);
                            List<KernelFunction> kernelFunctionList = new List<KernelFunction>();
                            ILogger logger = loggerFactory.CreateLogger(typeof(Kernel)) ?? NullLogger.Instance;

                            var promptRange = await this._promptRangeService.GetObjectAsync(z => z.Alias == pluginName || z.RangeName == pluginName);
                            if (promptRange == null)
                            {
                                //Go to try reading with file
                                tryLocalPromptFile = true;
                            }
                            else
                            {
                                //Find all target lanes for subordinates
                                var allPromptItems = await this._promptItemService
                                    .GetObjectListAsync(0, 0,
                                    z => z.RangeId == promptRange.Id
                                    //&& (functionNames.Any(f => f == z.FullVersion) //Full version information matching
                                    //    || functionNames.Any(f => f == z.NickName)) //alias matching
                                    ,
                                    z => z.EvalMaxScore, OrderingType.Descending);

                                /* There are several possibilities for PromptItem hits:
                                 * 1. FullVersion meets the requirement of matching functionName
                                 * 2. NickName meets the requirement of matching functionName (common)
                                 * 3. After meeting any of the above conditions (there may be multiple), find the one with the highest score in the relevant records
                                 */

                                Dictionary<string, PromptItem> filteredPromptItems = new Dictionary<string, PromptItem>();

                                foreach (var functionName in functionNames)
                                {
                                    //Second level: specific PromptItem filtering under a certain shooting range
                                    List<PromptItem> filteredItems = null;
                                    if (PromptItem.IsPromptVersion(functionName))
                                    {
                                        //Comply with version format
                                        filteredItems = allPromptItems
                                            .Where(z => PromptItem.IsValidVersionSegment(z.FullVersion, functionName))
                                            .ToList();
                                    }
                                    else
                                    {
                                        //Find using alias
                                        filteredItems = allPromptItems
                                               .Where(z => z.NickName == functionName)
                                               .ToList();
                                    }

                                    //Select best result
                                    if (filteredItems?.Count != 0)
                                    {
                                        var theBestItem = filteredItems.OrderByDescending(z => z.EvalMaxScore).FirstOrDefault();
                                        if (theBestItem != null)
                                        {
                                            //Add the best results for the current functionName
                                            filteredPromptItems.Add(functionName, theBestItem);

                                            #region 使用当前制定的 AI 模型进行生成
                                            if (senparcAiSetting == null)
                                            {
                                                try
                                                {
                                                    var aiModel = await _aiModelService.GetObjectAsync(z => z.Id == theBestItem.ModelId);
                                                    var aiModelDto = _aiModelService.Mapping<AIModelDto>(aiModel);
                                                    var newAiSetting = _aiModelService.BuildSenparcAiSetting(aiModelDto);
                                                    iWantToRun.SemanticKernelHelper.ResetSenparcAiSetting(newAiSetting);
                                                }
                                                catch (Exception ex)
                                                {
                                                    SenparcTrace.BaseExceptionLog(ex);
                                                    if (iWantToRun.IWantToBuild.IWantToConfig.IWantTo.SenparcAiSetting == null)
                                                    {
                                                        SenparcTrace.BaseExceptionLog(new Exception("AI 模型配置错误，将使用系统默认配置进行覆盖！"));
                                                        //Override with default configuration
                                                        iWantToRun.SemanticKernelHelper.ResetSenparcAiSetting(Senparc.AI.Config.SenparcAiSetting);
                                                    }
                                                }
                                            }

                                            #endregion

                                            PromptTemplateConfig promptTemplateConfig = new PromptTemplateConfig()
                                            {
                                                Description = theBestItem.Note,//TODO: Specially provide comments
                                                Name = this.GetAvaliableFunctionName(theBestItem.GetAvailableName()),
                                                //Set model parameters
                                                ExecutionSettings = new Dictionary<string, PromptExecutionSettings>() {
                                                    { "Default", new PromptExecutionSettings() {
                                                        ExtensionData = new Dictionary<string, object>() {
                                                            { "max_tokens", theBestItem.MaxToken },
                                                            { "temperature", theBestItem.Temperature },
                                                            { "top_p", theBestItem.TopP },
                                                            { "presence_penalty", theBestItem.PresencePenalty },
                                                            { "frequency_penalty", theBestItem.FrequencyPenalty },
                                                            { "stop_sequences", theBestItem.StopSequences.IsNullOrEmpty() ? "" : theBestItem.StopSequences.GetObject<List<string>>() }
                                                            }
                                                         }
                                                    }
                                                 },
                                                //Set input parameters
                                                InputVariables = theBestItem.VariableDictJson.IsNullOrEmpty()
                                                                    ? new List<InputVariable>()
                                                                    : theBestItem.GetInputValiableObject().Select(z => new InputVariable(z)).ToList(),
                                                Template = theBestItem.Content
                                            };

                                            await Console.Out.WriteLineAsync("promptTemplateConfig:" + promptTemplateConfig.ToJson(true));

                                            IPromptTemplate promptTemplate = factory.Create(promptTemplateConfig);

                                            if (logger.IsEnabled(LogLevel.Trace))
                                            {
                                                logger.LogTrace("Registering function {0}.{1} loaded from {2}", pluginName, theBestItem.GetAvailableName(), promptRange.GetAvailableName());
                                            }

                                            kernelFunctionList.Add(KernelFunctionFactory.CreateFromPrompt(promptTemplate, promptTemplateConfig, loggerFactory));
                                        }
                                    }
                                }

                                var avaliablePluginName = GetAvaliableFunctionName(pluginName);

                                kernelPlugin = KernelPluginFactory.CreateFromFunctions(avaliablePluginName, null, kernelFunctionList);

                            }
                        }
                    }

                    //Need to read from Plugin file
                    if (!fromDatabasePrompt || tryLocalPromptFile)
                    {
                        //read from file
                        var finalDir = Path.Combine(pluginDir, pluginName);
                        kernelPlugin = iWantToRun.ImportPluginFromPromptDirectory(finalDir, pluginName).kernelPlugin;
                    }

                    //Unifiedly register functions for reading files or databases
                    foreach (var functionName in functionNames)
                    {
                        if (kernelPlugin.Contains(functionName))
                        {
                            allFunctionPiple.Add(kernelPlugin[functionName]);
                        }
                        else
                        {
                            //TODO: give a warning
                        }

                    }
                }// foreach pluginCollection end
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

            //Build request object
            var request = context == null
                ? iWantToRun.CreateRequest(input, true, allFunctionPiple.ToArray())
                : iWantToRun.CreateRequest(context.KernelArguments, true, allFunctionPiple.ToArray());
            //ask
            var result = await iWantToRun.RunAsync<T>(request);
            return result.Output;
        }

        /// <summary>
        /// Provides a name that functionName can use
        /// <para>If special characters appear, an error may occur: A function name can contain only ASCII letters, digits, and underscores: '2024.05.17.1-T1.1-A3' is not a valid name. (Parameter 'value')</para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetAvaliableFunctionName(string name)
        {
            return name.Replace(".", "_").Replace("-", "_").Replace(" ", "_");
        }
    }
}