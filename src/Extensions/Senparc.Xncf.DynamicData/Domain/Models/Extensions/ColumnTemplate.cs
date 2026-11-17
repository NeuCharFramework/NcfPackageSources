/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ColumnTemplate.cs

    File description: ColumnTemplate related implementation

    

    

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

namespace Senparc.Xncf.DynamicData.Domain.Models.Extensions
{
    public class ColumnTemplate : List<ColumnMetadataDto>
    {
        public int TableId { get; set; }

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
