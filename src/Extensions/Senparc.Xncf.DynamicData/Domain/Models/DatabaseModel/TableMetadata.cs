using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// TableMetadata entity class, used to store basic information of the table.  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableMetadata))]
    [Serializable]
    public class TableMetadata : EntityBase<int>
    {
        /// <summary>  
        /// table name.  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; private set; }

        /// <summary>  
        ///Table description.  
        /// </summary>  
        public string Description { get; private set; }

        /// <summary>  
        /// Associated column metadata collection.  
        /// </summary>  
        //[InverseProperty(nameof(ColumnMetadata.TableMetadata))]
        public ICollection<ColumnMetadata> ColumnMetadatas { get; set; }

        /// <summary>  
        /// Associated data collection.  
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
