/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: NcfUnitTestDataDb.cs

    File description: NcfUnitTestDataDb related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

    public class NcfUnitTestDataDb : NcfDbData, INcfDbData, INcfClientDbData
    {
        private NcfUnitTestEntities dataContext;

        public NcfUnitTestDataDb() { }

        public NcfUnitTestDataDb(NcfUnitTestEntities dbContext)
        {
            dataContext = dbContext;
        }

        //public NcfUnitTestDataDb(IServiceProvider serviceProvider)
        //{
        //    dataContext = new NcfUnitTestEntities(new DbContextOptionsBuilder().Options, serviceProvider);
        //}



        public virtual BasePoolEntities DataContext
        {
            get
            {
                return BaseDataContext as BasePoolEntities;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public override void CloseConnection()
        {
            //Legacy comment translated to English.
            //BaseDataContext.Database.Connection.Close();
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
                    //Legacy comment translated to English.
                }
                //var hashCode = dataContext.GetHashCode();
                //Legacy comment translated to English.
                return dataContext;
            }

        }
    }
}
