/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FooterContentSanitizerTests.cs
    文件功能描述：Footer 默认值与安全链接渲染测试

    创建标识：Senparc - 20260802
----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Ncf.Core.Tests.Utility
{
    [TestClass]
    public class FooterContentSanitizerTests
    {
        [TestMethod]
        public void CreateDefaultFooterContent_ShouldUseSpecifiedYear()
        {
            var result = SystemConfig.CreateDefaultFooterContent(new System.DateTime(2031, 1, 1));

            Assert.AreEqual("© 2031 Senparc", result);
        }

        [TestMethod]
        public void Sanitize_ShouldKeepOnlySafeAbsoluteLinks()
        {
            var result = FooterContentSanitizer.Sanitize(
                "<a href=\"http://beian.miit.gov.cn\" onclick=\"alert(1)\">苏ICP备11023884号-12</a> © 2026 Senparc");

            Assert.AreEqual(
                "<a href=\"http://beian.miit.gov.cn\" target=\"_blank\" rel=\"noopener noreferrer\">苏ICP备11023884号-12</a> &#169; 2026 Senparc",
                result);
            Assert.AreEqual(result, FooterContentSanitizer.Sanitize(result));
        }

        [TestMethod]
        public void Sanitize_ShouldRemoveUnsafeLinkAndEncodeOtherHtml()
        {
            var result = FooterContentSanitizer.Sanitize(
                "<a href='javascript:alert(1)'>bad link</a><script>alert('x')</script>");

            Assert.IsFalse(result.Contains("javascript:", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Contains("<script", System.StringComparison.OrdinalIgnoreCase));
            StringAssert.Contains(result, "bad link");
            StringAssert.Contains(result, "&lt;script&gt;");
        }
    }
}
