using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.AI;
using Senparc.Ncf.Core.Tests;

namespace Senparc.Xncf.PromptRange.Tests
{
    [TestClass]
    public class PromptTestBase : TestBase
    {
        public PromptTestBase() : base()
        {
            var senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
            base.Configuration.GetSection("SenparcAiSetting").Bind(senparcAiSetting);

            base.registerService.UseSenparcAI(senparcAiSetting);
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