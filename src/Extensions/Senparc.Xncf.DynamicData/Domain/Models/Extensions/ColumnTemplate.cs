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
