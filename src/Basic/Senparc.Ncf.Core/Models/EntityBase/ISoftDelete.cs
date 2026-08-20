/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ISoftDelete.cs
    文件功能描述：ISoftDelete 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 数据库数据软删除接口
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>
        /// 是否软删除
        /// </summary>
        bool Flag { get; set; }
    }
}
