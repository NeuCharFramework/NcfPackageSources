/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColumnMetadataDto.cs
    File description: ColumnMetadataDto related implementation

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
    public class ColumnMetadataDto : DtoBase<ColumnMetadata,int>
    {
        /// <summary>  
        /// Gets or sets the parent table metadata identifier.
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get;  set; }

        /// <summary>  
        /// Gets or sets the database column name.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get;  set; }

        /// <summary>  
        /// Gets or sets the database/provider column type name.
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get;  set; }

        /// <summary>  
        /// Gets or sets whether the column permits null values.
        /// </summary>  
        [Required]
        public bool IsNullable { get;  set; }

        /// <summary>  
        /// Gets or sets the configured database default value.
        /// </summary>  
        public string DefaultValue { get;  set; }

        /// <summary>  
        /// Gets or sets the associated table metadata.
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadataDto TableMetadata { get; set; }

        /// <summary>  
        /// Gets or sets the data cells associated with this column.
        /// </summary>  
        //[InverseProperty(nameof(TableData.ColumnMetadata))]
        public ICollection<TableDataDto> TableDatas { get; set; }

        public ColumnMetadataDto() { }
    }
}
