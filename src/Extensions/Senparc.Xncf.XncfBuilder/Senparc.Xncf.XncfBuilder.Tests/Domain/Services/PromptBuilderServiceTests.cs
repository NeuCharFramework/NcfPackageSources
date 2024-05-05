using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.AI.Entities;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Annotations;
using Senparc.Xncf.PromptRange.Tests;
using Senparc.Xncf.XncfBuilder.Domain.Services.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Tests
{
    /// <summary>
    /// PromptBuilder 测试
    /// </summary>
    [TestClass()]
    public class PromptBuilderServiceTest : XncfBuilderTestBase
    {
        protected PromptBuilderService _service;

        public PromptBuilderServiceTest()
        {
            //说明：skprompt 的 maxToken 在文件中配置
            //var promptParameter = new PromptConfigParameter()
            //{
            //    MaxTokens = 2000,
            //    Temperature = 0.7,
            //    TopP = 0.5,
            //};

            _service = base._serviceProvder.GetRequiredService<PromptBuilderService>();
        }

        [TestMethod()]
        public async Task RunPromptTest()
        {
            var entityName = "MyClass";

            var input = $"这个领域用于控制所有的 Prompt 核心业务逻辑，包括使用 Prompt 操作大预言模型所需的所有必要的参数，类名叫：{entityName}，用于管理一组相关联的 Prompt，并统一配置其参数。生成的属性中需要包含常规的 LLM 被调用时的所需的参数，尽可能完整，包括但不仅限于： MaxToken、Temperature、TopP、FrequencyPenalty、ResultsPerPrompt、StopSequences、ChatSystemPrompt、TokenSelectionBiases，等等；除此以外，属性还需要包含用于评估 Prompt 效果所需要的必要参数，以及 Name 等常规实体类应该有的参数。";

            var projectPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "XncfBuilderTest");

            CO2NET.Helpers.FileHelper.TryCreateDirectory(projectPath);//重建目录

            #region Entity 生成
            
            var setting = Senparc.AI.Config.SenparcAiSetting;
            var entityResult = await _service.RunPromptAsync(setting, PromptBuildType.EntityClass, input, null, projectPath, "Senparc.Xncf.UnitTestProject");

            await Console.Out.WriteLineAsync("Run Entity Class Prompt Result");
            await Console.Out.WriteLineAsync(entityResult.Result);

            var promptGroupFilePath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel", $"{entityName}.cs");
            Assert.IsTrue(File.Exists(promptGroupFilePath));

            var promptGroupFileContent = File.ReadAllText(promptGroupFilePath);
            Assert.IsTrue(promptGroupFileContent.Contains($"public class {entityName}"));

            #endregion

            await Console.Out.WriteLineAsync("\n===========\n");

            #region Entity DTO 生成

            var entityCode = entityResult.ResponseText.GetObject<List<FileGenerateResult>>()[0].EntityCode;

            var entityDtoResult = await _service.RunPromptAsync(setting, PromptBuildType.EntityDtoClass, entityCode, null, projectPath, "Senparc.Xncf.UnitTestProject");
            Assert.IsTrue(File.Exists(Path.Combine(projectPath, "Domain", "Models", "DatabaseModel", $"Dto/{entityName}Dto.cs")));

            await Console.Out.WriteLineAsync(entityDtoResult.Result);
            await Console.Out.WriteLineAsync("Run Entity Class Prompt Result");

            #endregion

            await Console.Out.WriteLineAsync("\n===========\n");

            #region UpdateSenparcEntities

            var updateSenparcEntitiesResult = await _service.RunPromptAsync(setting, PromptBuildType.UpdateSenparcEntities, input, entityResult.Context, projectPath, "Senparc.Xncf.UnitTestProject");

            var senparcEntitiesFile = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel", "PromptRangeSenparcEntities.cs");
            Assert.IsTrue(File.Exists(senparcEntitiesFile));

            var newSenparcEntitiesContent = File.ReadAllText(senparcEntitiesFile);
            Assert.IsTrue(newSenparcEntitiesContent.Contains($"public DbSet<{entityName}> {entityName}es {{ get; set; }}") || newSenparcEntitiesContent.Contains($"public DbSet<{entityName}> {entityName}s {{ get; set; }}"));

            #endregion
        }

    }
}