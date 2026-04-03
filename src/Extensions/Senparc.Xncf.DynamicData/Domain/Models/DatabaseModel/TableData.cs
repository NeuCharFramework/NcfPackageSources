using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// TableData entity class, used to store cell data.  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableData))]
    [Serializable]
    public class TableData : EntityBase<int>
    {
        /// <summary>  
        ///The associated table ID.  
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        //public int TableMetadataId { get; set; }
        public int TableId { get; private set; }

        /// <summary>  
        /// Associated column ID.  
        /// </summary>  
        //[ForeignKey(nameof(ColumnMetadata))]
        public int ColumnMetadataId { get; private set; }

        /// <summary>  
        ///The value of the cell.  
        /// </summary>  
        public string CellValue { get; private set; }

        /// <summary>  
        /// Associated table metadata.  
        /// </summary>  
       
        ////[InverseProperty(nameof(TableMetadata.TableDatas))]
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// Associated column metadata.  
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
