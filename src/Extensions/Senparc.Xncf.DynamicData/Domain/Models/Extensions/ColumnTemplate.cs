/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ColumnTemplate.cs
    文件功能描述：ColumnTemplate 相关实现
    
    
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
