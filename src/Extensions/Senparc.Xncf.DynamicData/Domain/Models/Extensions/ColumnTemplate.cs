/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColumnTemplate.cs
    File description: ColumnTemplate related implementation

    Created by: Senparc - 20240812

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

    Modified by: Senparc - 20261127
    Change description: v0.7.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.DynamicData.Domain.Models.Extensions
{
    public class ColumnTemplate : List<ColumnMetadataDto>
    {
        public int TableId { get; set; }

        /// <summary>
        /// Initializes a column template for a table and optionally copies metadata entries.
        /// </summary>
        /// <param name="tableId">The table identifier.</param>
        /// <param name="data">Optional column metadata entries.</param>
        public ColumnTemplate(int tableId, IEnumerable<ColumnMetadataDto> data = null)
        {
            TableId = tableId;
            if (data != null)
            {
                base.AddRange(data);
            }
        }
    }
}
