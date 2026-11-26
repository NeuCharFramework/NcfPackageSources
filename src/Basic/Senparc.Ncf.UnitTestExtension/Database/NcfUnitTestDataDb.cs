/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfUnitTestDataDb.cs
    File description: NcfUnitTestDataDb related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.29 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension.Database
{
    /// <summary>
    /// Supplies the unit-test EF Core context through the NCF database abstraction.
    /// </summary>
    public class NcfUnitTestDataDb : NcfDbData, INcfDbData, INcfClientDbData
    {
        private NcfUnitTestEntities dataContext;

        /// <summary>Initializes an empty test database wrapper.</summary>
        public NcfUnitTestDataDb() { }

        /// <summary>Initializes the wrapper with an existing test context.</summary>
        /// <param name="dbContext">The test database context.</param>
        public NcfUnitTestDataDb(NcfUnitTestEntities dbContext)
        {
            dataContext = dbContext;
        }

        //public NcfUnitTestDataDb(IServiceProvider serviceProvider)
        //{
        //    dataContext = new NcfUnitTestEntities(new DbContextOptionsBuilder().Options, serviceProvider);
        //}



        /// <summary>Gets the underlying system-core-compatible context.</summary>
        public virtual BasePoolEntities DataContext
        {
            get
            {
                return BaseDataContext as BasePoolEntities;
            }
        }

        /// <summary>Releases the wrapper's context reference.</summary>
        public override void CloseConnection()
        {
            dataContext = null;
        }

        public override DbContext BaseDataContext
        {
            get
            {
                if (dataContext == null)
                {
                    //var connectionString = Ncf.Core.Config.SenparcDatabaseConfigs.ClientConnectionString;

                    //dataContext = SenparcDI.GetService<SenparcEntities>();
                }
                return dataContext;
            }

        }
    }
}
