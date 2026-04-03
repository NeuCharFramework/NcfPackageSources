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
        /// Close the connection (maintaining a connection operation for a long time will cause the database operation time to gradually become longer)
        /// </summary>
        public override void CloseConnection()
        {
            //COCONET upgrade to.net coreComment out the process
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
                    //TODO:Currently, injection can be used to ensure the HttpContext singleton. If you want a global singleton, you can use the singleton mode (you need to solve the release problem first)
                }
                //var hashCode = dataContext.GetHashCode();
                //System.Web.HttpContext.Current.Response.Write(dataContext.GetHashCode() + "<br />");//Test that there is only one SenparcEntities instance for the same Request
                return dataContext;
            }

        }
    }
}
