/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableMetadataDto.cs
    File description: TableMetadataDto related implementation

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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class TableMetadataDto : DtoBase<int>
    {
        /// <summary>  
        /// Gets or sets the database table name.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; set; }

        /// <summary>  
        /// Gets or sets the table description.
        /// </summary>  
        public string Description { get; set; }

        /// <summary>  
        /// Gets or sets the columns belonging to the table.
        /// </summary>  
        public ICollection<ColumnMetadataDto> ColumnMetadatas { get; set; }

        /// <summary>  
        /// Gets or sets the cell records belonging to the table.
        /// </summary>  
        public ICollection<TableDataDto> TableDatas { get; set; }


        public TableMetadataDto() { }
    }
}
