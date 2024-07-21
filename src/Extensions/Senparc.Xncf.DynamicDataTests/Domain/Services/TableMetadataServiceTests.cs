using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Xncf.DynamicData.Domain.Services;
using Senparc.Xncf.DynamicDataTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Services.Tests
{
    [TestClass()]
    public class TableMetadataServiceTests : BaseDynamicDataTest
    {
        [TestMethod()]
        public async Task GetTableMetadataDtoAsyncTest()
        {
            var service = base._serviceProvider.GetRequiredService<TableMetadataService>();

            {
                var tableMetadataDto = await service.GetTableMetadataDtoAsync(1);

                Assert.IsNotNull(tableMetadataDto);
                Assert.IsNotNull(tableMetadataDto.ColumnMetadatas);
                Assert.IsTrue(tableMetadataDto.ColumnMetadatas.Count > 0);//注意：InMemory 数据库中，这里会自动进行关联，无论底层代码是否 Include

                Console.WriteLine(tableMetadataDto.ToJson(true, new Newtonsoft.Json.JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                }));
            }

            {
                var tableMetadataDto = await service.GetObjectAsync(z => z.TableName.Contains("Product"));
                Assert.IsNotNull(tableMetadataDto);
                Assert.AreEqual("产品表",tableMetadataDto.Description);

            }

        }
    }
}