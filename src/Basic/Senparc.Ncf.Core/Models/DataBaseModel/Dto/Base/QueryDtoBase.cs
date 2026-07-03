/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：QueryDtoBase.cs
    文件功能描述：QueryDtoBase 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
