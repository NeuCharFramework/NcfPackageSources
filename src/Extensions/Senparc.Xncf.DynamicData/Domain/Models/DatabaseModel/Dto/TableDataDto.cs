/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TableDataDto.cs
    文件功能描述：TableDataDto 相关实现
    
    
    创建标识：Senparc - 20240718
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class TableDataDto: DtoBase
    {

        /// <summary>  
        /// 关联的表格ID。  
        /// </summary>  
        public int TableId { get; set; }

        /// <summary>  
        /// 关联的列ID。  
        /// </summary>  
        public int ColumnMetadataId { get; set; }

        /// <summary>  
        /// 单元格的值。  
        /// </summary>  
        public string CellValue { get; set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// 关联的列元数据。  
        /// </summary>  
        public ColumnMetadataDto ColumnMetadata { get; set; }

        public TableDataDto() { }

    }
}
