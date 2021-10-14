using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Tests;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase.Tests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Functions.Tests
{
    public class TestFunctionAppService : AppServiceBase
    {
        public TestFunctionAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public class SetConfigFunctionAppRequest : FunctionAppRequestBase
        {
            [Required]
            [MaxLength(300)]
            [System.ComponentModel.Description("自动备份周期（分钟）||0 则为不自动备份")]
            public int BackupCycleMinutes { get; set; }
            [Required]
            [MaxLength(300)]
            [System.ComponentModel.Description("备份路径||本地物理路径，如：E:\\Senparc\\Ncf\\NCF.bak")]
            public string BackupPath { get; set; }

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                BackupCycleMinutes = 999;
                BackupPath = "Test BackupCycleMinutes";

            }
        }

        [FunctionRender("设置参数", "设置备份间隔时间、备份文件路径等参数", typeof(Register))]
        public AppResponseBase<string> SetConfig(SetConfigFunctionAppRequest request)
        {
            return this.GetResponse<AppResponseBase<string>, string>((response, logger) =>
            {
                return request.BackupPath;
            });
        }
    }


    [TestClass()]
    public class FunctionHelperTests : RegisterTest
    {
        [TestMethod()]
        public void GetFunctionParameterInfoAsyncTest()
        {
            base.StartEngineTest();

            var serviceProvider = base.ServiceCollection.BuildServiceProvider();
            var functionBag = Senparc.Ncf.XncfBase.Register.FunctionRenderCollection[typeof(TestModule)].Values.First();
            var result = FunctionHelper.GetFunctionParameterInfoAsync(base.ServiceCollection.BuildServiceProvider(), functionBag, true).GetAwaiter().GetResult();

            Assert.IsTrue(result.Count > 0);
            Console.WriteLine(result.ToJson(true));
        }
    }
}