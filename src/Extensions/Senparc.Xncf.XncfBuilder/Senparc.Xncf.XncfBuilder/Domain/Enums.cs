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
