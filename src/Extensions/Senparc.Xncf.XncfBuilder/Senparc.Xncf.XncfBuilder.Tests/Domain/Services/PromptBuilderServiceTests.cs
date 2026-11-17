//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.SemanticKernel;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Senparc.AI.Entities;
//using Senparc.CO2NET.Helpers;
//using Senparc.Ncf.Core.Annotations;
//using Senparc.Xncf.AIKernel.Domain.Services;
//using Senparc.Xncf.AIKernel.Models;
//using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
//using Senparc.Xncf.PromptRange.Domain.Services;
//using Senparc.Xncf.PromptRange.Tests;
//using Senparc.Xncf.XncfBuilder.Domain.Services.Plugins;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace Senparc.Xncf.XncfBuilder.Domain.Services.Tests
//{
//    /// <summary>
//    /// PromptBuilder test
//    /// </summary>
//    [TestClass()]
//    public class PromptBuilderServiceTest : XncfBuilderTestBase
//    {
//        protected PromptBuilderService _promptBuilderService;

//        public PromptBuilderServiceTest()
//        {
//Legacy comment translated to English.
//            //var promptParameter = new PromptConfigParameter()
//            //{
//            //    MaxTokens = 2000,
//            //    Temperature = 0.7,
//            //    TopP = 0.5,
//            //};
//            var promptRangeService = new PromptRangeService(base.GetRespositoryObject<Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange>(), base._serviceProvider);
//            var aiModelService = new AIModelService(base.GetRespositoryObject<AIModel>(),base._serviceProvider);
//            var promptItemService = new PromptItemService(base.GetRespositoryObject<PromptItem>(), base._serviceProvider, aiModelService, promptRangeService);
//            var promptService = new PromptService(promptRangeService, promptItemService, null);
//            var llmModelService = new LlModelService(base.GetRespositoryObject<LlModel>(), base._serviceProvider);
//            var promptResultService = new PromptResultService(base.GetRespositoryObject<PromptResult>(), base._serviceProvider, promptItemService, promptRangeService, llmModelService);
//            _promptBuilderService = new PromptBuilderService(promptService, promptRangeService, promptItemService, promptResultService);
//        }

//        [TestMethod()]
//        public async Task RunPromptTest()
//        {
//            //var entityName = "MyTestClass";

//Legacy comment translated to English.

//            var projectPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "XncfBuilderTest");

//            try
//            {
//Legacy comment translated to English.
//            }
//            catch { }
//            finally
//            {
//Legacy comment translated to English.
//            }


//            var @namespace = "Senparc.Xncf.UnitTestProject.Models.DatabaseModel";

//            #region Entity generate

//            var setting = Senparc.AI.Config.SenparcAiSetting;
//            var entityResult = await _promptBuilderService.RunPromptAsync(setting, PromptBuildType.EntityClass, input, null, null, projectPath, @namespace);

//            var entityName = new Regex(@"public class (\w+)").Match(entityResult.FileResult.FileContents.First().Value).Groups[1].Value;

//            await Console.Out.WriteLineAsync("Run Entity Class Prompt Result");
//            await Console.Out.WriteLineAsync(entityResult.Log);

//            var promptGroupFilePath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel", $"{entityName}.cs");

//            Assert.IsTrue(File.Exists(promptGroupFilePath));

//            var promptGroupFileContent = File.ReadAllText(promptGroupFilePath);

//            Assert.IsTrue(promptGroupFileContent.Contains($"public class {entityName}"));

//            #endregion

//            await Console.Out.WriteLineAsync("\n===========\n");

//Legacy comment translated to English.
//            var className = new Regex(@"public class (\w+)").Match(promptGroupFileContent).Groups[1].Value;

//            #region Entity DTO generate

//            var entityCode = promptGroupFileContent;// entityResult.ResponseText.GetObject<List<FileGenerateResult>>()[0].EntityCode;

//            var entityDtoResult = await _promptBuilderService.RunPromptAsync(setting, PromptBuildType.EntityDtoClass, entityCode, className, null, projectPath, @namespace);

//            Assert.IsTrue(File.Exists(Path.Combine(projectPath, "Domain", "Models", "DatabaseModel", $"Dto/{className}Dto.cs")));

//            await Console.Out.WriteLineAsync(entityDtoResult.Log);
//            await Console.Out.WriteLineAsync("Run Entity Class Prompt Result");

//            #endregion

//            await Console.Out.WriteLineAsync("\n===========\n");

//            #region UpdateSenparcEntities

//            var updateSenparcEntitiesResult = await _promptBuilderService.RunPromptAsync(setting, PromptBuildType.UpdateSenparcEntities, input, className, entityResult.Context, projectPath, "Senparc.Xncf.UnitTestProject");

//            var senparcEntitiesFile = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel", "PromptRangeSenparcEntities.cs");
//            Assert.IsTrue(File.Exists(senparcEntitiesFile));

//            var newSenparcEntitiesContent = File.ReadAllText(senparcEntitiesFile);
//            Assert.IsTrue(newSenparcEntitiesContent.Contains($"public DbSet<{className}> "));

//            #endregion
//        }

//    }
//}