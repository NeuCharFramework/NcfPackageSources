using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Ncf.Core.Tests
{
    /// <summary>
    /// A test collection of a series of extension methods
    /// </summary>
    [TestClass]
    public class ExtensionTests : TestBase
    {
        [TestMethod]
        public void IsAssignableFromTest()
        {
            var result = typeof(IMultiTenancy).IsAssignableFrom(typeof(SysMenu));
            Assert.IsTrue(result);
        }
    }
}
