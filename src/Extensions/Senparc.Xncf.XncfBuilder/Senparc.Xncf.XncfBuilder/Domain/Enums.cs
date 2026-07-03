/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Enums.cs
    文件功能描述：Enums 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain
{
    /// <summary>
    /// XncfBuilder 的 Prompt 类型
    /// </summary>
    public enum PromptBuildType
    {
        /// <summary>
        /// 实体类
        /// </summary>
        EntityClass,
        /// <summary>
        /// 实体类 DTO
        /// </summary>
        EntityDtoClass,
        /// <summary>
        /// 更新 SenparcEntities
        /// </summary>
        UpdateSenparcEntities,
        /// <summary>
        /// Repository
        /// </summary>
        Repository,
        /// <summary>
        /// Service
        /// </summary>
        Service,
        /// <summary>
        /// AppService
        /// </summary>
        AppService,
        /// <summary>
        /// PL
        /// </summary>
        PL,
        /// <summary>
        /// Dbcontext
        /// </summary>
        DbContext
    }
}
