/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DataTemplate.cs
    File description: DataTemplate related implementation

    Created by: Senparc - 20240812

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

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

        /// <summary>
        /// Initializes a data template with optional column definitions and row data.
        /// </summary>
        /// <param name="tableId">The table identifier.</param>
        /// <param name="columnTemplate">Optional column metadata template.</param>
        /// <param name="tableDataDtos">Optional table rows.</param>
        public DataTemplate(int tableId, ColumnTemplate columnTemplate = null, List<TableDataDto> tableDataDtos = null)
        {
            TableId = tableId;

            ColumnTemplate = columnTemplate ?? new ColumnTemplate(tableId);
            TableDataDtos = tableDataDtos ?? new List<TableDataDto>();
        }
    }
}
