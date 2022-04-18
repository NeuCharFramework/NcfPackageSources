using _5990_Senparc.Xncf.TenantTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Tenant.Domain.DataBaseModel.Tests
{
    [TestClass()]
    public class TenantInfoTests : XncfTestBase
    {
        [TestMethod()]
        public void TenantInfoTest()
        {
            var tenantInfo = new TenantInfo("测试租户", true, "LOCALHOST");
            Assert.AreEqual("测试租户", tenantInfo.Name);
            Assert.AreEqual(true, tenantInfo.Enable);
            Assert.AreEqual("LOCALHOST", tenantInfo.TenantKey);
        }

        [TestMethod()]
        public void UpdateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ClearCacheTest()
        {
            Assert.Fail();
        }
    }
}