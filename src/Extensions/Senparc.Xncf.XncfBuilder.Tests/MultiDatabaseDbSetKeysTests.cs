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
        public MultiDatabaseDbSetKeysTests()
        {
            Senparc.Ncf.Core.Register.TryRegisterMiniCore(services => { });
            Senparc.Ncf.XncfBase.Register.StartEngine(base.ServiceCollection, TestBase.Configuration);
            //Senparc.Ncf.XncfBase.Register.UseXncfModules()
        }

        [TestMethod]
        public void RunTest()
        {
            var allEntitySetInfo = EntitySetKeys.GetAllEntitySetInfo();
            Console.WriteLine(XncfRegisterManager.RegisterList.Count);
            Console.WriteLine(allEntitySetInfo.Count);
        }
    }
}
