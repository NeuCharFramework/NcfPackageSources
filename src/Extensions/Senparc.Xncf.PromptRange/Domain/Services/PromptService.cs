/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptService.cs
    File description: Related implementation.

    Created by: Senparc - 20231001

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Entities;
using Senparc.AI.AgentKernel.Handlers;
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
        private readonly AgentAiHandler _aiHandler;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptItemService _promptItemService;
        private readonly AIModelService _aiModelService;
        private readonly ISenparcAiSetting _senparcAiSetting;

        public IWantToRun IWantToRun { get; set; }

        private string _userId = "XncfBuilder"; //Legacy comment translated to English.

        /// <summary>
        /// Initializes prompt execution with range/item persistence and AI settings.
        /// </summary>
        /// <param name="promptRangeService">The prompt-range service.</param>
        /// <param name="promptItemService">The prompt-item service.</param>
        /// <param name="senparcAiSetting">Optional AI settings; global settings are used when absent.</param>
        /// <param name="aiModelService">Optional AI model service.</param>
        public PromptService(PromptRangeService promptRangeService, PromptItemService promptItemService, ISenparcAiSetting senparcAiSetting = null, AIModelService aiModelService = null)
        {
            this._promptRangeService = promptRangeService;
            this._promptItemService = promptItemService;
            this._senparcAiSetting = senparcAiSetting ?? Senparc.AI.Config.SenparcAiSetting;
            this._aiHandler = new AgentAiHandler(this._senparcAiSetting);
            ReBuildKernel(this._senparcAiSetting);
            _aiModelService = aiModelService;
        }

        /// <summary>
        /// Rebuilds the text-completion kernel using supplied or global AI settings.
        /// </summary>
        /// <param name="senparcAiSetting">Optional AI settings.</param>
        /// <param name="userId">Optional execution user identifier.</param>
        /// <param name="modelName">Optional model name retained for compatibility.</param>
        /// <returns>The rebuilt agent runner.</returns>
        public IWantToRun ReBuildKernel(ISenparcAiSetting senparcAiSetting = null, string userId = null, string modelName = null)
        {
            senparcAiSetting ??= Senparc.AI.Config.SenparcAiSetting;
            IWantToRun = this._aiHandler.IWantTo(senparcAiSetting)
                           .ConfigModel(ConfigModel.TextCompletion, userId ?? _userId)
                           .BuildKernel();
            return IWantToRun;
        }

        /* Legacy comment translated to English. */

        // The former generic prompt-result helper is retained only as historical
        // reference; current execution uses the active PromptResultService pipeline.
        ///// <typeparam name="T"></typeparam>
        ///// <param name="senparcAiSetting"></param>
        ///// <param name="input"></param>
        ///// <param name="context"></param>
        ///// <param name="pluginCollection"></param>
        ///// <param name="pluginDir"></param>
        ///// <param name="promptTemplateFactory"></param>
        ///// <param name="services"></param>
        ///// <param name="functionPiple"></param>
        ///// <returns></returns>
        //public async Task<T> GetPromptResultAsync<T>(ISenparcAiSetting senparcAiSetting, string input, SenparcAiArguments context = null, Dictionary<string, List<string>> pluginCollection = null, string pluginDir = null, IPromptTemplateFactory? promptTemplateFactory = null, IServiceProvider? services = null, params KernelFunction[] functionPiple)
        //{
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //    //var modelName = "text-davinci-003";//defaultusemodel

        //    var iWantToRun = IWantToRun ?? ReBuildKernel(senparcAiSetting);

        //Legacy comment translated to English.
        //    //var promptParameter = new PromptConfigParameter()
        //    //{
        //    //    MaxTokens = 6000,
        //    //    Temperature = 0.2,
        //    //    TopP = 0.2,
        //    //};
        //    //iWantToRun.SetPromptConfigParameter(promptParameter);

        //    List<KernelFunction> allFunctionPiple = new List<KernelFunction>();

        //    if (pluginCollection?.Count > 0)
        //    {
        //Legacy comment translated to English.
        //Legacy comment translated to English.

        //        pluginDir ??= Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");

        //        foreach (var pluginItem in pluginCollection)
        //        {
        //Legacy comment translated to English.

        //Legacy comment translated to English.
        //            var functionNames = pluginItem.Value;

        //Legacy comment translated to English.

        //Legacy comment translated to English.

        //            if (iWantToRun.Kernel.Plugins.Contains(pluginName))
        //            {
        //                kernelPlugin = iWantToRun.Kernel.Plugins[pluginName];
        //            }
        //            else
        //            {
        //                if (fromDatabasePrompt)
        //                {
        //                    //fromdatabaseread

        //                    ILoggerFactory loggerFactory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        //                    IPromptTemplateFactory factory = promptTemplateFactory ?? new KernelPromptTemplateFactory(loggerFactory);
        //                    List<KernelFunction> kernelFunctionList = new List<KernelFunction>();
        //                    ILogger logger = loggerFactory.CreateLogger(typeof(Kernel)) ?? NullLogger.Instance;

        //                    var promptRange = await this._promptRangeService.GetObjectAsync(z => z.Alias == pluginName || z.RangeName == pluginName);
        //                    if (promptRange == null)
        //                    {
        //Legacy comment translated to English.
        //                        tryLocalPromptFile = true;
        //                    }
        //                    else
        //                    {
        //Legacy comment translated to English.
        //                        var allPromptItems = await this._promptItemService
        //                            .GetObjectListAsync(0, 0,
        //                            z => z.RangeId == promptRange.Id
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //                            ,
        //                            z => z.EvalMaxScore, OrderingType.Descending);

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //                         */

        //                        Dictionary<string, PromptItem> filteredPromptItems = new Dictionary<string, PromptItem>();

        //                        foreach (var functionName in functionNames)
        //                        {
        //Legacy comment translated to English.
        //                            List<PromptItem> filteredItems = null;
        //                            if (PromptItem.IsPromptVersion(functionName))
        //                            {
        //Legacy comment translated to English.
        //                                filteredItems = allPromptItems
        //                                    .Where(z => PromptItem.IsValidVersionSegment(z.FullVersion, functionName))
        //                                    .ToList();
        //                            }
        //                            else
        //                            {
        //Legacy comment translated to English.
        //                                filteredItems = allPromptItems
        //                                       .Where(z => z.NickName == functionName)
        //                                       .ToList();
        //                            }

        //Legacy comment translated to English.
        //                            if (filteredItems?.Count != 0)
        //                            {
        //                                var theBestItem = filteredItems.OrderByDescending(z => z.EvalMaxScore).FirstOrDefault();
        //                                if (theBestItem != null)
        //                                {
        //Legacy comment translated to English.
        //                                    filteredPromptItems.Add(functionName, theBestItem);

        //                                    #region Legacy section
        //                                    if (senparcAiSetting == null)
        //                                    {
        //                                        try
        //                                        {
        //                                            var aiModel = await _aiModelService.GetObjectAsync(z => z.Id == theBestItem.ModelId);
        //                                            var aiModelDto = _aiModelService.Mapping<AIModelDto>(aiModel);
        //                                            var newAiSetting = _aiModelService.BuildSenparcAiSetting(aiModelDto);
        //                                            iWantToRun.SemanticKernelHelper.ResetSenparcAiSetting(newAiSetting);
        //                                        }
        //                                        catch (Exception ex)
        //                                        {
        //                                            SenparcTrace.BaseExceptionLog(ex);
        //                                            if (iWantToRun.IWantToBuild.IWantToConfig.IWantTo.SenparcAiSetting == null)
        //                                            {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //                                                iWantToRun.SemanticKernelHelper.ResetSenparcAiSetting(Senparc.AI.Config.SenparcAiSetting);
        //                                            }
        //                                        }
        //                                    }

        //                                    #endregion

        //                                    PromptTemplateConfig promptTemplateConfig = new PromptTemplateConfig()
        //                                    {
        //Legacy comment translated to English.
        //                                        Name = this.GetAvaliableFunctionName(theBestItem.GetAvailableName()),
        //                                        //settingsmodel parameters
        //                                        ExecutionSettings = new Dictionary<string, PromptExecutionSettings>() {
        //                                            { "Default", new PromptExecutionSettings() {
        //                                                ExtensionData = new Dictionary<string, object>() {
        //                                                    { "max_tokens", theBestItem.MaxToken },
        //                                                    { "temperature", theBestItem.Temperature },
        //                                                    { "top_p", theBestItem.TopP },
        //                                                    { "presence_penalty", theBestItem.PresencePenalty },
        //                                                    { "frequency_penalty", theBestItem.FrequencyPenalty },
        //                                                    { "stop_sequences", theBestItem.StopSequences.IsNullOrEmpty() ? "" : theBestItem.StopSequences.GetObject<List<string>>() }
        //                                                    }
        //                                                 }
        //                                            }
        //                                         },
        //                                        //settingsinputparameters
        //                                        InputVariables = theBestItem.VariableDictJson.IsNullOrEmpty()
        //                                                            ? new List<InputVariable>()
        //                                                            : theBestItem.GetInputValiableObject().Select(z => new InputVariable(z)).ToList(),
        //                                        Template = theBestItem.Content
        //                                    };

        //                                    await Console.Out.WriteLineAsync("promptTemplateConfig:" + promptTemplateConfig.ToJson(true));

        //                                    IPromptTemplate promptTemplate = factory.Create(promptTemplateConfig);

        //                                    if (logger.IsEnabled(LogLevel.Trace))
        //                                    {
        //                                        logger.LogTrace("Registering function {0}.{1} loaded from {2}", pluginName, theBestItem.GetAvailableName(), promptRange.GetAvailableName());
        //                                    }

        //                                    kernelFunctionList.Add(KernelFunctionFactory.CreateFromPrompt(promptTemplate, promptTemplateConfig, loggerFactory));
        //                                }
        //                            }
        //                        }

        //                        var avaliablePluginName = GetAvaliableFunctionName(pluginName);

        //                        kernelPlugin = KernelPluginFactory.CreateFromFunctions(avaliablePluginName, null, kernelFunctionList);

        //                    }
        //                }
        //            }

        //            //needfrom Plugin fileread
        //            if (!fromDatabasePrompt || tryLocalPromptFile)
        //            {
        //                //fromfileread
        //                var finalDir = Path.Combine(pluginDir, pluginName);
        //                kernelPlugin = iWantToRun.ImportPluginFromPromptDirectory(finalDir, pluginName).kernelPlugin;
        //            }

        //Legacy comment translated to English.
        //            foreach (var functionName in functionNames)
        //            {
        //                if (kernelPlugin.Contains(functionName))
        //                {
        //                    allFunctionPiple.Add(kernelPlugin[functionName]);
        //                }
        //                else
        //                {
        //Legacy comment translated to English.
        //                }

        //            }
        //        }// foreach pluginCollection end
        //    }


        //    if (functionPiple?.Length > 0)
        //    {
        //        allFunctionPiple.AddRange(functionPiple);
        //    }

        //    if (context != null)
        //    {
        //        if (!input.IsNullOrEmpty())
        //        {
        //            context.KernelArguments["input"] = input;
        //        }
        //    }

        //Legacy comment translated to English.
        //    var request = context == null
        //        ? iWantToRun.CreateRequest(input, true, allFunctionPiple.ToArray())
        //        : iWantToRun.CreateRequest(context.KernelArguments, true, allFunctionPiple.ToArray());
        //    //request
        //    var result = await iWantToRun.RunAsync<T>(request);
        //    return result.Output;
        //}

        /// <summary>
        /// Converts a function name into a safe identifier for PromptRange function registration.
        /// </summary>
        /// <param name="name">The source function name.</param>
        /// <returns>The name with dots, dashes, and spaces replaced by underscores.</returns>
        public string GetAvaliableFunctionName(string name)
        {
            return name.Replace(".", "_").Replace("-", "_").Replace(" ", "_");
        }
    }
}
