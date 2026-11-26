/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableMetadata.cs
    File description: TableMetadata related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// Describes a DynamicData database table and its discovered columns and cells.
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableMetadata))]
    [Serializable]
    public class TableMetadata : EntityBase<int>
    {
        /// <summary>  
        /// Gets the database table name.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; private set; }

        /// <summary>  
        /// Gets the table description shown to administrators.
        /// </summary>  
        public string Description { get; private set; }

        /// <summary>  
        /// Gets or sets the columns discovered for the table.
        /// </summary>  
        //[InverseProperty(nameof(ColumnMetadata.TableMetadata))]
        public ICollection<ColumnMetadata> ColumnMetadatas { get; set; }

        /// <summary>  
        /// Gets or sets the cell records associated with the table.
        /// </summary>  
        //[InverseProperty(nameof(TableData.TableMetadata))]
        public ICollection<TableData> TableDatas { get; set; }

        private TableMetadata() { }

        /// <summary>
        /// Initializes table metadata from its display name and description.
        /// </summary>
        /// <param name="tableName">The database table name.</param>
        /// <param name="description">The administrator-facing table description.</param>
        public TableMetadata(string tableName, string description)
        {
            TableName = tableName;
            Description = description;
        }

    }
}
