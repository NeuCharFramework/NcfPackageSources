using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Tests.Resources;

namespace Senparc.Ncf.XncfBase.Tests.Localization;

[TestClass]
public class XncfRegisterTextTests
{
    [TestMethod]
    public void RegisterText_ShouldResolveMenuAndDescriptionForCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var text = XncfRegisterText.Create<AttributeTestResource>(
                "Function",
                "中文菜单默认值",
                "中文描述默认值");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Assert.AreEqual("Test menu", text.MenuName);
            Assert.AreEqual("Test description", text.Description);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
            Assert.AreEqual("测试菜单", text.MenuName);
            Assert.AreEqual("测试说明", text.Description);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
