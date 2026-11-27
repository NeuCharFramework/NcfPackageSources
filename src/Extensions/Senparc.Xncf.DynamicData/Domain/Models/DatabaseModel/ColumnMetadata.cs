/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColumnMetadata.cs
    File description: ColumnMetadata related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

    Modified by: Senparc - 20261127
    Change description: v0.7.12.2 Improve source documentation, headers, and module metadata consistency.

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
    /// Describes one database column exposed by a DynamicData table.
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(ColumnMetadata))]
    [Serializable]
    public class ColumnMetadata : EntityBase<int>
    {
        /// <summary>  
        /// Gets the parent table metadata identifier.
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get; private set; }

        /// <summary>  
        /// Gets the database column name.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get; private set; }

        /// <summary>  
        /// Gets the database/provider column type name.
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get; private set; }

        /// <summary>  
        /// Gets whether the column permits null values.
        /// </summary>  
        [Required]
        public bool IsNullable { get; private set; } = true;

        /// <summary>  
        /// Gets the configured database default value, when one exists.
        /// </summary>  
        public string DefaultValue { get; private set; }

        /// <summary>  
        /// Gets or sets the associated table metadata.
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// Gets or sets the data cells associated with this column.
        /// </summary>  
        //[InverseProperty(nameof(TableData.ColumnMetadata))]
        public ICollection<TableData> TableDatas { get; set; }

        /// <summary>Initializes an empty entity for ORM materialization.</summary>
        private ColumnMetadata() { }

        /// <summary>
        /// Initializes column metadata for a DynamicData table.
        /// </summary>
        /// <param name="tableMetadataId">The parent table metadata identifier.</param>
        /// <param name="columnName">The database column name.</param>
        /// <param name="columnType">The database column type.</param>
        /// <param name="isNullable">Whether the column permits null values.</param>
        /// <param name="defaultValue">The configured default value.</param>
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
