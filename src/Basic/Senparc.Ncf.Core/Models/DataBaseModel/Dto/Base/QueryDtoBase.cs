/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QueryDtoBase.cs
    File description: QueryDtoBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel.Dto.Base
{
    public class QueryDtoBase
    {
        /// <summary>
        /// xxxx Desc, ddd asc
        /// </summary>
        public string OrderBy { get; set; }
    }
}
