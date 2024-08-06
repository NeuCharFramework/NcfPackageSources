using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}