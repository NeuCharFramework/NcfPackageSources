/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfBuilderTestBase.cs
    文件功能描述：XncfBuilderTestBase 相关实现
    
    
    创建标识：Senparc - 20231001
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

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