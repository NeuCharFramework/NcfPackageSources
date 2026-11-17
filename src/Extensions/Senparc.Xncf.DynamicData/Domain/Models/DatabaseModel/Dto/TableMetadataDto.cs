/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: TableMetadataDto.cs

    File description: TableMetadataDto related implementation

    

    

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
    public class TableMetadataDto : DtoBase<int>
    {
        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string Description { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public ICollection<ColumnMetadataDto> ColumnMetadatas { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public ICollection<TableDataDto> TableDatas { get; set; }


        public TableMetadataDto() { }
    }
}
