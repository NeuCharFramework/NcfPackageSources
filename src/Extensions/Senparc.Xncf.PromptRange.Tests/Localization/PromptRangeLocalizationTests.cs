using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Senparc.Xncf.PromptRange.Tests.Localization;

[TestClass]
public class PromptRangeLocalizationTests
{
    [TestMethod]
    public void TypedLocalizer_ShouldResolveDashboardResources()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddLocalization();

            using var serviceProvider = services.BuildServiceProvider();
            var localizer = serviceProvider.GetRequiredService<IStringLocalizer<PromptRangeResource>>();
            var localized = localizer["Dashboard.AddModel"];

            Assert.IsFalse(localized.ResourceNotFound);
            Assert.AreEqual("新增模型", localized.Value);
            Assert.AreEqual("登录已过期，请重新登录", localizer["Auth.SessionExpired"].Value);
            Assert.AreEqual("无权访问当前功能", localizer["Auth.AccessDenied"].Value);
            Assert.IsTrue(localizer.GetAllStrings(includeParentCultures: true)
                .Any(item => item.Name == "Dashboard.AddModel" && item.Value == "新增模型"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [TestMethod]
    public void ModuleLocalizationPartial_ShouldUseUniqueCompiledPath()
    {
        var compiledViewIdentifiers = typeof(PromptRangeResource).Assembly
            .GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType.FullName ==
                                "Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute")
            .Select(attribute => attribute.ConstructorArguments[2].Value as string)
            .Where(identifier => identifier is not null)
            .ToArray();

        CollectionAssert.Contains(
            compiledViewIdentifiers,
            "/Areas/Admin/Pages/Shared/_PromptRangeLocalizationScripts.cshtml");
        CollectionAssert.DoesNotContain(
            compiledViewIdentifiers,
            "/Areas/Admin/Pages/Shared/_LocalizationScripts.cshtml");
    }
}
