using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    //TODO: If there are no obvious changes later (such as additional extensions), consider merging SenparcEntities and replacing them

    /// <summary>
    /// Current Entities only exist to help SenparcEntities generate Migration information and have no special operational significance.
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
