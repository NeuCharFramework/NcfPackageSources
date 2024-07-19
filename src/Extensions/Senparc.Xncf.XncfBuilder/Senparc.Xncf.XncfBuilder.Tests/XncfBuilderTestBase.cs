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

        IServiceCollection _services;
        protected override void RegisterServiceCollectionFinished(IServiceCollection services)
        {
            base.RegisterServiceCollectionFinished(services);

            _services = services;
        }

        public XncfBuilderTestBase() : base()
        {
            _senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(_senparcAiSetting);

            //_senparcAiSetting = new  SenparcAiSetting() { IsDebug = true };
            //_senparcAiSetting.GetSection("SenparcAiSetting").Bind(_senparcSetting);

            _services.AddSenparcAI(base.Configuration, _senparcAiSetting);

            _services.AddScoped<PromptService>();
            _services.AddScoped<PromptBuilderService>();

            _services.AddScoped<IAiHandler, SemanticAiHandler>();

            _serviceProvder = _services.BuildServiceProvider();

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