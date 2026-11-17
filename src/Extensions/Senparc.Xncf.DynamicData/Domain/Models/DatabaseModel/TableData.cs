/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: TableData.cs

    File description: TableData related implementation

    

    

    Created by: Senparc - 20240718

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    ///Legacy comment translated to English.
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableData))]
    [Serializable]
    public class TableData : EntityBase<int>
    {
        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        //public int TableMetadataId { get; set; }
        public int TableId { get; private set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        //[ForeignKey(nameof(ColumnMetadata))]
        public int ColumnMetadataId { get; private set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string CellValue { get; private set; }

        /// <summary>  
        /// associated table metadata。  
        /// </summary>  
       
        ////[InverseProperty(nameof(TableMetadata.TableDatas))]
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
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
