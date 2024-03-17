using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.PromptRange.Tests;
using Senparc.Xncf.XncfBuilder.Domain;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Tests.Domain.Services
{
    /// <summary>
    /// PromptRange 模块的初始化代码
    /// <para>注意：这会直接修改现有代码！</para>
    /// </summary>
    //[TestClass]//此测试用于项目代码的直接测试，默认情况下请关闭
    public class PromptRangeGenerateTests : XncfBuilderTestBase
    {
        protected PromptBuilderService _service;
        private string _projectPath = Path.Combine("Y:\\Senparc 项目\\NeuCharFramework\\NcfPackageSources\\src\\Extensions\\Senparc.Xncf.PromptRange\\", "Domain", "Models", "DatabaseModel");

        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        private async Task RunPromptBaseTest(string entityName, string input)
        {
            CO2NET.Helpers.FileHelper.TryCreateDirectory(_projectPath);

            var senparcAiSetting = Senparc.AI.Config.SenparcAiSetting;
            var result = await _service.RunPromptAsync(senparcAiSetting, PromptBuildType.EntityClass, input, null, _projectPath);

            Assert.IsTrue(result.Result.Contains("保存文件"));
            await Console.Out.WriteLineAsync(result.Result);

            var promptGroupFilePath = Path.Combine(_projectPath, $"{entityName}.cs");
            Assert.IsTrue(File.Exists(promptGroupFilePath));
            Assert.IsTrue(File.Exists(Path.Combine(_projectPath, $"Dto/{entityName}Dto.cs")));

            var promptGroupFileContent = File.ReadAllText(promptGroupFilePath);
            Assert.IsTrue(promptGroupFileContent.Contains($"public class {entityName}"));

            var senparcEntitiesFile = Path.Combine(_projectPath, "Domain", "Models", "DatabaseModel", "PromptRangeSenparcEntities.cs");
            Assert.IsTrue(File.Exists(senparcEntitiesFile));

            var newSenparcEntitiesContent = File.ReadAllText(senparcEntitiesFile);
            Assert.IsTrue(newSenparcEntitiesContent.Contains($"public DbSet<{entityName}> {entityName}s {{ get; set; }}"));
        }

        public PromptRangeGenerateTests()
        {
            _service = base._serviceProvder.GetRequiredService<PromptBuilderService>();
        }



        [TestMethod()]
        public async Task PromptGroupTest()
        {
            var entityName = "PromptGroup";
            var input = $"这个领域用于控制所有的 Prompt 核心业务逻辑，包括使用 Prompt 操作大预言模型所需的所有必要的参数，类名叫：{entityName}，用于管理一组相关联的 Prompt，并统一配置其参数。生成的属性中需要包含常规的 LLM 被调用时的所需的参数，尽可能完整，包括但不仅限于： MaxToken、Temperature、TopP、FrequencyPenalty、ResultsPerPrompt、StopSequences、ChatSystemPrompt、TokenSelectionBiases，等等；除此以外，属性还需要包含用于评估 Prompt 效果所需要的必要参数，以及 Name 等常规实体类应该有的参数。";

            await RunPromptBaseTest(entityName, input);
        }

        [TestMethod()]
        public async Task PromptItemTest()
        {
            //input 仅做演示
            var entityName = "PromptItem";
            var input = $"这个领域用于控制所有的 Prompt 核心业务逻辑，包括使用 Prompt 操作大预言模型所需的所有必要的参数，类名叫：{entityName}，用于管理一组相关联的 Prompt，并统一配置其参数。生成的属性中需要包含常规的 LLM 被调用时的所需的参数，尽可能完整，包括但不仅限于： MaxToken、Temperature、TopP、FrequencyPenalty、ResultsPerPrompt、StopSequences、ChatSystemPrompt、TokenSelectionBiases，等等；除此以外，属性还需要包含用于评估 Prompt 效果所需要的必要参数，以及 Name 等常规实体类应该有的参数。";

            await RunPromptBaseTest(entityName, input);
        }

        [TestMethod()]
        public async Task PromptResult()
        {
            var entityName = "PromptResult";
            var input = $"";

            await RunPromptBaseTest(entityName, input);
        }
    }
}
