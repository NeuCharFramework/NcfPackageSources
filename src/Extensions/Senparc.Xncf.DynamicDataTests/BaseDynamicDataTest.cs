using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.DynamicData;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.Domain.Services;

namespace Senparc.Xncf.DynamicDataTests
{
    public class BaseDynamicDataTest_Seed : UnitTestSeedDataBuilder
    {

        public override async Task<DataList> ExecuteAsync(IServiceProvider serviceProvider)
        {
            DataList dataList = new DataList(nameof(BaseDynamicDataTest_Seed));

            // TableMetadata
            List<TableMetadata> tableMetadataList = new() {
                     new("User","�û���"){
                          ColumnMetadatas=new List<ColumnMetadata>(){
                               new ColumnMetadata(0,"Guid","Text",false,""),
                               new ColumnMetadata(0,"UserName","Text",false,""),
                               new ColumnMetadata(0,"Balance","Float",false,"0.0"),
                          }
                         },
                         new("Product","��Ʒ��"){
                          ColumnMetadatas = new List<ColumnMetadata>(){
                               new ColumnMetadata(0,"Guid","Text",false,""),
                               new ColumnMetadata(0,"Name","Text",false,""),
                               new ColumnMetadata(0,"Price","Float",false,"0.0"),
                          }
                         },
                         new("Order","������"){
                          ColumnMetadatas = new List<ColumnMetadata>(){
                               new ColumnMetadata(0,"Guid","Text",false,""),
                               new ColumnMetadata(0,"UserGuid","Text",false,""),
                               new ColumnMetadata(0,"ProductGuid","Text",false,""),
                               new ColumnMetadata(0,"Price","Float",false,"0.0"),
                               new ColumnMetadata(0,"State","Enums(Open,Paid,Closed)",false,"0.0"),
                          }
                     },
                };

            dataList.Add(tableMetadataList);
            return dataList;
        }

        public override async Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList)
        {
            var tableDataService = serviceProvider.GetRequiredService<TableDataService>();
            var columnMetadataService = serviceProvider.GetRequiredService<ColumnMetadataService>();

            // User ��
            var userTableColumns = await columnMetadataService.GetColumnDtos(1);
            var tableDataDtos = new List<TableDataDto>();

            //����һЩ����

            /* User ��
             * | Column   | Value  |
             * |----------|--------|
             * | Guid     |        |
             * | UserName | UserX  |
             * | Balance  |   X    |
             */

            var userData = new[] {
                new { Guid = Guid.NewGuid().ToString(), UserName = "User1", Balance = "0.00" },
                new { Guid = Guid.NewGuid().ToString(), UserName = "User2", Balance = "1.00" },
                new { Guid = Guid.NewGuid().ToString(), UserName = "User3", Balance = "2.00" },
            };

            foreach (var item in userData)
            {
                SetData(userTableColumns, tableDataDtos, nameof(item.Guid), item.Guid);
                SetData(userTableColumns, tableDataDtos, nameof(item.UserName), item.UserName);
                SetData(userTableColumns, tableDataDtos, nameof(item.Balance), item.Balance);
            }

            static void SetData(List<ColumnMetadataDto> userTableColumns, List<TableDataDto> tableDataDtos, string columnName, string value)
            {
                var column = userTableColumns.First(z => z.ColumnName == columnName);
                tableDataDtos.Add(new TableDataDto()
                {
                    TableId = column.TableMetadataId,
                    ColumnMetadataId = column.Id,
                    CellValue = value
                });
            }
        }
    }

    [TestClass]
    public class BaseDynamicDataTest : BaseNcfUnitTest
    {
        public BaseDynamicDataTest(Action<IServiceCollection> servicesRegister = null, UnitTestSeedDataBuilder seedDataBuilder = null)
     : base(servicesRegister, seedDataBuilder ?? new BaseDynamicDataTest_Seed())
        {

        }

        protected override void BeforeRegisterServiceCollection(IServiceCollection services)
        {
            base.BeforeRegisterServiceCollection(services);

            Console.WriteLine("BaseDynamicDataTest.BeforeRegisterServiceCollection");
        }

        protected override void RegisterServiceCollectionFinished(IServiceCollection services)
        {
            base.RegisterServiceCollectionFinished(services);
        }
    }
}