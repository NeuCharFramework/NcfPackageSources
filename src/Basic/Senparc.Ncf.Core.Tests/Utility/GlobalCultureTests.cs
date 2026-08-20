using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Utility.Helpers;
using System.Globalization;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Tests.Utility
{
    [TestClass]
    public class GlobalCultureTests
    {
        private CultureInfo _originalCulture;
        private CultureInfo _originalUiCulture;

        [TestInitialize]
        public void Initialize()
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUiCulture = CultureInfo.CurrentUICulture;
            GlobalCulture.ResetCurrentLanguage();
        }

        [TestCleanup]
        public void Cleanup()
        {
            GlobalCulture.ResetCurrentLanguage();
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }

        [DataTestMethod]
        [DataRow("zh-CN", SystemLanguage.Chinese)]
        [DataRow("en-US", SystemLanguage.English)]
        [DataRow("ja-JP", SystemLanguage.Japanese)]
        [DataRow("fr-FR", SystemLanguage.French)]
        [DataRow("es-ES", SystemLanguage.Spanish)]
        [DataRow("ru-RU", SystemLanguage.Russian)]
        public void CurrentLanguage_ShouldFollowCurrentUiCulture(
            string cultureName,
            SystemLanguage expectedLanguage)
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            Assert.AreEqual(expectedLanguage, GlobalCulture.CurrentLanguage);
        }

        [DataTestMethod]
        [DataRow("EN-us", "en")]
        [DataRow("ja-JP", "ja")]
        [DataRow("zh", "zh-CN")]
        public void TryNormalizeCulture_ShouldReturnCanonicalSupportedCulture(
            string requestedCulture,
            string expectedCulture)
        {
            var success = NcfLocalizationOptions.TryNormalizeCulture(
                requestedCulture,
                out var normalizedCulture);

            Assert.IsTrue(success);
            Assert.AreEqual(expectedCulture, normalizedCulture);
        }

        [TestMethod]
        public void TryNormalizeCulture_ShouldRejectUnknownCulture()
        {
            var success = NcfLocalizationOptions.TryNormalizeCulture(
                "not-a-culture",
                out var normalizedCulture);

            Assert.IsFalse(success);
            Assert.AreEqual(NcfLocalizationOptions.DefaultCulture, normalizedCulture);
        }

        [TestMethod]
        public async Task ExplicitLanguage_ShouldBeIsolatedBetweenAsyncFlows()
        {
            var chineseTask = Task.Run(async () =>
            {
                GlobalCulture.CurrentLanguage = SystemLanguage.Chinese;
                await Task.Yield();
                return GlobalCulture.CurrentLanguage;
            });

            var englishTask = Task.Run(async () =>
            {
                GlobalCulture.CurrentLanguage = SystemLanguage.English;
                await Task.Yield();
                return GlobalCulture.CurrentLanguage;
            });

            var results = await Task.WhenAll(chineseTask, englishTask);

            CollectionAssert.AreEqual(
                new[] { SystemLanguage.Chinese, SystemLanguage.English },
                results);
        }

        [TestMethod]
        public void InvokeDefault_ShouldUseEnglishFallbackForJapaneseWhenNotProvided()
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            var result = string.Empty;

            GlobalCulture.Create()
                .SetChinese(() => result = "zh-CN")
                .SetEnglish(() => result = "en")
                .InvokeDefault();

            Assert.AreEqual("en", result);
        }
    }
}
