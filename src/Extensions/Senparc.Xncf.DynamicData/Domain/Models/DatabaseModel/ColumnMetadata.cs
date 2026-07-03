/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ColumnMetadata.cs
    文件功能描述：ColumnMetadata 相关实现
    
    
    创建标识：Senparc - 20240718
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.VisualBasic;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// ColumnMetadata 实体类，用于存储列的基本信息。  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(ColumnMetadata))]
    [Serializable]
    public class ColumnMetadata : EntityBase<int>
    {
        /// <summary>  
        /// 关联的表格ID。  
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get; private set; }

        /// <summary>  
        /// 列名称。  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get; private set; }

        /// <summary>  
        /// 列的数据类型。  
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get; private set; }

        /// <summary>  
        /// 是否允许NULL值。  
        /// </summary>  
        [Required]
        public bool IsNullable { get; private set; } = true;

        /// <summary>  
        /// 列的默认值。  
        /// </summary>  
        public string DefaultValue { get; private set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //[InverseProperty(nameof(TableData.ColumnMetadata))]
        public ICollection<TableData> TableDatas { get; set; }

        private ColumnMetadata() { }
        public ColumnMetadata(int tableMetadataId, string columnName, string columnType, bool isNullable, string defaultValue)
        {
            TableMetadataId = tableMetadataId;
            ColumnName = columnName;
            ColumnType = columnType;
            IsNullable = isNullable;
            DefaultValue = defaultValue;
        }
    }
}
