/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TableMetadata.cs
    文件功能描述：TableMetadata 相关实现
    
    
    创建标识：Senparc - 20240718
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// TableMetadata 实体类，用于存储表格的基本信息。  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableMetadata))]
    [Serializable]
    public class TableMetadata : EntityBase<int>
    {
        /// <summary>  
        /// 表格名称。  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; private set; }

        /// <summary>  
        /// 表格描述。  
        /// </summary>  
        public string Description { get; private set; }

        /// <summary>  
        /// 关联的列元数据集合。  
        /// </summary>  
        //[InverseProperty(nameof(ColumnMetadata.TableMetadata))]
        public ICollection<ColumnMetadata> ColumnMetadatas { get; set; }

        /// <summary>  
        /// 关联的数据集合。  
        /// </summary>  
        //[InverseProperty(nameof(TableData.TableMetadata))]
        public ICollection<TableData> TableDatas { get; set; }

        private TableMetadata() { }

        public TableMetadata(string tableName, string description)
        {
            TableName = tableName;
            Description = description;
        }

    }
}
