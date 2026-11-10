using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Localization;
using Senparc.Ncf.Core.Tests.Resources;

namespace Senparc.Ncf.Core.Tests.Localization;

[TestClass]
public class NcfFallbackStringLocalizerTests
{
    [TestMethod]
    public void MissingJapaneseResource_ShouldUseEnglishBeforeChinese()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var factory = new NcfFallbackStringLocalizerFactory();
            var localizer = factory.Create(typeof(AttributeTestResource));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");

            Assert.AreEqual("Test function", localizer["Function.Name"].Value);
            Assert.IsFalse(localizer["Function.Name"].ResourceNotFound);
            Assert.AreEqual(
                "Test function",
                ResourceStringLocalizer.Get(typeof(AttributeTestResource), "Function.Name"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [TestMethod]
    public void ChineseResource_ShouldRemainTheFinalSourceLanguage()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var factory = new NcfFallbackStringLocalizerFactory();
            var localizer = factory.Create(typeof(AttributeTestResource));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");

            Assert.AreEqual("测试功能", localizer["Function.Name"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [TestMethod]
    public void MissingEnglishResource_ShouldUseChinese()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var factory = new NcfFallbackStringLocalizerFactory();
            var localizer = factory.Create(typeof(AttributeTestResource));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");

            Assert.AreEqual("仅中文", localizer["NeutralOnly"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [TestMethod]
    public void GetAllStrings_ShouldMergeCurrentEnglishAndChineseResources()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var factory = new NcfFallbackStringLocalizerFactory();
            var localizer = factory.Create(typeof(AttributeTestResource));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            var values = localizer.GetAllStrings(includeParentCultures: true)
                .ToDictionary(item => item.Name, item => item.Value);

            Assert.AreEqual("Test function", values["Function.Name"]);
            Assert.AreEqual("Parameter||Parameter description", values["Parameter.Description"]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [TestMethod]
    public void AddNcfLocalization_ShouldConfigureGenericStringLocalizer()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            using var services = new ServiceCollection()
                .AddNcfLocalization()
                .BuildServiceProvider();

            var localizer = services.GetRequiredService<IStringLocalizer<AttributeTestResource>>();

            Assert.AreEqual("Test function", localizer["Function.Name"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
