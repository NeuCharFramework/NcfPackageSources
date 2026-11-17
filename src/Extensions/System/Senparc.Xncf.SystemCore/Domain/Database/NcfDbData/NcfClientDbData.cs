/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: NcfClientDbData.cs

    File description: NcfClientDbData related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/


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
