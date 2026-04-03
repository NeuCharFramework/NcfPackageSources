using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class TableDataDto: DtoBase
    {

        /// <summary>  
        ///The associated table ID.  
        /// </summary>  
        public int TableId { get; set; }

        /// <summary>  
        /// Associated column ID.  
        /// </summary>  
        public int ColumnMetadataId { get; set; }

        /// <summary>  
        ///The value of the cell.  
        /// </summary>  
        public string CellValue { get; set; }

        /// <summary>  
        /// Associated table metadata.  
        /// </summary>  
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// Associated column metadata.  
        /// </summary>  
        public ColumnMetadataDto ColumnMetadata { get; set; }

        public TableDataDto() { }

    }
}
