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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services
{
    public class PromptBuilderService
    {
        private readonly SemanticAiHandler _aiHandler;
        private readonly PromptService _promptService;

        public PromptBuilderService(/*IAiHandler aiHandler,*/ PromptService promptService)
        {
            //this._aiHandler = (SemanticAiHandler)aiHandler;
            this._aiHandler = promptService.IWantToRun.SemanticAiHandler;
            this._promptService = promptService;
        }

        /// <summary>
        /// 运行提示内容
        /// </summary>
        /// <param name="buildType"></param>
        /// <param name="input"></param>
        /// <param name="projectPath"></param>
        /// <param name="namespace"></param>
        /// <returns></returns>
        public async Task<string> RunPromptAsync(PromptBuildType buildType, string input, string projectPath = null, string @namespace = null)
        {
            StringBuilder sb = new StringBuilder();

            string createFilePath = projectPath;

            var plugins = new Dictionary<string, List<string>>();

            //选择需要执行的生成方式
            switch (buildType)
            {
                case PromptBuildType.EntityClass:
                    plugins["XncfBuilderPlugin"] = new List<string>() { "GenerateEntityClass" };
                    if (projectPath!=null)
                    {
                        createFilePath = Path.Combine(createFilePath, "Domain", "Models", "DatabaseModel");
                    }
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
            context.ContextVariables["namespace"] = @namespace;

            var promptResult = await _promptService.GetPromptResultAsync(input, context, plugins);
            
            sb.AppendLine(promptResult);

            await Console.Out.WriteLineAsync($"{buildType.ToString()} 信息：");
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

                var createFileResult = await _promptService.GetPromptResultAsync("", fileContext, null, functionPiple);

                sb.AppendLine();
                sb.AppendLine($"[{SystemTime.Now.ToString()}]");
                sb.AppendLine(createFileResult);
                await Console.Out.WriteLineAsync(createFileResult);

                #endregion

                switch (buildType)
                {
                    case PromptBuildType.EntityClass:

                        #region 更新 SenparcEntities

                        var updateFunctionPiple = new[] { skills[nameof(filePlugin.UpdateSenparcEntities)] };

                        fileContext.ContextVariables["projectPath"] = projectPath;
                        fileContext.ContextVariables["entityName"] = fileGenerateResult[0].FileName.Split('.')[0]; ;

                        var updateSenparcEntitiesResult = await _promptService.GetPromptResultAsync("", fileContext, null, updateFunctionPiple);

                        sb.AppendLine();
                        sb.AppendLine($"[{SystemTime.Now.ToString()}]");
                        sb.AppendLine(updateSenparcEntitiesResult);
                        await Console.Out.WriteLineAsync(updateSenparcEntitiesResult);

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

            return sb.ToString();
        }
    }
}
