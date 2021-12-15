using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Tests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Tests
{
    public class TestModule : XncfRegisterBase, IXncfRegister
    {
        public override string Name => "Senparc.Ncf.XncfBase.Tests.TestModule";

        public override string Uid => "000111";

        public override string Version => "1.0";

        public override string MenuName => "测试模块";
        public override string Icon => "fa fa-space-shuttle";//参考如：https://colorlib.com/polygon/gentelella/icons.html

        public override string Description => "这是测试模块的介绍";

        //public override IList<Type> Functions => new List<Type>() { typeof(FunctionBaseTest_Function) };

        public override Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            Console.WriteLine(installOrUpdate);
            return Task.CompletedTask;
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            Console.WriteLine("Uninstall");
            await unsinstallFunc().ConfigureAwait(false);
        }
    }

    [TestClass]
    public class RegisterTest : TestBase
    {
        [TestMethod]
        public void StartEngineTest()
        {
            var result = base.ServiceCollection.StartEngine(TestBase.Configuration, TestBase.Env);
            Console.WriteLine(result);
            Assert.IsTrue(Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList.Count > 0);
        }
    }
}
