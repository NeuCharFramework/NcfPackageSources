/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DataTemplate.cs

    File description: DataTemplate related implementation

    

    

    Created by: Senparc - 20240812

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
        public int TableId { get; set; } //Legacy comment translated to English.
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
