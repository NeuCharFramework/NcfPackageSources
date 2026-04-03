using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Tenant.Domain.Models
{
    public interface ITenantInfoDbData : INcfDbData
    {
        SenparcEntitiesMultiTenant DataContext { get; }
    }

    public class TenantInfoDbData : NcfDbData, INcfDbData, ITenantInfoDbData
    {
        private SenparcEntitiesMultiTenant dataContext;

        public TenantInfoDbData(SenparcEntitiesMultiTenant senparcEntities)
        {
            dataContext = senparcEntities;
        }


        public SenparcEntitiesMultiTenant DataContext
        {
            get
            {
                return BaseDataContext as SenparcEntitiesMultiTenant;
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
                return dataContext;
            }

        }
    }
}
