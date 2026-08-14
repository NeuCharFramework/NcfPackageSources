using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Tests.Resources;
using System.Globalization;

namespace Senparc.Ncf.Core.Tests.AppServices
{
    [TestClass]
    public class LocalizedMetadataTests
    {
        [TestMethod]
        public void FunctionRenderAttribute_ShouldFollowCurrentRequestCulture_WhenCached()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var attribute = new FunctionRenderAttribute(
                    typeof(AttributeTestResource),
                    "Function.Name",
                    "Function.Description",
                    typeof(LocalizedMetadataTests));

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                Assert.AreEqual("测试功能", attribute.Name);
                Assert.AreEqual("测试说明", attribute.Description);

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
                Assert.AreEqual("Test function", attribute.Name);
                Assert.AreEqual("Test description", attribute.Description);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [TestMethod]
        public void LocalizedDescriptionAttribute_ShouldUseResourceAndFallback()
        {
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");

                var localized = new LocalizedDescriptionAttribute(
                    typeof(AttributeTestResource),
                    "Parameter.Description");
                var fallback = new LocalizedDescriptionAttribute(
                    typeof(AttributeTestResource),
                    "Missing.Key",
                    "Fallback");

                Assert.AreEqual("Parameter||Parameter description", localized.Description);
                Assert.AreEqual("Fallback", fallback.Description);
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [TestMethod]
        public void FunctionRenderAttribute_ShouldAllowExplicitAiOptOut()
        {
            var attribute = new FunctionRenderAttribute("Safe workflow", "description", typeof(LocalizedMetadataTests));
            Assert.IsTrue(attribute.AllowAiInvocation);

            attribute.AllowAiInvocation = false;
            Assert.IsFalse(attribute.AllowAiInvocation);
        }
    }
}
