/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableDataDto.cs
    File description: TableDataDto related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///Legacy comment translated to English.
        /// </summary>  
        public int TableId { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public int ColumnMetadataId { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string CellValue { get; set; }

        /// <summary>  
        /// associated table metadata。  
        /// </summary>  
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public ColumnMetadataDto ColumnMetadata { get; set; }

        public TableDataDto() { }

    }
}
