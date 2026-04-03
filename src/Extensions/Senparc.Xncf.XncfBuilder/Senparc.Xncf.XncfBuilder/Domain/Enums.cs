using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain
{
    /// <summary>
    /// Prompt type of XncfBuilder
    /// </summary>
    public enum PromptBuildType
    {
        /// <summary>
        /// Entity class
        /// </summary>
        EntityClass,
        /// <summary>
        /// Entity class DTO
        /// </summary>
        EntityDtoClass,
        /// <summary>
        ///Update SenparcEntities
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
