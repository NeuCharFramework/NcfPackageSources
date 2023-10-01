using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services.Tests
{
    [TestClass()]
    public class PromptServiceTests : PromptTestBase
    {
        protected PromptService _service;

        public PromptServiceTests()
        {
            _service = base._serviceProvder.GetRequiredService<PromptService>();
        }

        [TestMethod()]
        public async Task GetPromptResultTest()
        {
            Assert.IsNotNull(this._service);

            var prompt = "这个领域用于控制所有的 Prompt 核心业务逻辑，包括使用 Prompt 操作大预言模型所需的所有必要的参数，类名叫：PromptItem";
            var result = await this._service.GetPromptResultAsync(XncfBuilderPromptType.EntityClass, prompt);

            Assert.IsNotNull(result);
            await Console.Out.WriteLineAsync(result);
        }
    }
}