using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.DynamicData;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.Domain.Services;

namespace Senparc.Xncf.DynamicDataTests.Domain.Services
{
    [TestClass]
    public class TableDataTests : BaseDynamicDataTest
    {
        TableDataService _tableDataService;
        ColumnMetadataService _columnMetadataService;

        public TableDataTests()
        {
            _tableDataService = base._serviceProvider.GetRequiredService<TableDataService>();
            _columnMetadataService = base._serviceProvider.GetRequiredService<ColumnMetadataService>();
        }

        [TestMethod]
        public async Task InsertDataTest()
        {
            var tableId = 1;
            var data = await _tableDataService.GetTableDataTemplateAsync(tableId);
            Assert.AreEqual(3, data.ColumnTamplate.Count);
            Assert.AreEqual(3, data.DataTemplate.Count);

            var dataDic1 = new Dictionary<string, string>() {
                {"Guid",Guid.NewGuid().ToString()},
                {"UserName","Jeff"},
                {"Balance","1000"}
            };
            var dataDic2 = new Dictionary<string, string>() {
                {"Guid",Guid.NewGuid().ToString()},
                {"UserName","Bob"},
                {"Balance","2000"}
            };

            _tableDataService.SetData(data.ColumnTamplate, data.DataTemplate, dataDic1);
            var insertResult = await _tableDataService.InsertDataAsync(data.DataTemplate);
            Assert.IsTrue(insertResult.Success);
            Assert.AreEqual(3, insertResult.SucessDataList.Count);

            //插入第二条数据   TODO:
            _tableDataService.SetData(data.ColumnTamplate, data.DataTemplate, dataDic2);
            insertResult = await _tableDataService.InsertDataAsync(data.DataTemplate);
            Assert.IsTrue(insertResult.Success);
            Assert.AreEqual(3, insertResult.SucessDataList.Count);

            var tableData = await _tableDataService.GetFullListAsync(z => z.TableId == tableId);

            Assert.IsNotNull(tableData);
            Assert.AreEqual(6, tableData.Count);
            Assert.IsTrue(tableData.Exists(z => z.CellValue == "Jeff"));
            Assert.IsTrue(tableData.Exists(z => z.CellValue == "1000"));
            Assert.IsTrue(tableData.Exists(z => z.CellValue == "Bob"));
            Assert.IsTrue(tableData.Exists(z => z.CellValue == "2000"));

        }
    }
}
