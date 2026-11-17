/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: XncfBuilderTestBase.cs

    File description: XncfBuilderTestBase related implementation

    

    

    Created by: Senparc - 20231001

    

    Modified by: Senparc - 20260702

    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling



----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.AgentKernel;
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

            _senparcAiSetting = new Senparc.AI.AgentKernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(_senparcAiSetting);

            //_senparcAiSetting = new  SenparcAiSetting() { IsDebug = true };
            //_senparcAiSetting.GetSection("SenparcAiSetting").Bind(_senparcSetting);

            services.AddSenparcAI(base.Configuration, _senparcAiSetting);

            services.AddScoped<PromptService>();
            services.AddScoped<PromptBuilderService>();

            services.AddScoped<IAiHandler, AgentAiHandler>();
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