using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Tests;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;

namespace Senparc.Xncf.PromptRange.Tests
{
    [TestClass]
    public class PromptTestBase : TestBase
    {
        protected IServiceProvider _serviceProvder;

        protected override void ActionInServiceCollection()
        {
            var senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(senparcAiSetting);
            base.ServiceCollection.AddSenparcAI(base.Configuration, senparcAiSetting);

            base.ActionInServiceCollection();
        }

        public PromptTestBase() : base()
        {


            base.registerService.UseSenparcAI();

            base.ServiceCollection.AddScoped<PromptService>();
            base.ServiceCollection.AddScoped<IAiHandler, SemanticAiHandler>();

            _serviceProvder = base.ServiceCollection.BuildServiceProvider();
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