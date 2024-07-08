using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Tests;
using Senparc.Ncf.Repository;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Linq.Expressions;

namespace Senparc.Xncf.PromptRange.Tests
{
    internal class MockObjects
    {
        public IRepositoryBase<PromptItem> PromptItemRepository { get; set; }
        public List<PromptItem> PromptItems { get; set; } = new List<PromptItem>();
    }

    [TestClass]
    public class PromptTestBase : TestBase
    {
        protected IServiceProvider _serviceProvder;

        internal MockObjects MockObjects { get; set; }


        public PromptTestBase() : base()
        {
            base.registerService.UseSenparcAI();

            base.ServiceCollection.AddScoped<PromptService>();
            base.ServiceCollection.AddScoped<IAiHandler, SemanticAiHandler>();

            _serviceProvder = base.ServiceCollection.BuildServiceProvider();

            //Mock Arrange
            MockObjects = new MockObjects();
            var mockRepository = new Mock<IRepositoryBase<PromptItem>>();

            //模拟数据

            var expectedEntity = new PromptItem(new PromptRange.Models.DatabaseModel.Dto.PromptItemDto()
            {
                Id = 1,
                FullVersion = "2024.7.7.1-T1-A1",
                Content = "2024.7.7.1-T1-A1"
            });
            MockObjects.PromptItems.Add(expectedEntity);

            // 设置仓储Mock对象的GetItem方法  
            mockRepository.Setup(repo => repo.GetFirstOrDefaultObjectAsync(It.IsAny<Expression<Func<PromptItem, bool>>>()))
            .ReturnsAsync(expectedEntity);

            MockObjects.PromptItemRepository = mockRepository.Object;


        }
        protected override void ActionInServiceCollection()
        {
            var senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(senparcAiSetting);
            base.ServiceCollection.AddSenparcAI(base.Configuration, senparcAiSetting);

            base.ActionInServiceCollection();
        }

        [TestMethod]
        public void SenparcAiSettingTest()
        {
            var senparcAiSetting = Senparc.AI.Config.SenparcAiSetting;
            Assert.IsNotNull(senparcAiSetting);
            Assert.AreEqual(true, senparcAiSetting.IsDebug);
            Console.WriteLine(senparcAiSetting.ToJson(true));
        }
    }
}