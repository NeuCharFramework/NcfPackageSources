
using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    public interface INcfClientDbData : INcfDbData
    {
        BasePoolEntities DataContext { get; }
    }

    //[Pluggable("ClientDatabase")]
    public class NcfClientDbData : NcfDbData, INcfDbData, INcfClientDbData
    {
        private BasePoolEntities dataContext;

        //public NcfClientDbData() { }

        public NcfClientDbData(BasePoolEntities senparcEntities)
        {
            dataContext = senparcEntities;
        }


        public BasePoolEntities DataContext
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
            //Comment out COCONET when upgrading to .net core.
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
                    //TODO: Currently, injection can ensure the HttpContext singleton. If you want a global singleton, you can use the singleton mode (you need to solve the release problem first)
                }
                //var hashCode = dataContext.GetHashCode();
                //System.Web.HttpContext.Current.Response.Write(dataContext.GetHashCode() + "<br />");//Test that there is only one SenparcEntities instance for the same Request
                return dataContext;
            }

        }
    }
}
