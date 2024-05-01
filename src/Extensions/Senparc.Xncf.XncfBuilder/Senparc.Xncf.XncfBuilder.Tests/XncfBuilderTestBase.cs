using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Tests;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using System;

namespace Senparc.Xncf.PromptRange.Tests
{
    [TestClass]
    public class XncfBuilderTestBase : TestBase
    {
        protected IServiceProvider _serviceProvder;
        protected SenparcAiSetting _senparcAiSetting;

        public XncfBuilderTestBase() : base()
        {
            _senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(_senparcAiSetting);

            //_senparcAiSetting = new  SenparcAiSetting() { IsDebug = true };
            //_senparcAiSetting.GetSection("SenparcAiSetting").Bind(_senparcSetting);

            base.ServiceCollection.AddSenparcAI(base.Configuration, _senparcAiSetting);

            base.ServiceCollection.AddScoped<PromptService>();
            base.ServiceCollection.AddScoped<PromptBuilderService>();

            base.ServiceCollection.AddScoped<IAiHandler, SemanticAiHandler>();

            _serviceProvder = base.ServiceCollection.BuildServiceProvider();

            base.registerService.UseSenparcAI(/*senparcAiSetting*/);

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