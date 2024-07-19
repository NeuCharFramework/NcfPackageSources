using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Tests;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Tests
{
    public class TestModuleRegister : XncfRegisterBase, IXncfRegister
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

    public class TestModuleFunction : AppServiceBase
    {
        public TestModuleFunction(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        [FunctionRender("Agent 模板管理", "Agent 模板管理", typeof(TestModuleRegister))]
        public async Task<StringAppResponse> AgentTemplateManage(string str)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                logger.Append("收到请求：" + SystemTime.Now);
                return logger.ToString();
            });
        }
    }

    [TestClass]
    public class RegisterTest : TestBase
    {
        IServiceCollection _services;
        protected override void RegisterServiceCollectionFinished(IServiceCollection services)
        {
            base.RegisterServiceCollectionFinished(services);

            _services = services;
        }

        [TestMethod]
        public void StartEngineTest()
        {
            try
            {
                var result = _services.StartNcfEngine(base.Configuration, base.Env, null);
                Console.WriteLine(result);

                //基类中可能会已经执行过
            }
            catch (XncfFunctionException ex)
            {
                if (!ex.Message.Contains("已经存在相同 Uid 的模块"))
                {
                    Assert.Fail();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }

            Assert.IsTrue(Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList.Count > 0);
        }
    }
}
