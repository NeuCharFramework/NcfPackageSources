/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableMetadata.cs
    File description: TableMetadata related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
    [Table(Register.DATABASE_PREFIX + nameof(TableMetadata))]
    [Serializable]
    public class TableMetadata : EntityBase<int>
    {
        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; private set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string Description { get; private set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        //[InverseProperty(nameof(ColumnMetadata.TableMetadata))]
        public ICollection<ColumnMetadata> ColumnMetadatas { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
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
