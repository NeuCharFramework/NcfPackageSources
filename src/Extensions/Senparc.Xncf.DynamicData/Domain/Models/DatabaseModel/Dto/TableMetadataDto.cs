/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TableMetadataDto.cs
    文件功能描述：TableMetadataDto 相关实现
    
    
    创建标识：Senparc - 20240718
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class TableMetadataDto : DtoBase<int>
    {
        /// <summary>  
        /// 表格名称。  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; set; }

        /// <summary>  
        /// 表格描述。  
        /// </summary>  
        public string Description { get; set; }

        /// <summary>  
        /// 关联的列元数据集合。  
        /// </summary>  
        public ICollection<ColumnMetadataDto> ColumnMetadatas { get; set; }

        /// <summary>  
        /// 关联的数据集合。  
        /// </summary>  
        public ICollection<TableDataDto> TableDatas { get; set; }


        public TableMetadataDto() { }
    }
}
