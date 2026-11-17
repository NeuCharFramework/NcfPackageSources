/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Enums.cs

    File description: Enums related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum PromptBuildType
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        EntityClass,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        EntityDtoClass,
        /// <summary>
        /// update SenparcEntities
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
