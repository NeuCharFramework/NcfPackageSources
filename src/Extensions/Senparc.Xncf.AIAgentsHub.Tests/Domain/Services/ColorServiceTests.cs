using Microsoft.AspNetCore.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Tests;
using Senparc.Xncf.AIAgentsHub.Domain.Services;
using Senparc.Xncf.AIAgentsHub.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIAgentsHub.Domain.Services.Tests
{
    [TestClass()]
    public class ColorServiceTests : AiAgentsHubTestBase
    {
        private readonly ColorService _colorService;

        public ColorServiceTests()
        {

        }

        [TestMethod()]
        public async Task CreateNewColorTest()
        {
            var colorDto = await _colorService.CreateNewColor();

            Assert.IsNotNull(colorDto);

            await Console.Out.WriteLineAsync(colorDto.ToJson(true));
        }
    }
}