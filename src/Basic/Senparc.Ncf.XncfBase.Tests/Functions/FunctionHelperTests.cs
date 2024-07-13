using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Tests;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        public async Task<StringAppResponse> SetConfig(SetConfigFunctionAppRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                return request.BackupPath;
            });
        }
    }


    [TestClass()]
    public class FunctionHelperTests : BaseXncfBaseTest
    {
        [TestMethod()]
        public void GetFunctionParameterInfoAsyncTest()
        {
            Console.WriteLine("FunctionRenderCollection: " + Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.Keys.Select(z => z.FullName).ToJson(true));

            var registerList = XncfRegisterManager.RegisterList;
            Console.WriteLine("Register List:" + registerList.Select(z => z.Name).ToJson(true));

            var functionBag = Senparc.Ncf.XncfBase.Register.FunctionRenderCollection[typeof(TestModuleRegister)].Values.First();
            var result = FunctionHelper.GetFunctionParameterInfoAsync(base._serviceProvider, functionBag, true).GetAwaiter().GetResult();

            Assert.IsTrue(result.Count > 0);
            Console.WriteLine(result.ToJson(true));
        }
    }
}