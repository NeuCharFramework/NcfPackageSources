using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Xncf.AIAgentsHub.Tests;

namespace Senparc.Xncf.AIAgentsHub.Domain.Services.Tests
{
    [TestClass()]
    public class ColorServiceTests : AiAgentsHubTestBase
    {
        private readonly ColorService _colorService;

        public ColorServiceTests()
        {
            _colorService = new ColorService(base.GetRespositoryObject<Color>(), base._serviceProvider);
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