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
            var tableMetadataDto = await base._tableMetadataService.GetTableMetadataDtoAsync(1);

            Assert.IsNotNull(tableMetadataDto);
            Console.WriteLine(tableMetadataDto.ToJson(true));
        }
    }
}