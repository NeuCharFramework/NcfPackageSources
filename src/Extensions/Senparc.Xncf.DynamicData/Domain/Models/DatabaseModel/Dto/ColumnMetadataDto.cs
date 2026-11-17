/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ColumnMetadataDto.cs

    File description: ColumnMetadataDto related implementation

    

    

    Created by: Senparc - 20240718

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
        ///Legacy comment translated to English.
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get;  set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get;  set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get;  set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        public bool IsNullable { get;  set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string DefaultValue { get;  set; }

        /// <summary>  
        /// associated table metadata。  
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadataDto TableMetadata { get; set; }

        /// <summary>  
        /// associated table metadata。  
        /// </summary>  
        //[InverseProperty(nameof(TableData.ColumnMetadata))]
        public ICollection<TableDataDto> TableDatas { get; set; }

        public ColumnMetadataDto() { }
    }
}
