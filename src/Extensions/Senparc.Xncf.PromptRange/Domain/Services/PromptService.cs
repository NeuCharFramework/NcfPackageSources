using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Entities.Keys;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Entities;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptService /*: ServiceDataBase*/
    {
        private readonly SemanticAiHandler _aiHandler;
        private readonly ISenparcAiSetting _senparcAiSetting;

        public IWantToRun IWantToRun { get; set; }

        private string _userId = "XncfBuilder"; //区分用户

        public PromptService(ISenparcAiSetting senparcAiSetting = null)
        {
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

        /// <summary>
        /// 根据 Plugin 的 Prompt 获取结果
        /// </summary>
        /// <param name="senparcAiSetting"></param>
        /// <param name="input">用户输入</param>
        /// <param name="context">上下文参数</param>
        /// <param name="plugins">所有需要引用的 Skill（Plugin） 的清单
        /// <para>Key：Skill Name</para>
        /// <para>Value：Function Name List</para>
        /// </param>
        /// <param name="functionPiple">functionPiple</param>
        /// <returns></returns>
        public async Task<T> GetPromptResultAsync<T>(ISenparcAiSetting senparcAiSetting, string input, SenparcAiArguments context = null, Dictionary<string, List<string>> plugins = null, string pluginDir = null, params KernelFunction[] functionPiple)
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

            if (plugins?.Count > 0)
            {
                //TODO: 默认从数据库查找

                pluginDir ??= Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");

                foreach (var pluginName in plugins)
                {
                    var finalDir = Path.Combine(pluginDir, pluginName.Key);
                    var pluginResults = iWantToRun.ImportPluginFromPromptDirectory(finalDir, pluginName.Key);

                    foreach (var functionName in pluginName.Value)
                    {
                        allFunctionPiple.Add(pluginResults.kernelPlugin[functionName]);
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
    }
}