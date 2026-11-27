/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableDataDto.cs
    File description: TableDataDto related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

    Modified by: Senparc - 20261127
    Change description: v0.7.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
        /// Gets or sets the parent table identifier.
        /// </summary>  
        public int TableId { get; set; }

        /// <summary>  
        /// Gets or sets the column metadata identifier.
        /// </summary>  
        public int ColumnMetadataId { get; set; }

        /// <summary>  
        /// Gets or sets the serialized cell value.
        /// </summary>  
        public string CellValue { get; set; }

        /// <summary>  
        /// associated table metadata。  
        /// </summary>  
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// Gets or sets the associated column metadata.
        /// </summary>  
        public ColumnMetadataDto ColumnMetadata { get; set; }

        public TableDataDto() { }

    }
}
