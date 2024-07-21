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
        protected SenparcAiSetting _senparcAiSetting;

        protected override void BeforeRegisterServiceCollection(IServiceCollection services)
        {
            base.BeforeRegisterServiceCollection(services);
        }

        protected override void RegisterServiceCollectionFinished(IServiceCollection services)
        {
            base.RegisterServiceCollectionFinished(services);

            _senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(_senparcAiSetting);

            //_senparcAiSetting = new  SenparcAiSetting() { IsDebug = true };
            //_senparcAiSetting.GetSection("SenparcAiSetting").Bind(_senparcSetting);

            services.AddSenparcAI(base.Configuration, _senparcAiSetting);

            services.AddScoped<PromptService>();
            services.AddScoped<PromptBuilderService>();

            services.AddScoped<IAiHandler, SemanticAiHandler>();
        }

        public XncfBuilderTestBase() : base()
        {
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