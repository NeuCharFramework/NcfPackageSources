/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfClientDbData.cs
    File description: NcfClientDbData related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.14.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/


using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    /// <summary>Provides access to the shared client database context.</summary>
    public interface INcfClientDbData : INcfDbData
    {
        /// <summary>Gets the shared client database context.</summary>
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

        /// <inheritdoc />
        public override void CloseConnection()
        {
            //BaseDataContext.Database.Connection.Close();
            dataContext = null;
        }

        public override DbContext BaseDataContext
        {
            get
            {
                if (dataContext == null)
                {
                    // A closed wrapper intentionally remains disconnected; the scoped
                    // client context is recreated by dependency injection at the next boundary.
                }
                return dataContext;
            }

        }
    }
}
