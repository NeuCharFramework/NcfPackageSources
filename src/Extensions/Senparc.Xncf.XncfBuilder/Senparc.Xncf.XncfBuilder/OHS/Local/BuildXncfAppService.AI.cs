using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using Senparc.Xncf.XncfBuilder.OHS.PL;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    public partial class BuildXncfAppService
    {
        /// <summary>
        /// AI 生成数据库实体
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NcfExceptionBase"></exception>
        [FunctionRender("[AI] 生成数据库实体", "生成符合 DDD 约束的数据库实体及其包含的方法。注意：1、请在开发环境中使用此方法，系统将自动检测。2、请做好代码备份，建议切换一个干净的分支。", typeof(Register))]
        public async Task<StringAppResponse> CreateDatabaseEntity(BuildXncf_CreateDatabaseEntityRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var promptBuilderService = base.ServiceProvider.GetRequiredService<PromptBuilderService>();
                var aiModelSelected = request.AIModel.SelectedValues.FirstOrDefault();

                #region PromptRange 是否已经初始化
                if (request.UseDatabasePrompt.SelectedValues.FirstOrDefault() == "1")
                {
                    var promptRangeRegister = new Xncf.PromptRange.Register();
                    var promptRangeModule = this._xncfModuleService.GetObject(z => z.Uid == promptRangeRegister.Uid);

                    if (promptRangeModule == null)
                    {
                        //开始安装模块（创建数据库相关表）
                        await promptRangeRegister.InstallOrUpdateAsync(ServiceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);

                        await this._xncfModuleServiceExtension.InstallModuleAsync(promptRangeRegister.Uid);

                        promptRangeModule = this._xncfModuleService.GetObject(z => z.Uid == promptRangeRegister.Uid);
                    }

                    if (promptRangeModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                    {
                        promptRangeModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                        await _xncfModuleService.SaveObjectAsync(promptRangeModule);
                    }

                    //检查是否已经初始化
                    var promptRangeInitResult = await promptBuilderService.InitPromptAsync("XncfBuilderPlugin", false, aiModelSelected);
                    logger.Append("PromptRange 初始化：" + promptRangeInitResult);
                }
                #endregion


                var input = request.Requirement;

                var projectPath = request.InjectDomain.SelectedValues.FirstOrDefault();

                if (projectPath.IsNullOrEmpty() || projectPath == "N/A")
                {
                    throw new Exception("没有发现任何可用的 XNCF 项目，请确保你正在一个标准的 NCF 开发环境中！");
                }

                var @namespace = Path.GetFileName(projectPath) + ".Models.DatabaseModel";

                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                if (aiModelSelected != "Default")
                {
                    int.TryParse(aiModelSelected, out int aiModelId);
                    var aiModel = await _aIModelService.GetObjectAsync(z => z.Id == aiModelId);
                    if (aiModel == null)
                    {
                        throw new NcfExceptionBase($"当前选择的 AI 模型不存在：{aiModelSelected}");
                    }

                    var aiModelDto = _aIModelService.Mapper.Map<AIModelDto>(aiModel);

                    aiSetting = _aIModelService.BuildSenparcAiSetting(aiModelDto);
                }

                #region 生成实体

                var entityResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.EntityClass, input, null, null, projectPath, @namespace);
                logger.Append("生成实体：");
                logger.Append(entityResult.Log);

                #endregion

                #region 生成实体 DTO

                var fileInfo = entityResult.FileResult.FileContents.First();

                //从 promptGroupFileContent 分析获得类名
                var fileContent = fileInfo.Value; //File.ReadAllText(fileInfo.Key);
                //var entityResultObj = entityResult.ResponseText.GetObject<dynamic>();
                var className = new Regex(@"public class (\w+)").Match(fileContent).Groups[1].Value;

                var entityDtoResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.EntityDtoClass, fileContent, className, null, projectPath, @namespace);
                logger.Append("生成实体 DTO：");
                logger.Append(entityResult.Log);

                #endregion

                #region 更新 SenparcEntities

                var updateSenparcEntitiesResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.UpdateSenparcEntities, className, className, entityResult.Context, projectPath, @namespace);
                logger.Append("更新 SenparcEntities：");
                logger.Append(entityResult.Log);

                #endregion

                return logger.ToString();
            });
        }

        //TODO：生成实体的过程中默认检查是否自动载入Prompt，提供手动指定地址选项

        //初始化 PromptRange 方法（需要先确保已经安装）


        [FunctionRender("初始化 Prompt", "初始化所有 AI 代码生成需要的 Prompt", typeof(Register))]
        public async Task<StringAppResponse> InitPrompt(BuildXncf_InitPromptRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var needOverride = request.Override.SelectedValues.Contains("1");
                var aiModel = request.AIModel.SelectedValues.FirstOrDefault();

                var promptBuilderService = base.ServiceProvider.GetRequiredService<PromptBuilderService>();
                var log = await promptBuilderService.InitPromptAsync("XncfBuilderPlugin", needOverride, aiModel);
                logger.Append(log);

                logger.SaveLogs("InitPrompt");

                return log;
            });
        }
    }
}
