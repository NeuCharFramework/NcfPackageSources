using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.Ncf.Core.Tests;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;

namespace Senparc.Xncf.PromptRange.Tests
{
    [TestClass]
    public class XncfBuilderTestBase : TestBase
    {
        protected IServiceProvider _serviceProvder;

        public XncfBuilderTestBase() : base()
        {
            var senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(senparcAiSetting);

            base.registerService.UseSenparcAI(senparcAiSetting);

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
        }
    }
}