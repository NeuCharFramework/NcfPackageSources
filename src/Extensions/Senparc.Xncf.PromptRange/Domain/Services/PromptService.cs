using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Entities;
using Senparc.AI.Kernel.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptService /*: ServiceDataBase*/
    {
        private readonly SemanticAiHandler _aiHandler;

        public IWantToRun IWantToRun { get; set; }

        private string _userId = "XncfBuilder";//区分用户
        private string _modelName = "text-davinci-003";//默认使用模型

        public PromptService(/*IAiHandler aiHandler*/)
        {
            //this._aiHandler = (SemanticAiHandler)aiHandler;
            this._aiHandler = new SemanticAiHandler();
            ReBuildKernel();
        }

        public IWantToRun ReBuildKernel(string userId = null, string modelName = null)
        {
            IWantToRun = this._aiHandler.IWantTo()
                           .ConfigModel(ConfigModel.TextCompletion, userId ?? _userId, modelName ?? _modelName)
                           .BuildKernel();
            return IWantToRun;
        }

        /// <summary>
        /// 根据 Plugin 的 Prompt 获取结果
        /// </summary>
        /// <param name="input">用户输入</param>
        /// <param name="context">上下文参数</param>
        /// <param name="plugins">所有需要引用的 Skill（Plugin） 的清单
        /// <para>Key：Skill Name</para>
        /// <para>Value：Function Name List</para>
        /// </param>
        /// <param name="functionPiple">functionPiple</param>
        /// <returns></returns>
        public async Task<string> GetPromptResultAsync(string input, SenparcAiArguments context = null, Dictionary<string, List<string>> plugins = null, params KernelFunction[] functionPiple)
        {
            //准备运行
            //var userId = "XncfBuilder";//区分用户
            //var modelName = "text-davinci-003";//默认使用模型

            var iWantToRun = IWantToRun ?? ReBuildKernel();

            List<KernelFunction> allFunctionPiple = new List<KernelFunction>();

            if (plugins?.Count > 0)
            {
                var pluginDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");
                foreach (var plubName in plugins)
                {
                    var pluginResults = iWantToRun.ImportPluginFromDirectory(pluginDir, plubName.Key);

                    foreach (var functionName in plubName.Value)
                    {
                        allFunctionPiple.Add(pluginResults.skillList[functionName]);
                    }
                }
            }

            if (functionPiple?.Length > 0)
            {
                allFunctionPiple.AddRange(functionPiple);
            }

            if (context != null)
            {
                context.KernelArguments["input"] = input;
            }

            //构建请求对象
            var request = context == null
                ? iWantToRun.CreateRequest(input, true, allFunctionPiple.ToArray())
                : iWantToRun.CreateRequest(context.KernelArguments, true, allFunctionPiple.ToArray());
            //请求
            var result = await iWantToRun.RunAsync(request);
            return result.Output;
        }
    }
}
