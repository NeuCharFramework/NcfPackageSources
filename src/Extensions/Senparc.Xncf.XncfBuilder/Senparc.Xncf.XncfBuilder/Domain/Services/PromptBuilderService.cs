using Microsoft.SemanticKernel.SkillDefinition;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Entities;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.XncfBuilder.Domain.Services.Plugins;
using System;
using System.Collections.Generic;
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
        public async Task<string> RunPromptAsync(PromptBuildType buildType, string input, string projectPath = null)
        {
            var plugins = new Dictionary<string, List<string>>();

            //选择需要执行的生成方式
            switch (buildType)
            {
                case PromptBuildType.EntityClass:
                    plugins["XncfBuilderPlugin"] = new List<string>() { "GenerateEntityClass" };
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

            var context = new SenparcAiContext();
            context.ContextVariables["input"] = input;

            var promptResult = await _promptService.GetPromptResultAsync(input, context, plugins);

            await Console.Out.WriteLineAsync(promptResult);

            //需要保存文件
            if (!projectPath.IsNullOrEmpty())
            {
                #region 创建文件

                //输入生成文件的项目路径
                //var context = _promptService.IWantToRun.Kernel.CreateNewContext();//TODO：简化
                var fileContext = new AI.Kernel.Entities.SenparcAiContext();//TODO：简化

                fileContext.ContextVariables["fileBasePath"] = projectPath;
                fileContext.ContextVariables["fileGenerateResult"] = promptResult;

                var fileGenerateResult = promptResult.GetObject<List<FileGenerateResult>>();

                //添加保存文件的 Plugin
                var filePlugin = new FilePlugin(_promptService.IWantToRun);
                var skills = _promptService.IWantToRun.Kernel.ImportSkill(filePlugin, "FilePlugin");

                ISKFunction[] functionPiple = new[] { skills[nameof(filePlugin.CreateFile)] };

                promptResult = await _promptService.GetPromptResultAsync("", fileContext, null, functionPiple);

                #endregion

                switch (buildType)
                {
                    case PromptBuildType.EntityClass:

                        #region 更新 SenparcEntities

                        var updateFunctionPiple = new[] { skills[nameof(filePlugin.UpdateSenparcEntities)] };

                        fileContext.ContextVariables["projectPath"] = projectPath;
                        fileContext.ContextVariables["entityName"] = fileGenerateResult[0].FileName.Split('.')[0]; ;

                        promptResult = await _promptService.GetPromptResultAsync("", fileContext, null, updateFunctionPiple);

                        #endregion

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
            }

            return promptResult;
        }
    }
}
