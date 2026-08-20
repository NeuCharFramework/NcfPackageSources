/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ColumnMetadataDto.cs
    文件功能描述：ColumnMetadataDto 相关实现
    
    
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
    public class ColumnMetadataDto : DtoBase<ColumnMetadata,int>
    {
        /// <summary>  
        /// 关联的表格ID。
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get;  set; }

        /// <summary>  
        /// 列名称。  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get;  set; }

        /// <summary>  
        /// 列的数据类型。  
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get;  set; }

        /// <summary>  
        /// 是否允许NULL值。  
        /// </summary>  
        [Required]
        public bool IsNullable { get;  set; }

        /// <summary>  
        /// 列的默认值。  
        /// </summary>  
        public string DefaultValue { get;  set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadataDto TableMetadata { get; set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //[InverseProperty(nameof(TableData.ColumnMetadata))]
        public ICollection<TableDataDto> TableDatas { get; set; }

        public ColumnMetadataDto() { }
    }
}
