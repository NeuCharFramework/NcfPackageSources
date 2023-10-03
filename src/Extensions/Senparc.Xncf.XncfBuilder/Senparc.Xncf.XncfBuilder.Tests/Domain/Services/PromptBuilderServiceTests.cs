using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Annotations;
using Senparc.Xncf.PromptRange.Tests;
using System;
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
            _service = base._serviceProvder.GetRequiredService<PromptBuilderService>();
        }

        [TestMethod()]
        public async Task RunPromptTest()
        {
            var entityName = "MyClass";

            var input = $"这个领域用于控制所有的 Prompt 核心业务逻辑，包括使用 Prompt 操作大预言模型所需的所有必要的参数，类名叫：{entityName}，用于管理一组相关联的 Prompt，并统一配置其参数。生成的属性中需要包含常规的 LLM 被调用时的所需的参数，尽可能完整，包括但不仅限于： MaxToken、Temperature、TopP、FrequencyPenalty、ResultsPerPrompt、StopSequences、ChatSystemPrompt、TokenSelectionBiases，等等；除此以外，属性还需要包含用于评估 Prompt 效果所需要的必要参数，以及 Name 等常规实体类应该有的参数。";

            var projectPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "XncfBuilderTest");

            CO2NET.Helpers.FileHelper.TryCreateDirectory(projectPath);//重建目录

            var result = await _service.RunPromptAsync(PromptBuildType.EntityClass, input, projectPath);

            await Console.Out.WriteLineAsync("Run Prompt Result");
            await Console.Out.WriteLineAsync(result);

            var promptGroupFilePath = Path.Combine(projectPath, $"{entityName}.cs");
            Assert.IsTrue(File.Exists(promptGroupFilePath));
            Assert.IsTrue(File.Exists(Path.Combine(projectPath, $"Dto/{entityName}Dto.cs")));

            var promptGroupFileContent = File.ReadAllText(promptGroupFilePath);
            Assert.IsTrue(promptGroupFileContent.Contains($"public class {entityName}"));

            var senparcEntitiesFile = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel", "PromptRangeSenparcEntities.cs");
            Assert.IsTrue(File.Exists(senparcEntitiesFile));

            var newSenparcEntitiesContent = File.ReadAllText(senparcEntitiesFile);
            Assert.IsTrue(newSenparcEntitiesContent.Contains($"public DbSet<{entityName}> {entityName}es {{ get; set; }}"));
        }

    }
}