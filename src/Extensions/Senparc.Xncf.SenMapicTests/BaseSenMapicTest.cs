using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;

namespace Senparc.Xncf.SenMapicTests
{
    public class BaseSenMapicTest_Seed : UnitTestSeedDataBuilder
    {
        public override async Task<DataList> ExecuteAsync(IServiceProvider serviceProvider)
        {
            return new DataList("BaseSenMapicTest_Seed");
        }

        public override Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList)
        {
            return Task.CompletedTask;
        }
    }
    [TestClass]
    public class BaseSenMapicTest : BaseNcfUnitTest
    {
        public BaseSenMapicTest(Action<IServiceCollection> servicesRegister = null, UnitTestSeedDataBuilder seedDataBuilder = null) 
            : base(servicesRegister, seedDataBuilder??new BaseSenMapicTest_Seed())
        {
        }
    }
}
