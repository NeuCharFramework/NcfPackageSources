/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TableData.cs
    文件功能描述：TableData 相关实现
    
    
    创建标识：Senparc - 20240718
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// TableData 实体类，用于存储单元格的数据。  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableData))]
    [Serializable]
    public class TableData : EntityBase<int>
    {
        /// <summary>  
        /// 关联的表格ID。  
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        //public int TableMetadataId { get; set; }
        public int TableId { get; private set; }

        /// <summary>  
        /// 关联的列ID。  
        /// </summary>  
        //[ForeignKey(nameof(ColumnMetadata))]
        public int ColumnMetadataId { get; private set; }

        /// <summary>  
        /// 单元格的值。  
        /// </summary>  
        public string CellValue { get; private set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
       
        ////[InverseProperty(nameof(TableMetadata.TableDatas))]
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// 关联的列元数据。  
        /// </summary>  
        //[InverseProperty(nameof(ColumnMetadata.TableDatas))]
        public ColumnMetadata ColumnMetadata { get; set; }

        private TableData() { }
        public TableData(int tableId, int columnMetadataId, string cellValue)
        {
            TableId = tableId;
            ColumnMetadataId = columnMetadataId;
            CellValue = cellValue;
        }

    }
}
