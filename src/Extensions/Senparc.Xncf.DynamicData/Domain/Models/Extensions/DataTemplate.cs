using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.Domain.Models.Extensions;

namespace Senparc.Xncf.DynamicData.Domain.Models
{
    public class DataTemplate
    {
        public int TableId { get; set; } //TODO: 缓存所有 Table 和 Column 信息
        public ColumnTemplate ColumnTemplate { get; set; }

        public List<TableDataDto> TableDataDtos { get; set; }

        public DataTemplate(int tableId, ColumnTemplate columnTemplate = null, List<TableDataDto> tableDataDtos = null)
        {
            TableId = tableId;

            ColumnTemplate = columnTemplate ?? new ColumnTemplate(tableId);
            TableDataDtos = tableDataDtos ?? new List<TableDataDto>();
        }
    }
}
