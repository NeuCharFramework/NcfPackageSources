using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Tests;
using Senparc.Ncf.XncfBase;
using System;

namespace Senparc.Xncf.XncfBuilder.Tests
{
    [TestClass]
    public class MultiDatabaseDbSetKeysTests : TestBase
    {
        [TestMethod]
        public void RunTest()
        {
            var allEntitySetInfo = EntitySetKeys.GetAllEntitySetInfo();
            Console.WriteLine(allEntitySetInfo.Count);
        }
    }
}
