using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.XncfBuilder.Domain.Services.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services
{
    public class PromptBuilderService
    {
        private readonly SemanticAiHandler _aiHandler;
        private readonly PromptService _promptService;

        public PromptBuilderService(IAiHandler aiHandler, PromptService promptService)
        {
            this._aiHandler = (SemanticAiHandler)aiHandler;
            this._promptService = promptService;
        }

        /// <summary>
        /// 运行提示内容
        /// </summary>
        /// <param name="buildType"></param>
        /// <param name="input"></param>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        public async Task<string> RunPrompt(PromptBuildType buildType, string input, string projectPath)
        {
            var functions = new Dictionary<string, List<string>>();

            switch (buildType)
            {
                case PromptBuildType.EntityClass:
                    functions["XncfBuilderPlugin"] = new List<string>() { "GenerateEntityClass" };
                    break;
                case PromptBuildType.Repository:
                    break;
                case PromptBuildType.Service:
                    break;
                case PromptBuildType.AppService:
                    break;
                case PromptBuildType.PL:
                    break;
                case PromptBuildType.DbContext:
                    break;
                default:
                    break;
            }

            var filePlugin = new FilePlugin();

            var skills = _promptService.IWantToRun.Kernel.ImportSkill(filePlugin, nameof(filePlugin.CreateAsync));

            //输入生成文件的项目路径
            //var context = _promptService.IWantToRun.Kernel.CreateNewContext();//TODO：简化
            var context = new AI.Kernel.Entities.SenparcAiContext();//TODO：简化
            context.ExtendContext["ProjectPath"] = projectPath;

            var promptResult = await _promptService.GetPromptResultAsync(input, functions, context);

            return promptResult;
        }
    }
}
