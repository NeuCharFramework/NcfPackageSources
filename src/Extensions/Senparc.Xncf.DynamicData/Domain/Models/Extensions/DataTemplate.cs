/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DataTemplate.cs
    文件功能描述：DataTemplate 相关实现
    
    
    创建标识：Senparc - 20240812
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
