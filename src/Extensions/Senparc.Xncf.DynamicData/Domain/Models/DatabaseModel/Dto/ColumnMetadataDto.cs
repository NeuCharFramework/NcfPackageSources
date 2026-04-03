using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class ColumnMetadataDto : DtoBase<ColumnMetadata,int>
    {
        /// <summary>  
        ///The associated table ID.
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get;  set; }

        /// <summary>  
        /// column name.  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get;  set; }

        /// <summary>  
        /// The data type of the column.  
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get;  set; }

        /// <summary>  
        /// Whether to allow NULL values.  
        /// </summary>  
        [Required]
        public bool IsNullable { get;  set; }

        /// <summary>  
        /// The default value for the column.  
        /// </summary>  
        public string DefaultValue { get;  set; }

        /// <summary>  
        /// Associated table metadata.  
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadataDto TableMetadata { get; set; }

        /// <summary>  
        /// Associated table metadata.  
        /// </summary>  
        //[InverseProperty(nameof(TableData.ColumnMetadata))]
        public ICollection<TableDataDto> TableDatas { get; set; }

        public ColumnMetadataDto() { }
    }
}
