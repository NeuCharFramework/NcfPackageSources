/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableData.cs
    File description: TableData related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// Stores one cell value associated with a DynamicData table and column.
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableData))]
    [Serializable]
    public class TableData : EntityBase<int>
    {
        /// <summary>  
        /// Gets the parent table identifier.
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        //public int TableMetadataId { get; set; }
        public int TableId { get; private set; }

        /// <summary>  
        /// Gets the column metadata identifier.
        /// </summary>  
        //[ForeignKey(nameof(ColumnMetadata))]
        public int ColumnMetadataId { get; private set; }

        /// <summary>  
        /// Gets the serialized cell value.
        /// </summary>  
        public string CellValue { get; private set; }

        /// <summary>  
        /// associated table metadata。  
        /// </summary>  
       
        ////[InverseProperty(nameof(TableMetadata.TableDatas))]
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// Gets or sets the associated column metadata.
        /// </summary>  
        //[InverseProperty(nameof(ColumnMetadata.TableDatas))]
        public ColumnMetadata ColumnMetadata { get; set; }

        private TableData() { }
        /// <summary>
        /// Initializes one table cell value.
        /// </summary>
        /// <param name="tableId">The parent table identifier.</param>
        /// <param name="columnMetadataId">The column metadata identifier.</param>
        /// <param name="cellValue">The serialized value stored in the cell.</param>
        public TableData(int tableId, int columnMetadataId, string cellValue)
        {
            TableId = tableId;
            ColumnMetadataId = columnMetadataId;
            CellValue = cellValue;
        }

    }
}
