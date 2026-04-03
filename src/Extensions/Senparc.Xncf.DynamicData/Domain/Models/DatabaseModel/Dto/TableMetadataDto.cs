using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class TableMetadataDto : DtoBase<int>
    {
        /// <summary>  
        /// table name.  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; set; }

        /// <summary>  
        ///Table description.  
        /// </summary>  
        public string Description { get; set; }

        /// <summary>  
        /// Associated column metadata collection.  
        /// </summary>  
        public ICollection<ColumnMetadataDto> ColumnMetadatas { get; set; }

        /// <summary>  
        /// Associated data collection.  
        /// </summary>  
        public ICollection<TableDataDto> TableDatas { get; set; }


        public TableMetadataDto() { }
    }
}
