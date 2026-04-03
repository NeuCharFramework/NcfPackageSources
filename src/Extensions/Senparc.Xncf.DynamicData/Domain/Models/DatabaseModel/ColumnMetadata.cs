using Microsoft.VisualBasic;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// ColumnMetadata entity class, used to store basic information of columns.  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(ColumnMetadata))]
    [Serializable]
    public class ColumnMetadata : EntityBase<int>
    {
        /// <summary>  
        ///The associated table ID.  
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get; private set; }

        /// <summary>  
        /// column name.  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get; private set; }

        /// <summary>  
        /// The data type of the column.  
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get; private set; }

        /// <summary>  
        /// Whether to allow NULL values.  
        /// </summary>  
        [Required]
        public bool IsNullable { get; private set; } = true;

        /// <summary>  
        /// The default value for the column.  
        /// </summary>  
        public string DefaultValue { get; private set; }

        /// <summary>  
        /// Associated table metadata.  
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// Associated table metadata.  
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
