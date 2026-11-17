/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ColumnMetadata.cs

    File description: ColumnMetadata related implementation

    

    

    Created by: Senparc - 20240718

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(ColumnMetadata))]
    [Serializable]
    public class ColumnMetadata : EntityBase<int>
    {
        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get; private set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get; private set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get; private set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        public bool IsNullable { get; private set; } = true;

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string DefaultValue { get; private set; }

        /// <summary>  
        /// associated table metadata。  
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// associated table metadata。  
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
