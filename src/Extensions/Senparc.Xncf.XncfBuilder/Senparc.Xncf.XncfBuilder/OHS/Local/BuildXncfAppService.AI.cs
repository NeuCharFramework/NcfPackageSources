using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Functions;
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

                var fileInfo = entityResult.FileResult.FileContents.First();

                //从 promptGroupFileContent 分析获得类名
                var fileContent = fileInfo.Value; //File.ReadAllText(fileInfo.Key);
                                                  //var entityResultObj = entityResult.ResponseText.GetObject<dynamic>();
                var className = new Regex(@"public class (\w+)").Match(fileContent).Groups[1].Value;

                #endregion

                #region 生成实体 DTO

                if (request.MoreActions.IsSelected("BuildDto"))
                {
                    var entityDtoResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.EntityDtoClass, fileContent, className, null, projectPath, @namespace);
                    logger.Append("生成实体 DTO：");
                    logger.Append(entityResult.Log);
                }

                #endregion

                #region 更新 SenparcEntities

                var updateSenparcEntitiesResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.UpdateSenparcEntities, className, className, entityResult.Context, projectPath, @namespace);
                logger.Append("更新 SenparcEntities：");
                logger.Append(entityResult.Log);

                #endregion

                #region 进行 Migration
                if (request.MoreActions.IsSelected("BuildMigration"))
                {
                    await Console.Out.WriteLineAsync("进入 Migration，可能耗时较长，请等待");
                    logger.Append();
                    logger.Append("Migration 开始执行");

                    #region 需要把 DatabasePlant 进行文件附加
                    var databasePlantPath = Path.GetFullPath(Path.Combine(projectPath, "..", "Senparc.Web.DatabasePlant"));
                    var databasePlantCsprojPath = Path.Combine(databasePlantPath, "Senparc.Web.DatabasePlant.csproj");
                    //目标项目的文件夹名称
                    var projectFolderName = Path.GetFileName(projectPath.TrimEnd(Path.DirectorySeparatorChar));
                    //目标项目的 csproj 项目文件名
                    string newReferencePath = @$"..\{projectFolderName}\{projectFolderName}.csproj";
                    XDocument doc = XDocument.Load(databasePlantCsprojPath);

                    // 获取 <ItemGroup> 元素  
                    var itemGroups = doc.Root.Elements("ItemGroup");

                    // 删除所有的 ProjectReference  
                    foreach (var itemGroup in itemGroups)
                    {
                        var projectReferences = itemGroup.Elements("ProjectReference").ToList();
                        foreach (var reference in projectReferences)
                        {
                            reference.Remove();
                        }
                    }

                    // 添加新的 ProjectReference  
                    XElement newProjectReference = new XElement("ProjectReference", new XAttribute("Include", newReferencePath));
                    var itemGroupToAdd = itemGroups.FirstOrDefault() ?? new XElement("ItemGroup");
                    itemGroupToAdd.Add(newProjectReference);

                    if (itemGroupToAdd.Parent == null)
                    {
                        doc.Root.Add(itemGroupToAdd);
                    }

                    // 保存修改后的 .csproj 文件  
                    doc.Save(databasePlantCsprojPath);

                    logger.Append($"完成 Senparc.Web.DatabasePlant 项目引用自动替换");
                    logger.Append($"databasePlantPath: {databasePlantPath}");
                    logger.Append($"projectPath: {projectPath}");
                    logger.Append($"projectFoldeName: {projectFolderName}");
                    logger.Append($"newProjectReference: {newProjectReference}");

                    #endregion

                    var requestObj = new DatabaseMigrations_MigrationRequest()
                    {
                        //CustomProjectPath = projectPath,
                        DatabasePlantPath = databasePlantPath,
                        MigrationName = $"Add_{className}_Entity",
                    };
                    //载入数据
                    await requestObj.LoadData(this.ServiceProvider);

                    //指定项目路径
                    requestObj.ProjectPath.SelectedValues = new[] { projectPath };
                    //选中所有数据库
                    requestObj.DatabaseTypes.SelectedValues = requestObj.DatabaseTypes.Items.Select(z => z.Value).ToArray();
                    //选中输出详情
                    requestObj.OutputVerbose.SelectedValues = new[] { requestObj.OutputVerbose.Items.First().Value };

                    var databaseMigrationsAppService = base.ServiceProvider.GetRequiredService<DatabaseMigrationsAppService>();
                    var migrationResult = await databaseMigrationsAppService.AddMigration(requestObj);


                    logger.Append("执行结束，是否成功：" + migrationResult.Success);
                    logger.Append("消息：" + (migrationResult.Success == true ? migrationResult.Data : $"{migrationResult.Data} / {migrationResult.ErrorMessage}"));
                    logger.Append("Migration 内部日志：");

                    #region 从缓存中读取日志

                    var tempId = migrationResult.RequestTempId;
                    var cache = this.ServiceProvider.GetObjectCacheStrategyInstance();
                    //为了加快响应速度，不等待
                    var migrationLog = await cache.GetAsync<string>(tempId);
                    logger.Append(migrationLog);

                    #endregion

                    logger.Append();
                }
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

        //[FunctionRender("[AI] 生成 AppService", "使用 AI 指令生成 AppService", typeof(Register))]
        //public async Task<StringAppResponse> CreateAppService()
        //{ 
        
        //}
    }
}
