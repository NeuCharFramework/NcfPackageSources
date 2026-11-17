/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: BasePoolEntities.cs

    File description: BasePoolEntities related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    //Legacy comment translated to English.

    /// <summary>
    /// The current Entities exist only to help SenparcEntities generate migration information and have no special operational purpose.
    /// </summary>
    public class BasePoolEntities : SenparcEntities
    {
        private BasePoolEntities() : this(null, null) { }

        public BasePoolEntities(DbContextOptions/*<BasePoolEntities>*/ dbContextOptions, IServiceProvider serviceProvider)
            : base(dbContextOptions, serviceProvider)
        {

        }
    }
}
