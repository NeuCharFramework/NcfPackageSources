/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoDbData.cs
    File description: TenantInfoDbData related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
                return dataContext;
            }

        }
    }
}
