/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: BuildXncfAppService.AI.cs

    File description: Related implementation.

    

    

    Created by: Senparc - 20240514

    

    Modified by: Senparc - 20260702

    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling



    Modified by: Senparc - 20260717

    Change description: v0.37.0-preview5 Updated module behavior and related support.



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Interfaces;
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
        /* Legacy comment translated to English. */

        /*

         * 

        /// <summary>

        Legacy comment translated to English.

        /// </summary>

        /// <param name="request"></param>

        /// <returns></returns>

        /// <exception cref="Exception"></exception>

        /// <exception cref="NcfExceptionBase"></exception>

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.GenerateEntity.Name", "Function.XncfBuilder.GenerateEntity.Description", typeof(Register))]

        public async Task<StringAppResponse> CreateDatabaseEntity(BuildXncf_CreateDatabaseEntityRequest request)

        {

            return await this.GetStringResponseAsync(async (response, logger) =>

            {

                var promptBuilderService = base.ServiceProvider.GetRequiredService<PromptBuilderService>();

                var aiModelSelected = request.AIModel;

                ISenparcAiSetting aiSetting = null;



                #region PromptRange whetheralreadyinitialize

                if (request.UseDatabasePrompt)

                {

                    var promptRangeRegister = new Xncf.PromptRange.Register();

                    var promptRangeModule = this._xncfModuleService.GetObject(z => z.Uid == promptRangeRegister.Uid);



                    if (promptRangeModule == null)

                    {

                        //start installing the module (create related database tables)

                        await promptRangeRegister.InstallOrUpdateAsync(ServiceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);



                        await this._xncfModuleServiceExtension.InstallModuleAsync(promptRangeRegister.Uid);



                        promptRangeModule = this._xncfModuleService.GetObject(z => z.Uid == promptRangeRegister.Uid);

                    }



                    Legacy comment translated to English.

                    {

                        Legacy comment translated to English.

                        await _xncfModuleService.SaveObjectAsync(promptRangeModule);

                    }



                    Legacy comment translated to English.



                    //checkwhetheralreadyinitialize

                    var promptRangeInitResult = await promptBuilderService.InitPromptAsync("XncfBuilderPlugin", false, aiModelSelected);

                    logger.Append("PromptRange initialize：" + promptRangeInitResult);

                }

                else

                {

                    Legacy comment translated to English.

                    aiSetting = Senparc.AI.Config.SenparcAiSetting;

                    if (aiModelSelected != "Default")

                    {

                        int.TryParse(aiModelSelected, out int aiModelId);

                        var aiModel = await _aIModelService.GetObjectAsync(z => z.Id == aiModelId);

                        if (aiModel == null)

                        {

                            Legacy comment translated to English.

                        }



                        var aiModelDto = _aIModelService.Mapping<AIModelDto>(aiModel);



                        aiSetting = _aIModelService.BuildSenparcAiSetting(aiModelDto);

                        Legacy comment translated to English.

                    }

                    else

                    {

                        Legacy comment translated to English.

                    }

                }

                #endregion





                var input = request.Requirement;



                var projectPath = request.InjectDomain;



                if (projectPath.IsNullOrEmpty() || projectPath == "N/A")

                {

                    Legacy comment translated to English.

                }



                var @namespace = Path.GetFileName(projectPath) + ".Models.DatabaseModel";



                #region generate entity



                var entityResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.EntityClass, input, null, null, projectPath, @namespace);

                logger.Append("generate entity：");

                logger.Append(entityResult.Log);



                var fileInfo = entityResult.FileResult.FileContents.First();



                Legacy comment translated to English.

                var fileContent = fileInfo.Value; //File.ReadAllText(fileInfo.Key);

                                                  //var entityResultObj = entityResult.ResponseText.GetObject<dynamic>();

                var className = new Regex(@"public class (\w+)").Match(fileContent).Groups[1].Value;



                #endregion



                #region generate entity DTO



                if ((request.MoreActions ?? Array.Empty<string>()).Contains("BuildDto"))

                {

                    var entityDtoResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.EntityDtoClass, fileContent, className, null, projectPath, @namespace);

                    logger.Append("generate entity DTO：");

                    logger.Append(entityResult.Log);

                }



                #endregion



                #region update SenparcEntities



                var updateSenparcEntitiesResult = await promptBuilderService.RunPromptAsync(aiSetting, Domain.PromptBuildType.UpdateSenparcEntities, className, className, entityResult.Context, projectPath, @namespace);

                logger.Append("update SenparcEntities：");

                logger.Append(entityResult.Log);



                #endregion



                #region run the migration

                if ((request.MoreActions ?? Array.Empty<string>()).Contains("BuildMigration"))

                {

                    Legacy comment translated to English.

                    logger.Append();

                    logger.Append("Migration startexecute");



                    #region attach DatabasePlant files

                    var databasePlantPath = Path.GetFullPath(Path.Combine(projectPath, "..", "Senparc.Web.DatabasePlant"));

                    var databasePlantCsprojPath = Path.Combine(databasePlantPath, "Senparc.Web.DatabasePlant.csproj");

                    Legacy comment translated to English.

                    var projectFolderName = Path.GetFileName(projectPath.TrimEnd(Path.DirectorySeparatorChar));

                    Legacy comment translated to English.

                    string newReferencePath = @$"..\{projectFolderName}\{projectFolderName}.csproj";

                    XDocument doc = XDocument.Load(databasePlantCsprojPath);



                    Legacy comment translated to English.  

                    var itemGroups = doc.Root.Elements("ItemGroup");



                    Legacy comment translated to English.  

                    foreach (var itemGroup in itemGroups)

                    {

                        var projectReferences = itemGroup.Elements("ProjectReference").ToList();

                        foreach (var reference in projectReferences)

                        {

                            reference.Remove();

                        }

                    }



                    Legacy comment translated to English.  

                    XElement newProjectReference = new XElement("ProjectReference", new XAttribute("Include", newReferencePath));

                    var itemGroupToAdd = itemGroups.FirstOrDefault() ?? new XElement("ItemGroup");

                    itemGroupToAdd.Add(newProjectReference);



                    if (itemGroupToAdd.Parent == null)

                    {

                        doc.Root.Add(itemGroupToAdd);

                    }



                    Legacy comment translated to English.  

                    doc.Save(databasePlantCsprojPath);



                    Legacy comment translated to English.

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

                    Legacy comment translated to English.

                    await requestObj.LoadData(this.ServiceProvider);



                    Legacy comment translated to English.

                    requestObj.ProjectPath = projectPath;

                    Legacy comment translated to English.

                    requestObj.DatabaseTypes = requestObj.DatabaseTypeOptions.Items.Select(z => z.Value).ToArray();

                    Legacy comment translated to English.

                    requestObj.OutputVerbose = true;



                    var databaseMigrationsAppService = base.ServiceProvider.GetRequiredService<DatabaseMigrationsAppService>();

                    var migrationResult = await databaseMigrationsAppService.AddMigration(requestObj);





                    logger.Append("executeend，whethersuccess：" + migrationResult.Success);

                    Legacy comment translated to English.

                    Legacy comment translated to English.



                    #region read logs from the cache



                    var tempId = migrationResult.RequestTempId;

                    var cache = this.ServiceProvider.GetObjectCacheStrategyInstance();

                    Legacy comment translated to English.

                    var migrationLog = await cache.GetAsync<string>(tempId);

                    logger.Append(migrationLog);



                    #endregion



                    logger.Append();

                }

                #endregion



                return logger.ToString();

            });

        }



        Legacy comment translated to English.



        Legacy comment translated to English.





        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.InitPrompt.Name", "Function.XncfBuilder.InitPrompt.Description", typeof(Register))]

        public async Task<StringAppResponse> InitPrompt(BuildXncf_InitPromptRequest request)

        {

            return await this.GetStringResponseAsync(async (response, logger) =>

            {

                var needOverride = request.Override;

                var aiModel = request.AIModel;



                var promptBuilderService = base.ServiceProvider.GetRequiredService<PromptBuilderService>();

                var log = await promptBuilderService.InitPromptAsync("XncfBuilderPlugin", needOverride, aiModel);

                logger.Append(log);



                logger.SaveLogs("InitPrompt");



                return log;

            });

        }



        */

        //Legacy comment translated to English.
        //public async Task<StringAppResponse> CreateAppService()
        //{ 

        //}
    }
}
