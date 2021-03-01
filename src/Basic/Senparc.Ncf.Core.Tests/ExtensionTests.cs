using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Tests
{
    /// <summary>
    /// 一系列扩展方法的测试集合
    /// </summary>
    [TestClass]
    public class ExtensionTests
    {
        [TestMethod]
        public void IsAssignableFromTest()
        {
            var result = typeof(IMultiTenancy).IsAssignableFrom(typeof(SysMenu));
            Assert.IsTrue(result);
        }
    }
}
