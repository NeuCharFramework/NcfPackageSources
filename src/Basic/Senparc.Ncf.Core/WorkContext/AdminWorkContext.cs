/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminWorkContext.cs
    文件功能描述：AdminWorkContext 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.WorkContext
{
    /// <summary>
    /// 管理员区域上下文
    /// </summary>
    public class AdminWorkContext
    {
        /// <summary>
        /// 姓名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 用户Id
        /// </summary>
        public int AdminUserId { get; set; }

        /// <summary>
        /// 拥有的角色
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }
    }
}
