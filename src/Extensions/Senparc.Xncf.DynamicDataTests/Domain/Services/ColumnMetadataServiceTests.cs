using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.DynamicData.Domain.Models.Extensions;
using Senparc.Xncf.DynamicData.Domain.Services;
using Senparc.Xncf.DynamicDataTests;
using Senparc.Xncf.DynamicDataTests.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Services.Tests
{
    [TestClass()]
    public class ColumnMetadataServiceTests : BaseDynamicDataTest
    {
        ColumnMetadataService _columnMetadataService;

        public ColumnMetadataServiceTests()
        {
            _columnMetadataService = base._serviceProvider.GetRequiredService<ColumnMetadataService>();
        }

        [TestMethod()]
        public async Task GetColumnDtosTest()
        {
            var data = await _columnMetadataService.GetColumnDtos(1);
            Assert.IsNotNull(data);
            Assert.AreEqual(3, data.Count);
            Assert.AreEqual("Guid", data.First().ColumnName);
            Assert.AreEqual("UserName", data.Skip(1).First().ColumnName);
            Assert.AreEqual("Balance", data.Skip(2).First().ColumnName);
        }

        [TestMethod]
        public async Task TryCreateTableAndColumnMetaFromEntityTest()
        {
            //根据实体自动创建表和列
            ColumnTemplate columnTemplate = await _columnMetadataService.TryCreateTableAndColumnMetaFromEntityAsync<AdminUserInfo>();
            Assert.IsTrue(columnTemplate != null);
            Assert.IsTrue(columnTemplate.Count > 4);
            Assert.AreEqual(4, columnTemplate.TableId);

            Assert.IsTrue(columnTemplate.Exists(z => z.ColumnName == "Id"));
            Assert.IsTrue(columnTemplate.Exists(z => z.ColumnName == "Flag")); 
            Assert.IsTrue(columnTemplate.Exists(z => z.ColumnName == "Guid"));
            Assert.IsTrue(columnTemplate.Exists(z => z.ColumnName == "UserName"));
            Assert.IsTrue(columnTemplate.Exists(z => z.ColumnName == "Password")); 
            Assert.IsTrue(columnTemplate.Exists(z => z.ColumnName == "LastLoginTime"));

        }

    }
}