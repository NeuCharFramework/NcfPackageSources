using System.Collections.Specialized;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.DynamicData;
using Senparc.Xncf.DynamicData.Domain.Services;

namespace Senparc.Xncf.DynamicDataTests
{
    [TestClass]
    public class BaseDynamicDataTest : BaseNcfUnitTest
    {
        public static Action<DataList> InitSeedData = dataList =>
        {
            // TableMetadata
            List<TableMetadata> tableMetadataList = new() {
                 new("User","用户表"){
                  ColumnMetadatas=new List<ColumnMetadata>(){
                       new ColumnMetadata(0,"Guid","Text",false,""),
                       new ColumnMetadata(0,"UserName","Text",false,""),
                       new ColumnMetadata(0,"Balance","Float",false,"0.0"),
                  }
                 },
                 new("Product","产品表"){ 
                  ColumnMetadatas = new List<ColumnMetadata>(){
                       new ColumnMetadata(0,"Guid","Text",false,""),
                       new ColumnMetadata(0,"Name","Text",false,""),
                       new ColumnMetadata(0,"Price","Float",false,"0.0"),
                  }
                 },
                 new("Order","订单表"){ 
                  ColumnMetadatas = new List<ColumnMetadata>(){
                       new ColumnMetadata(0,"Guid","Text",false,""),
                       new ColumnMetadata(0,"UserGuid","Text",false,""),
                       new ColumnMetadata(0,"ProductGuid","Text",false,""),
                       new ColumnMetadata(0,"Price","Float",false,"0.0"),
                       new ColumnMetadata(0,"State","Enums(Open,Paid,Closed)",false,"0.0"),
                  }
                 },
            };

            for (int i = 1; i <= tableMetadataList.Count; i++)
            {
                var data = tableMetadataList[i - 1];
                //data.Id = i;
            }

            dataList.Add(tableMetadataList);
        };

        //protected TableDataService _tableDataService;
        protected TableMetadataService _tableMetadataService;

        public BaseDynamicDataTest(Action<IServiceCollection> servicesRegister = null, Action<DataList> initSeedData = null)
            : base(servicesRegister, initSeedData ?? InitSeedData)
        {

        }

        protected override void BeforeRegisterServiceCollection(IServiceCollection services)
        {
            base.BeforeRegisterServiceCollection(services);

            Console.WriteLine("BaseDynamicDataTest.BeforeRegisterServiceCollection");

            services.AddScoped<TableMetadataService>();
        }

        protected override void RegisterServiceCollectionFinished(IServiceCollection services)
        {
            base.RegisterServiceCollectionFinished(services);
        }
    }
}